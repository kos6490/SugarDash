using System;
using System.Collections.Generic;
using System.Linq;
using SugarDash.Domain.Config;
using SugarDash.Domain.Director;
using SugarDash.Domain.Events;
using SugarDash.Domain.Glucose;

namespace SugarDash.Domain.Hypo
{
    /// <summary>
    /// 저혈당 4단계 대처 상태 기계(docs/superpowers/specs/2026-10-20-hypo-event-fsm-design.md).
    /// 마감은 GlucoseModel의 DecisionTime(유예 시간만 SimTime) 기준 절대 시각으로 두고 <see cref="Update"/>에서 판정한다.
    /// 혈당에는 아동이 실제로 고른 간식과 양을 반영하고, 아동이 고른 것이 없을 때만 어른이 정답을 대신한다.
    /// </summary>
    public sealed class HypoEventFsm
    {
        private const string ChoiceMeter = "meter";
        private const string ChoiceKeepRunning = "keep_running";
        private const string ChoiceNoSugar = "no_sugar";
        private const string ChoiceHelp = "help";

        private readonly GameConfig _config;
        private readonly HypoConfig _hypo;
        private readonly Dictionary<string, SnackConfig> _snacks;
        private readonly GlucoseModel _model;
        private readonly EventDirector _director;
        private readonly DomainEventBus _bus;

        private double _phaseStart;
        private double _deadline;
        private bool _anyError;

        // 알리기 대기 중 상태
        private HypoStep _promptStep;
        private HypoErrorCode _promptError;
        private Action _afterNotify;

        public HypoEventFsm(GameConfig config, GlucoseModel model, EventDirector director, DomainEventBus bus)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _hypo = config.Hypo;
            _snacks = config.Glucose.Snacks.ToDictionary(s => s.Id);
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _director = director ?? throw new ArgumentNullException(nameof(director));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public HypoPhase Phase { get; private set; } = HypoPhase.Idle;
        /// <summary>진행 중인 이벤트 번호. Idle이면 0.</summary>
        public int EventId { get; private set; }
        /// <summary>3단계에서 양을 고를 간식. 2단계에서 아동이 고른 간식, 고르지 못했으면 어른이 건넨 빠른 당.</summary>
        public string DoseSnackId { get; private set; }

        /// <summary>true면 RunController가 worldDt를 0으로 둔다.</summary>
        public bool IsWorldFrozen
        {
            get
            {
                switch (Phase)
                {
                    case HypoPhase.Idle:
                    case HypoPhase.Recognize:
                    case HypoPhase.Grace:
                        return false;
                    default:
                        return true;
                }
            }
        }

        public bool IsInGrace => Phase == HypoPhase.Grace;

        public void Begin(HypoEventTriggered e)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            if (Phase != HypoPhase.Idle) throw new InvalidOperationException($"이미 진행 중이다 ({Phase})");

            EventId = e.EventId;
            DoseSnackId = null;
            _anyError = false;
            Enter(HypoPhase.Recognize, _hypo.Step1TimeLimitSec);
        }

        /// <summary>
        /// 매 프레임 GlucoseModel.Tick과 EventDirector.Evaluate 뒤에 호출한다.
        /// 한 번에 현재 단계의 마감 하나만 처리한다. 새 단계의 마감은 지금부터 잰다.
        /// </summary>
        public void Update()
        {
            if (Phase == HypoPhase.Idle || Phase == HypoPhase.Eliminated) return;
            if (_model.DecisionTime < _deadline) return;

            switch (Phase)
            {
                case HypoPhase.Recognize:
                    Error(HypoStep.Recognize, null, HypoErrorCode.SMissed, EnterRespond);
                    break;
                case HypoPhase.Respond:
                    DoseSnackId = _hypo.GuardianSnackId;
                    Error(HypoStep.Respond, null, HypoErrorCode.RsTimeout, EnterDose);
                    break;
                case HypoPhase.Dose:
                    Error(HypoStep.Dose, null, HypoErrorCode.DsTimeout, () =>
                    {
                        ApplyDose(_hypo.Doses.Proper, false);
                        EnterRemeasure();
                    });
                    break;
                case HypoPhase.GuardianPrompt:
                    Eliminate();
                    break;
            }
        }

        public void PressMeter()
        {
            switch (Phase)
            {
                case HypoPhase.Idle:
                    PublishMeter(0, false);
                    break;
                case HypoPhase.Recognize:
                    PublishMeter(EventId, false);
                    bool measuredBeforeWarning = !_director.IsGaugeWarningOn;
                    Correct(HypoStep.Recognize, ChoiceMeter, measuredBeforeWarning);
                    EnterRespond();
                    break;
                // 2~4단계와 알리기 대기 중에는 패널 UI가 측정기를 막는다.
            }
        }

        public bool ChooseSnack(string snackId)
        {
            if (Phase != HypoPhase.Respond) return false;
            if (snackId == null || !_snacks.TryGetValue(snackId, out var snack))
                throw new ArgumentException($"설정에 없는 간식이다: {snackId}", nameof(snackId));

            DoseSnackId = snackId;
            if (snack.Kind == SnackKind.Fast)
            {
                Correct(HypoStep.Respond, snackId);
                EnterDose();
            }
            else
            {
                Error(HypoStep.Respond, snackId, HypoErrorCode.RsSlow, EnterDose);
            }
            return true;
        }

        public bool ChooseKeepRunning() => ChooseNoSnack(ChoiceKeepRunning, HypoErrorCode.RsKeepRunning);

        public bool ChooseNoSugar() => ChooseNoSnack(ChoiceNoSugar, HypoErrorCode.RsNone);

        public bool ChooseDose(DoseLevel level)
        {
            if (Phase != HypoPhase.Dose) return false;

            double servings = _hypo.Doses.Servings(level);
            string choiceId = level.ToString().ToLowerInvariant();
            if (level == DoseLevel.Proper)
            {
                Correct(HypoStep.Dose, choiceId);
                ApplyDose(servings, true);
                EnterRemeasure();
            }
            else
            {
                var code = level == DoseLevel.Under ? HypoErrorCode.DsUnder : HypoErrorCode.DsOver;
                Error(HypoStep.Dose, choiceId, code, () =>
                {
                    ApplyDose(servings, true);
                    EnterRemeasure();
                });
            }
            return true;
        }

        public bool ChooseRecheck() => false;

        /// <summary>알리기 대기에서 "다시 달리기"를 고르면 탈락한다.</summary>
        public bool ChooseRunAgain()
        {
            if (Phase != HypoPhase.GuardianPrompt) return false;

            Eliminate();
            return true;
        }

        /// <summary>
        /// "어른에게 알리기" 버튼. 알리기 대기에서는 정답 행동이고,
        /// 2·3단계에서 선택 대신 누르면 HELP_INSTEAD 오답이지만 알리기 대기 없이 어른이 보정한다.
        /// </summary>
        public bool PressHelp()
        {
            switch (Phase)
            {
                case HypoPhase.GuardianPrompt:
                    Publish(new GuardianNotified(EventId, _promptStep, _model.SimTime, _model.DecisionTime));
                    var afterNotify = _afterNotify;
                    _afterNotify = null;
                    afterNotify();
                    return true;
                case HypoPhase.Respond:
                    DoseSnackId = _hypo.GuardianSnackId;
                    HelpInstead(HypoStep.Respond, EnterDose);
                    return true;
                case HypoPhase.Dose:
                    HelpInstead(HypoStep.Dose, () =>
                    {
                        ApplyDose(_hypo.Doses.Proper, false);
                        EnterRemeasure();
                    });
                    return true;
                default:
                    return false;
            }
        }

        private bool ChooseNoSnack(string choiceId, HypoErrorCode code)
        {
            if (Phase != HypoPhase.Respond) return false;

            DoseSnackId = _hypo.GuardianSnackId;
            Error(HypoStep.Respond, choiceId, code, EnterDose);
            return true;
        }

        private void EnterRespond() => Enter(HypoPhase.Respond, _hypo.Step2TimeLimitSec);

        private void EnterDose() => Enter(HypoPhase.Dose, _hypo.Step3TimeLimitSec);

        private void EnterRemeasure() => Enter(HypoPhase.Remeasure, _hypo.Step4TimeLimitSec);

        private void Enter(HypoPhase phase, double limitSec)
        {
            Phase = phase;
            _phaseStart = _model.DecisionTime;
            _deadline = _model.DecisionTime + limitSec;
        }

        private void Correct(HypoStep step, string choiceId, bool measuredBeforeWarning = false) =>
            PublishAnswer(step, choiceId, true, HypoErrorCode.None, false, measuredBeforeWarning);

        /// <summary>오류 공통 처리: 기록 → 위급 연출 → "어른에게 알리기" 대기.</summary>
        private void Error(HypoStep step, string choiceId, HypoErrorCode code, Action afterNotify)
        {
            _anyError = true;
            PublishAnswer(step, choiceId, false, code, false, false);
            Publish(new GuardianPromptStarted(EventId, step, code, _model.SimTime, _model.DecisionTime));

            _promptStep = step;
            _promptError = code;
            _afterNotify = afterNotify;
            Enter(HypoPhase.GuardianPrompt, _hypo.GuardianPromptTimeLimitSec);
        }

        private void HelpInstead(HypoStep step, Action afterNotify)
        {
            _anyError = true;
            PublishAnswer(step, ChoiceHelp, false, HypoErrorCode.HelpInstead, true, false);
            Publish(new GuardianNotified(EventId, step, _model.SimTime, _model.DecisionTime));
            afterNotify();
        }

        private void Eliminate()
        {
            Publish(new RunEliminated(EventId, _promptStep, _promptError, _model.SimTime, _model.DecisionTime));
            _afterNotify = null;
            Phase = HypoPhase.Eliminated;
        }

        private void ApplyDose(double servings, bool chosenByChild)
        {
            _model.EatSnack(DoseSnackId, servings);
            Publish(new DoseApplied(EventId, DoseSnackId, servings, chosenByChild, _model.SimTime,
                _model.DecisionTime));
        }

        private void PublishAnswer(HypoStep step, string choiceId, bool isCorrect, HypoErrorCode code,
            bool helpInstead, bool measuredBeforeWarning)
        {
            Publish(new StepAnswered(EventId, step, choiceId, isCorrect, code, helpInstead,
                _model.DecisionTime - _phaseStart, measuredBeforeWarning, _model.SimTime, _model.DecisionTime));
        }

        private void PublishMeter(int eventId, bool inGrace)
        {
            double actual = _model.Actual;
            bool unnecessary = actual >= _config.Glucose.Levels.HypoThreshold;
            Publish(new MeterPressed(eventId, actual, unnecessary, inGrace, _model.SimTime, _model.DecisionTime));
        }

        private void Publish<T>(T domainEvent) => _bus.Publish(domainEvent);
    }
}
