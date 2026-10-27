using System;
using SugarDash.Domain.Config;
using SugarDash.Domain.Events;
using SugarDash.Domain.Glucose;
using SugarDash.Domain.Ports;

namespace SugarDash.Domain.Director
{
    /// <summary>
    /// 급하강, 저혈당 이벤트 발생, 게이지 경고를 결정한다(docs/glucose-model.md 1.2·4절).
    /// 혈당 계산은 GlucoseModel이 하고, 여기서는 판정과 이벤트 발행만 한다.
    /// 이벤트 종료부터 유예 시간 끝까지는 발동과 급하강을 억제한다.
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

        private readonly GlucoseConfig _config;
        private readonly GlucoseModel _model;
        private readonly IRandomSource _random;
        private readonly DomainEventBus _bus;

        private HypoState _hypoState = HypoState.Armed;
        private double _cooldownEnd;
        private bool _rearmedDuringCooldown;
        private bool _suppressed;
        private int _lastEventId;

        public EventDirector(GlucoseConfig config, GlucoseModel model, IRandomSource random, DomainEventBus bus)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            IsGaugeWarningOn = IsDisplayedBelowWarning();
        }

        public bool IsHypoEventActive => _hypoState == HypoState.Active;

        /// <summary>표시 혈당이 경고선 아래인 동안 true. 히스테리시스는 두지 않는다.</summary>
        public bool IsGaugeWarningOn { get; private set; }

        /// <summary>RunController가 GlucoseModel.Tick 직후 호출한다.</summary>
        /// <param name="simDt">이번 Tick에서 흐른 simTime.</param>
        public void Evaluate(double simDt)
        {
            if (!(simDt >= 0)) throw new ArgumentOutOfRangeException(nameof(simDt), simDt, "0 이상이어야 한다");

            EvaluateSuddenDrop(simDt);
            EvaluateHypo();
            EvaluateGaugeWarning();
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

        /// <summary>HypoEventFsm이 재개 후 유예 시간을 마치면 호출한다. 다음 판정부터 기존 규칙으로 돌아간다.</summary>
        public void NotifyGraceEnded()
        {
            if (!_suppressed) throw new InvalidOperationException("유예 중이 아니다");

            _suppressed = false;
        }

        private void EvaluateSuddenDrop(double simDt)
        {
            if (_hypoState == HypoState.Active || _suppressed || simDt <= 0 || _model.IsSuddenDropActive) return;

            // 1초당 확률을 simDt에 맞춰 환산해 프레임 길이와 무관하게 한다.
            double p = 1 - Math.Pow(1 - _config.SuddenDrop.ChancePerSec, simDt);
            if (p <= 0 || _random.NextDouble() >= p) return;

            _model.StartSuddenDrop();
            _bus.Publish(new SuddenDropStarted(_model.SimTime, _model.DecisionTime));
        }

        private void EvaluateHypo()
        {
            var levels = _config.Levels;
            double actual = _model.Actual;
            bool below = actual < levels.HypoThreshold;
            bool released = actual >= levels.HypoReleaseLevel;

            switch (_hypoState)
            {
                case HypoState.Armed:
                    if (below) Trigger(false);
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
                        if (below) Trigger(false);
                    }
                    else if (below)
                    {
                        Trigger(true);
                    }
                    else
                    {
                        _hypoState = HypoState.PendingRecurrence;
                    }
                    break;

                case HypoState.PendingRecurrence:
                    if (below) Trigger(true);
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
