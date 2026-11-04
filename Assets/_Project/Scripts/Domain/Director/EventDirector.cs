using System;
using SugarDash.Domain.Config;
using SugarDash.Domain.Events;
using SugarDash.Domain.Glucose;
using SugarDash.Domain.Ports;

namespace SugarDash.Domain.Director
{
    /// <summary>
    /// 급하강, 저혈당·고혈당 이벤트 발생, 게이지 경고를 결정한다(docs/glucose-model.md 1.2·4절, docs/hyper-event.md).
    /// 혈당 계산은 GlucoseModel이 하고, 여기서는 판정과 이벤트 발행만 한다.
    /// 이벤트 진행 중과 이벤트 종료부터 유예 시간 끝까지는 발동과 급하강을 억제한다.
    /// </summary>
    public sealed class EventDirector
    {
        private enum HypoState
        {
            Armed,
            Active,
            Cooldown,
            /// <summary>cooldown이 끝났지만 해제 수준에 닿지 못했다. 다시 내려가면 재발로 발동한다.</summary>
            PendingRecurrence,
        }

        private enum LowState
        {
            Armed,
            Active,
            /// <summary>tell을 마쳤다. 저혈당 해제 수준 이상으로 올라오면 다시 무장한다.</summary>
            WaitingRelease,
        }

        private enum HyperState
        {
            Armed,
            Active,
            /// <summary>대처를 마쳤다. 해제 수준 아래로 내려오면 다시 무장한다. cooldown과 재발은 없다.</summary>
            WaitingRelease,
        }

        private readonly GlucoseConfig _config;
        private readonly GlucoseModel _model;
        private readonly IRandomSource _random;
        private readonly DomainEventBus _bus;
        private readonly Type2Config _type2;

        private HypoState _hypoState = HypoState.Armed;
        private HyperState _hyperState = HyperState.Armed;
        private double _cooldownEnd;
        private bool _rearmedDuringCooldown;
        private bool _suppressed;
        private int _lastEventId;
        private bool _targetRangeEvaluated;
        private double? _suddenDropChanceOverride;

        // 2형 판정 상태. type2가 null이면 쓰지 않는다.
        private LowState _lowState = LowState.Armed;
        private double _highHold;
        private double _lowHold;
        private double _noticeEnd;

        /// <param name="type2">null이 아니면 2형 모드다. 급하강·1형 저혈당·게이지 경고 대신 hold 누적과 notice, tell 판정을 한다.</param>
        public EventDirector(GlucoseConfig config, GlucoseModel model, IRandomSource random, DomainEventBus bus,
            Type2Config type2 = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _type2 = type2;
            IsGaugeWarningOn = type2 == null && IsDisplayedBelowWarning();
        }

        /// <summary>
        /// 발동 허용 범위. 기본 All. All이 아니면 급하강은 일어나지 않고, 막힌 쪽은 발동과 2형 hold 누적을 하지 않는다.
        /// 상태(cooldown, 해제, 재발) 갱신은 그대로 한다.
        /// </summary>
        public TriggerGate Gate { get; private set; } = TriggerGate.All;

        public void SetTriggerGate(TriggerGate gate) => Gate = gate;

        private bool LowAllowed => Gate == TriggerGate.All || Gate == TriggerGate.LowOnly;

        private bool HighAllowed => Gate == TriggerGate.All || Gate == TriggerGate.HighOnly;

        /// <summary>장면 덮어쓰기. null이면 설정 값(suddenDrop.chancePerSec)으로 돌아간다.</summary>
        public void SetSuddenDropChancePerSec(double? chancePerSec) => _suddenDropChanceOverride = chancePerSec;

        /// <summary>
        /// 2형에서 notice가 시작된 뒤 그 tell이 끝날 때(<see cref="NotifyHyperEventEnded"/>, <see cref="NotifyLowEventEnded"/>)까지 값이 있다.
        /// </summary>
        public NoticeKind? ActiveSignal { get; private set; }

        public bool IsHypoEventActive => _hypoState == HypoState.Active;

        public bool IsHyperEventActive => _hyperState == HyperState.Active;

        /// <summary>
        /// 다음 판정에서 고혈당이 발동한다. 유예가 끝난 프레임에 true가 되고 다음 판정에서 발동한다.
        /// RunController는 이 동안 주행을 끝내지 않는다(과잉 처치 뒤 고혈당 알리기를 건너뛰지 않게).
        /// </summary>
        public bool IsHyperTriggerPending => _type2 == null && CanTriggerHyper();

        /// <summary>표시 혈당이 경고선 아래인 동안 true. 히스테리시스는 두지 않는다.</summary>
        public bool IsGaugeWarningOn { get; private set; }

        /// <summary>
        /// 실제 혈당이 적정 범위(normalRangeMin 이상, hyperThreshold 미만) 안이면 true.
        /// <see cref="EvaluateTargetRange"/>를 처음 부르기 전에는 false.
        /// </summary>
        public bool IsInTargetRange { get; private set; }

        /// <summary>RunController가 GlucoseModel.Tick 직후 호출한다.</summary>
        /// <param name="simDt">이번 Tick에서 흐른 simTime.</param>
        public void Evaluate(double simDt)
        {
            if (!(simDt >= 0)) throw new ArgumentOutOfRangeException(nameof(simDt), simDt, "0 이상이어야 한다");

            if (_type2 != null)
            {
                EvaluateType2(simDt);
                EvaluateTargetRange();
                return;
            }

            EvaluateSuddenDrop(simDt);
            EvaluateHypo();
            EvaluateHyper();
            EvaluateGaugeWarning();
            EvaluateTargetRange();
        }

        /// <summary>
        /// 적정 범위 안팎을 판정한다. 처음 판정에서 범위 안이면 GlucoseRangeEntered를 내고 범위 밖이면 아무것도 내지 않는다.
        /// 그 뒤에는 안팎이 바뀔 때만 들어감·나감을 낸다. <see cref="Evaluate"/> 끝에서도 호출한다.
        /// </summary>
        public void EvaluateTargetRange()
        {
            var levels = _config.Levels;
            double actual = _model.Actual;
            bool inRange = levels.NormalRangeMin <= actual && actual < levels.HyperThreshold;

            bool first = !_targetRangeEvaluated;
            _targetRangeEvaluated = true;
            if (!first && inRange == IsInTargetRange) return;

            IsInTargetRange = inRange;
            if (inRange) _bus.Publish(new GlucoseRangeEntered(actual, _model.SimTime, _model.DecisionTime));
            else if (!first) _bus.Publish(new GlucoseRangeExited(actual, _model.SimTime, _model.DecisionTime));
        }

        /// <summary>
        /// HypoEventFsm이 4단계 대처를 마치면 호출한다. 이때부터 cooldown이 시작되고,
        /// <see cref="NotifyGraceEnded"/> 전까지 발동과 급하강을 억제한다.
        /// </summary>
        public void NotifyHypoEventEnded()
        {
            if (_hypoState != HypoState.Active) throw new InvalidOperationException("진행 중인 저혈당 이벤트가 없다");

            _hypoState = HypoState.Cooldown;
            _cooldownEnd = _model.SimTime + _config.Recurrence.CooldownSec;
            _rearmedDuringCooldown = false;
            _suppressed = true;
        }

        /// <summary>
        /// HypoEventFsm이 고혈당 알리기를 마치면 호출한다. <see cref="NotifyGraceEnded"/> 전까지 발동과 급하강을 억제한다.
        /// 저혈당 cooldown에는 영향을 주지 않는다.
        /// </summary>
        public void NotifyHyperEventEnded()
        {
            if (_hyperState != HyperState.Active) throw new InvalidOperationException("진행 중인 고혈당 이벤트가 없다");

            _hyperState = HyperState.WaitingRelease;
            _suppressed = true;
            ActiveSignal = null;
        }

        /// <summary>2형 저혈당 tell을 마치면 호출한다. <see cref="NotifyGraceEnded"/> 전까지 누적을 멈추고, 해제 수준 이상이 되어야 다시 무장한다.</summary>
        public void NotifyLowEventEnded()
        {
            if (_lowState != LowState.Active) throw new InvalidOperationException("진행 중인 2형 저혈당 tell이 없다");

            _lowState = LowState.WaitingRelease;
            _suppressed = true;
            ActiveSignal = null;
        }

        /// <summary>HypoEventFsm이 재개 후 유예 시간을 마치면 호출한다. 다음 판정부터 기존 규칙으로 돌아간다.</summary>
        public void NotifyGraceEnded()
        {
            if (!_suppressed) throw new InvalidOperationException("유예 중이 아니다");

            _suppressed = false;
        }

        // 2형 판정(설계 4.3절). 순서: 해제 → tell 진행 중이면 끝 → notice 끝나면 tell → 억제 중이면 hold 0 → hold 누적 → notice 시작.
        private void EvaluateType2(double simDt)
        {
            var levels = _config.Levels;
            double actual = _model.Actual;

            if (_hyperState == HyperState.WaitingRelease && actual < levels.HyperReleaseLevel)
                _hyperState = HyperState.Armed;
            if (_lowState == LowState.WaitingRelease && actual >= levels.HypoReleaseLevel)
                _lowState = LowState.Armed;

            if (_hyperState == HyperState.Active || _lowState == LowState.Active) return;

            if (ActiveSignal.HasValue)
            {
                if (_model.SimTime >= _noticeEnd) TriggerTell();
                return;
            }

            if (_suppressed)
            {
                _highHold = 0;
                _lowHold = 0;
                return;
            }

            bool high = HighAllowed && _hyperState == HyperState.Armed && actual >= levels.HyperThreshold;
            bool low = LowAllowed && _lowState == LowState.Armed && actual < levels.HypoThreshold;
            _highHold = high ? _highHold + simDt : 0;
            _lowHold = low ? _lowHold + simDt : 0;

            if (high && _highHold >= _type2.HighHoldSec) StartNotice(NoticeKind.High);
            else if (low && _lowHold >= _type2.LowHoldSec) StartNotice(NoticeKind.Low);
        }

        private void StartNotice(NoticeKind kind)
        {
            _lastEventId++;
            ActiveSignal = kind;
            _highHold = 0;
            _lowHold = 0;
            _noticeEnd = _model.SimTime + _type2.NoticeSec;
            _bus.Publish(new NoticeStarted(_lastEventId, kind, _model.SimTime, _model.DecisionTime, _model.Actual));
            if (_type2.NoticeSec <= 0) TriggerTell();
        }

        private void TriggerTell()
        {
            if (ActiveSignal == NoticeKind.High)
            {
                _hyperState = HyperState.Active;
                _bus.Publish(new HyperEventTriggered(_lastEventId, _model.SimTime, _model.DecisionTime, _model.Actual));
            }
            else
            {
                _lowState = LowState.Active;
                _bus.Publish(new LowEventTriggered(_lastEventId, _model.SimTime, _model.DecisionTime, _model.Actual));
            }
        }

        private void EvaluateSuddenDrop(double simDt)
        {
            if (Gate != TriggerGate.All || _hypoState == HypoState.Active || _hyperState == HyperState.Active || _suppressed || simDt <= 0 ||
                _model.IsSuddenDropActive) return;

            // 1초당 확률을 simDt에 맞춰 환산해 프레임 길이와 무관하게 한다.
            double p = 1 - Math.Pow(1 - (_suddenDropChanceOverride ?? _config.SuddenDrop.ChancePerSec), simDt);
            if (p <= 0 || _random.NextDouble() >= p) return;

            _model.StartSuddenDrop();
            _bus.Publish(new SuddenDropStarted(_model.SimTime, _model.DecisionTime));
        }

        private void EvaluateHypo()
        {
            // 고혈당 대처 중에는 판정하지 않는다. 해제 수준 도달은 이어지는 유예 중에 기록된다.
            if (_hyperState == HyperState.Active) return;

            var levels = _config.Levels;
            double actual = _model.Actual;
            bool below = actual < levels.HypoThreshold;
            bool released = actual >= levels.HypoReleaseLevel;

            switch (_hypoState)
            {
                case HypoState.Armed:
                    if (below && LowAllowed) Trigger(false);
                    break;

                case HypoState.Active:
                    break;

                case HypoState.Cooldown:
                    if (released) _rearmedDuringCooldown = true;
                    // 유예 중에는 해제 수준 도달만 기록하고 판정은 유예가 끝난 뒤에 한다.
                    if (_suppressed || _model.SimTime < _cooldownEnd) break;

                    if (_rearmedDuringCooldown)
                    {
                        _hypoState = HypoState.Armed;
                        if (below && LowAllowed) Trigger(false);
                    }
                    else if (below)
                    {
                        // 막혀 있으면 cooldown에 머물며 열릴 때까지 다시 판정한다.
                        if (LowAllowed) Trigger(true);
                    }
                    else
                    {
                        _hypoState = HypoState.PendingRecurrence;
                    }
                    break;

                case HypoState.PendingRecurrence:
                    if (below) { if (LowAllowed) Trigger(true); }
                    else if (released) _hypoState = HypoState.Armed;
                    break;
            }
        }

        private void Trigger(bool isRecurrence)
        {
            _hypoState = HypoState.Active;
            _lastEventId++;
            _bus.Publish(new HypoEventTriggered(_lastEventId, isRecurrence, _model.SimTime, _model.DecisionTime,
                _model.Actual));
        }

        private void EvaluateHyper()
        {
            var levels = _config.Levels;
            double actual = _model.Actual;

            switch (_hyperState)
            {
                case HyperState.Armed:
                    if (!CanTriggerHyper()) break;

                    _hyperState = HyperState.Active;
                    _lastEventId++;
                    _bus.Publish(new HyperEventTriggered(_lastEventId, _model.SimTime, _model.DecisionTime, actual));
                    break;

                case HyperState.Active:
                    break;

                case HyperState.WaitingRelease:
                    if (actual < levels.HyperReleaseLevel) _hyperState = HyperState.Armed;
                    break;
            }
        }

        // 저혈당 대처 중이거나 유예 중이면 유예가 끝난 뒤 판정한다.
        private bool CanTriggerHyper() =>
            HighAllowed && _hyperState == HyperState.Armed && _model.Actual >= _config.Levels.HyperThreshold &&
            _hypoState != HypoState.Active && !_suppressed;

        private void EvaluateGaugeWarning()
        {
            bool on = IsDisplayedBelowWarning();
            if (on && !IsGaugeWarningOn)
                _bus.Publish(new GaugeWarningRaised(_model.SimTime, _model.DecisionTime, _model.Displayed));
            IsGaugeWarningOn = on;
        }

        private bool IsDisplayedBelowWarning() => _model.Displayed < _config.Display.WarningThreshold;
    }
}
