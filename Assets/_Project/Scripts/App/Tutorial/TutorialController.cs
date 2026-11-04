using System;
using System.Collections.Generic;
using SugarDash.Domain.Config;
using SugarDash.Domain.Director;
using SugarDash.Domain.Hypo;

namespace SugarDash.App
{
    /// <summary>
    /// 튜토리얼 판의 안내 순서와 상태(docs/superpowers/specs/2026-11-04-tutorial-design.md 4·5절).
    /// 대상이 다가옴 → 멈춤 → 설명 창 → "알겠어요" → 3-2-1 → 직접 해 봄 → 성공이면 다음 안내, 실패면 같은 안내를 다시 한다.
    /// 정답 판정은 하지 않는다. 대처 단계는 HypoEventFsm 연습 모드가 판정하고, 트랙 안내는 화면이 넘긴 결과만 본다.
    /// RunController.Tick 뒤에 <see cref="Tick"/>을 부른다.
    /// </summary>
    public sealed class TutorialController
    {
        private static readonly TutorialStep[] Type1Order =
        {
            TutorialStep.MoveLeft, TutorialStep.MoveRight, TutorialStep.Jump, TutorialStep.Slide, TutorialStep.Snack,
            TutorialStep.HypoRecognize, TutorialStep.HypoRespond, TutorialStep.HypoDose, TutorialStep.HypoRemeasure,
            TutorialStep.HypoNotify, TutorialStep.HyperNotify, TutorialStep.Finish,
        };

        private static readonly TutorialStep[] Type2Order =
        {
            TutorialStep.MoveLeft, TutorialStep.MoveRight, TutorialStep.Jump, TutorialStep.Slide, TutorialStep.FoodRow,
            TutorialStep.Rest, TutorialStep.HighNotice, TutorialStep.HighTell, TutorialStep.LowNotice,
            TutorialStep.LowTell, TutorialStep.Finish,
        };

        private static readonly int[] AllLanes = { -1, 0, 1 };

        private readonly RunController _run;
        private readonly double _glucoseMoveSec;
        private readonly double _retryTextSec;

        private IReadOnlyList<TutorialStep> _order;
        private int _index;
        private Action _onGuidedComplete;
        // 이번 Approach의 배치를 아직 내주지 않았다.
        private bool _placementPending;
        // 배치를 내준 뒤에만 NotifyTargetNear를 받는다.
        private bool _placementTaken;
        // 혈당 안내의 이번 이동 목표. 이동이 모자라면 모자란 만큼 더 멀리 잡는다.
        private double _moveAim;
        // Trying에 들어갈 때(또는 마지막 다시 하기 때)의 PracticeRetryCount.
        private int _retryBaseline;
        // 대처 단계 오답 뒤 결과 문구 창이 닫히기를 기다린다.
        private bool _retryAwaitingFeedback;
        private double _retryRemaining;

        /// <param name="glucoseMoveSec">혈당을 목표값까지 옮기는 시간(초, Inspector 값).</param>
        /// <param name="retryTextSec">"다시 해봐요" 문구를 보여 주는 시간(초, Inspector 값).</param>
        public TutorialController(RunController run, double glucoseMoveSec, double retryTextSec)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            if (!(glucoseMoveSec > 0) || double.IsInfinity(glucoseMoveSec))
                throw new ArgumentOutOfRangeException(nameof(glucoseMoveSec), glucoseMoveSec, "0보다 큰 유한수여야 한다");
            if (!(retryTextSec >= 0) || double.IsInfinity(retryTextSec))
                throw new ArgumentOutOfRangeException(nameof(retryTextSec), retryTextSec, "0 이상의 유한수여야 한다");
            _glucoseMoveSec = glucoseMoveSec;
            _retryTextSec = retryTextSec;
        }

        /// <summary>튜토리얼 판이다. 안내 구간을 마친 뒤(Done)에도 판이 끝날 때까지 true.</summary>
        public bool IsActive { get; private set; }

        /// <summary>안내 구간 중이다.</summary>
        public bool IsGuided => IsActive && Stage != TutorialStage.Done;

        public TutorialStage Stage { get; private set; } = TutorialStage.Inactive;

        /// <summary>지금 안내. 튜토리얼 판이 아니면 null. Done이면 Finish.</summary>
        public TutorialStep? Step => IsActive ? _order[_index] : (TutorialStep?)null;

        public TrackType Type { get; private set; }

        /// <summary>이 유형의 튜토리얼을 끝낸 적이 있어 "건너뛰기"를 보여 준다.</summary>
        public bool CanSkip { get; private set; }

        /// <summary>
        /// 안내 구간을 시작한다. <c>run.StartRun(type, false)</c> 뒤에 부른다. guided를 켜고 발동을 모두 막는다.
        /// </summary>
        /// <param name="onGuidedComplete">마무리 설명에서 "알겠어요"를 누를 때 한 번 부른다.</param>
        public void Begin(TrackType type, bool canSkip, Action onGuidedComplete)
        {
            if (_run.CurrentConfig == null) throw new InvalidOperationException("열린 주행이 없다");
            if (_run.TrackType != type)
                throw new ArgumentException($"주행 유형({_run.TrackType})과 다르다", nameof(type));

            IsActive = true;
            Type = type;
            CanSkip = canSkip;
            _onGuidedComplete = onGuidedComplete;
            _order = type == TrackType.Type2 ? Type2Order : Type1Order;
            _run.SetTutorialGuided(true);
            _run.SetTriggerGate(TriggerGate.None);
            EnterStep(0);
        }

        /// <summary>튜토리얼 상태만 지운다. 주행은 건드리지 않는다(건너뛰기·탈락·종료 때 EndRun 전에 부른다).</summary>
        public void Stop()
        {
            IsActive = false;
            Stage = TutorialStage.Inactive;
            _order = null;
            _index = 0;
            _onGuidedComplete = null;
            _placementPending = false;
            _placementTaken = false;
            _retryAwaitingFeedback = false;
            _retryRemaining = 0;
        }

        /// <param name="dt">이번 프레임의 실제 경과 시간(초). "다시 해봐요" 시간에만 쓴다.</param>
        public void Tick(double dt)
        {
            if (!(dt >= 0) || double.IsInfinity(dt))
                throw new ArgumentOutOfRangeException(nameof(dt), dt, "0 이상의 유한수여야 한다");
            if (!IsGuided || _run.State != RunState.Running) return;

            switch (Stage)
            {
                case TutorialStage.Approach:
                    TickApproach();
                    break;
                case TutorialStage.Trying:
                    TickTrying();
                    break;
                case TutorialStage.Retry:
                    TickRetry(dt);
                    break;
            }
        }

        /// <summary>설명 창의 "알겠어요". 설명 창이 떠 있을 때만 받는다.</summary>
        public void Acknowledge()
        {
            if (!IsGuided || Stage != TutorialStage.Prompt) return;

            var step = CurrentStep;
            if (step == TutorialStep.Finish)
            {
                _run.SetTriggerGate(TriggerGate.All);
                _run.SetTutorialGuided(false);
                Stage = TutorialStage.Done;
                _run.ReleaseTutorialHold(Countdown);
                _onGuidedComplete?.Invoke();
                return;
            }

            // 세계가 다시 달려야 하는 안내만 3-2-1을 넣는다. 멈춘 패널 앞에서는 바로 고른다.
            _run.ReleaseTutorialHold(IsPanel(step) ? 0 : Countdown);
            Stage = TutorialStage.Trying;
            _retryBaseline = _run.PracticeRetryCount;
            _retryAwaitingFeedback = false;
        }

        /// <summary>트랙 안내가 놓을 대상. 놓을 것이 없으면 null이고, Approach마다 한 번만 돌려준다.</summary>
        /// <param name="playerLane">캐릭터의 지금 레인(−1, 0, 1).</param>
        public TutorialPlacement TakePlacement(int playerLane)
        {
            if (!IsGuided || Stage != TutorialStage.Approach || !_placementPending) return null;

            _placementPending = false;
            _placementTaken = true;
            int p = Math.Max(-1, Math.Min(1, playerLane));
            switch (CurrentStep)
            {
                case TutorialStep.MoveLeft:
                    return Blocks(p == -1 ? 0 : p - 1);
                case TutorialStep.MoveRight:
                    return Blocks(p == 1 ? 0 : p + 1);
                case TutorialStep.Jump:
                    return new TutorialPlacement(TutorialObject.LowObstacle, AllLanes);
                case TutorialStep.Slide:
                    return new TutorialPlacement(TutorialObject.HighObstacle, AllLanes);
                case TutorialStep.Snack:
                    return new TutorialPlacement(TutorialObject.Snack, new[] { p == 0 ? 1 : 0 });
                case TutorialStep.Rest:
                    return new TutorialPlacement(TutorialObject.RestSpot, new[] { p == 0 ? 1 : 0 });
                case TutorialStep.FoodRow:
                    return new TutorialPlacement(TutorialObject.FoodRow, Array.Empty<int>());
                default:
                    _placementTaken = false;
                    return null;
            }
        }

        /// <summary>놓은 대상이 캐릭터 앞 멈춤 거리에 왔다. 세계를 멈추고 설명 창을 띄운다.</summary>
        public void NotifyTargetNear()
        {
            if (!IsGuided || Stage != TutorialStage.Approach || !IsTrack(CurrentStep) || !_placementTaken) return;

            // 트랙 안내 동안 발동은 막혀 있는데 혈당은 계속 내려간다. 이벤트 없이 흔들림·게이지 경고가 나오지 않게
            // 설명 창 아래에서 시작 값으로 되돌린다. 간식 안내의 간식은 이 뒤(Trying)에 먹으므로 그대로 반영된다.
            _run.ResetGlucose(_run.CurrentConfig.Glucose.Levels.Initial);
            EnterPrompt();
        }

        /// <summary>놓은 대상을 지나간 결과. 직접 해 보는 중에만 받는다.</summary>
        public void ReportAttempt(TutorialAttempt attempt)
        {
            if (!IsGuided || Stage != TutorialStage.Trying || !IsTrack(CurrentStep)) return;

            if (Succeeded(CurrentStep, attempt))
            {
                EnterStep(_index + 1);
                return;
            }
            // 장애물에 부딪혀도 guided라 탈락하지 않는다. 같은 대상을 다시 놓는다.
            StartRetryText();
        }

        private TutorialStep CurrentStep => _order[_index];

        private double Countdown => _run.CurrentConfig.Hypo.ResumeCountdownSec;

        private void EnterStep(int index)
        {
            _index = index;
            var step = CurrentStep;
            if (step == TutorialStep.Finish)
            {
                // 설명 창이 떠 있는 동안 되돌려 아이에게 튀어 보이지 않게 한다. 1형은 표시 혈당도 함께 맞춘다.
                _run.ResetGlucose(_run.CurrentConfig.Glucose.Levels.Initial);
                EnterPrompt();
                return;
            }

            Stage = TutorialStage.Approach;
            _placementPending = IsTrack(step);
            _placementTaken = false;
            if (IsGlucoseStep(step))
            {
                _run.SetTriggerGate(GateFor(step));
                _moveAim = Target(step);
                _run.MoveGlucoseTo(_moveAim, _glucoseMoveSec);
            }
        }

        private void EnterPrompt()
        {
            _run.HoldForTutorial();
            Stage = TutorialStage.Prompt;
            _retryAwaitingFeedback = false;
        }

        private void StartRetryText()
        {
            _run.HoldForTutorial();
            Stage = TutorialStage.Retry;
            _retryRemaining = _retryTextSec;
        }

        private void TickApproach()
        {
            var step = CurrentStep;
            if (!IsGlucoseStep(step)) return;

            if (TargetAppeared(step))
            {
                // 튜토리얼이 일으킨 이벤트 하나만 진행한다. 다른 발동은 다음 혈당 안내까지 막는다.
                _run.SetTriggerGate(TriggerGate.None);
                EnterPrompt();
                return;
            }

            if (_run.IsGlucoseMoveActive || _run.Phase != HypoPhase.Idle || _run.ActiveSignal.HasValue) return;

            // 이동이 끝났는데 발동하지 않았다(기본 하강·흡수 중인 간식 때문에 기준 밖으로 밀림).
            // 같은 시간 동안 같은 만큼 밀린다고 보고, 모자랐던 만큼 목표를 더 멀리 잡아 다시 옮긴다.
            // 목표는 탈락 안전 범위 안으로만 잡는다.
            var cfg = _run.CurrentConfig;
            double target = Target(step);
            _moveAim = Math.Max(cfg.Elimination.GlucoseLow,
                Math.Min(cfg.Elimination.GlucoseHigh, target + (_moveAim - _run.Actual)));
            _run.MoveGlucoseTo(_moveAim, _glucoseMoveSec);
        }

        private void TickTrying()
        {
            var step = CurrentStep;
            if (IsTrack(step)) return;

            // 다음 안내가 이 단계에서 이어지는 패널이면, 그 Phase가 열리고 결과 문구 창이 닫힐 때 설명 창을 띄운다.
            if (_index + 1 < _order.Count && IsChainedPanel(_order[_index + 1]) &&
                _run.Phase == PanelPhase(_order[_index + 1]) && !_run.IsShowingAnswerFeedback)
            {
                _index++;
                EnterPrompt();
                return;
            }

            if (IsPanel(step))
            {
                if (_run.PracticeRetryCount > _retryBaseline)
                {
                    _retryBaseline = _run.PracticeRetryCount;
                    if (IsNotifyPhase(PanelPhase(step)))
                    {
                        // 고혈당·저혈당 알리기는 결과 문구 창이 없으므로 "다시 해봐요"를 직접 보여 준다.
                        StartRetryText();
                        return;
                    }
                    _retryAwaitingFeedback = true;
                }
                if (_retryAwaitingFeedback && !_run.IsShowingAnswerFeedback)
                {
                    EnterPrompt();
                    return;
                }
            }

            // 대처를 마치고 재개 카운트다운·유예까지 끝났다.
            if (IsEventEnd(step) && _run.Phase == HypoPhase.Idle) EnterStep(_index + 1);
        }

        private void TickRetry(double dt)
        {
            _retryRemaining -= dt;
            if (_retryRemaining > 0) return;

            if (IsTrack(CurrentStep))
            {
                _run.ReleaseTutorialHold(0);
                EnterStep(_index);
            }
            else
            {
                EnterPrompt();
            }
        }

        private bool TargetAppeared(TutorialStep step)
        {
            switch (step)
            {
                case TutorialStep.HypoRecognize: return _run.Phase == HypoPhase.Recognize;
                case TutorialStep.HyperNotify: return _run.Phase == HypoPhase.HyperNotify;
                case TutorialStep.HighNotice: return _run.ActiveSignal == NoticeKind.High;
                case TutorialStep.LowNotice: return _run.ActiveSignal == NoticeKind.Low;
                default: return false;
            }
        }

        private double Target(TutorialStep step) =>
            GateFor(step) == TriggerGate.LowOnly
                ? TutorialTargets.Low(_run.CurrentConfig)
                : TutorialTargets.High(_run.CurrentConfig);

        private static TutorialPlacement Blocks(int open)
        {
            var lanes = new List<int>(2);
            foreach (int lane in AllLanes)
            {
                if (lane != open) lanes.Add(lane);
            }
            return new TutorialPlacement(TutorialObject.LaneBlock, lanes);
        }

        private static bool Succeeded(TutorialStep step, TutorialAttempt attempt)
        {
            switch (step)
            {
                case TutorialStep.Snack:
                case TutorialStep.FoodRow:
                    return attempt.Collected;
                case TutorialStep.Rest:
                    return attempt.Rested;
                default:
                    return !attempt.Hit;
            }
        }

        /// <summary>트랙에 대상을 놓는 안내.</summary>
        private static bool IsTrack(TutorialStep step) => step <= TutorialStep.Rest;

        /// <summary>혈당을 옮겨 이벤트를 일으키는 안내.</summary>
        private static bool IsGlucoseStep(TutorialStep step) =>
            step == TutorialStep.HypoRecognize || step == TutorialStep.HyperNotify ||
            step == TutorialStep.HighNotice || step == TutorialStep.LowNotice;

        private static TriggerGate GateFor(TutorialStep step) =>
            step == TutorialStep.HypoRecognize || step == TutorialStep.LowNotice
                ? TriggerGate.LowOnly
                : TriggerGate.HighOnly;

        /// <summary>세계가 멈춘 패널에서 고르는 안내. 설명 창을 닫으면 3-2-1 없이 바로 패널이 나온다.</summary>
        private static bool IsPanel(TutorialStep step)
        {
            switch (step)
            {
                case TutorialStep.HypoRespond:
                case TutorialStep.HypoDose:
                case TutorialStep.HypoRemeasure:
                case TutorialStep.HypoNotify:
                case TutorialStep.HyperNotify:
                case TutorialStep.HighTell:
                case TutorialStep.LowTell:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>앞 안내의 Trying에서 Phase가 열리면 이어지는 패널 안내(Approach가 없다).</summary>
        private static bool IsChainedPanel(TutorialStep step) => IsPanel(step) && step != TutorialStep.HyperNotify;

        private static HypoPhase PanelPhase(TutorialStep step)
        {
            switch (step)
            {
                case TutorialStep.HypoRespond: return HypoPhase.Respond;
                case TutorialStep.HypoDose: return HypoPhase.Dose;
                case TutorialStep.HypoRemeasure: return HypoPhase.Remeasure;
                case TutorialStep.HypoNotify: return HypoPhase.Notify;
                case TutorialStep.HyperNotify:
                case TutorialStep.HighTell: return HypoPhase.HyperNotify;
                case TutorialStep.LowTell: return HypoPhase.LowNotify;
                default: return HypoPhase.Idle;
            }
        }

        private static bool IsNotifyPhase(HypoPhase phase) =>
            phase == HypoPhase.HyperNotify || phase == HypoPhase.LowNotify;

        /// <summary>대처 이벤트의 마지막 안내. Phase가 Idle로 돌아오면 다음 안내로 간다.</summary>
        private static bool IsEventEnd(TutorialStep step) =>
            step == TutorialStep.HypoNotify || step == TutorialStep.HyperNotify ||
            step == TutorialStep.HighTell || step == TutorialStep.LowTell;
    }
}
