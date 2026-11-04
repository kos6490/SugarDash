using System;
using System.Collections.Generic;
using SugarDash.Domain.Config;
using SugarDash.Domain.Director;
using SugarDash.Domain.Events;
using SugarDash.Domain.Hypo;
using SugarDash.Domain.Ports;

namespace SugarDash.App
{
    /// <summary>
    /// 주행 진행. 매 프레임 경과 시간을 세 시계(worldDt, simDt, decisionDt)로 나눠 도메인에 전달한다
    /// (docs/glucose-model.md 2.1절).
    /// 화면의 명령은 큐에 쌓았다가 다음 <see cref="Tick"/> 맨 앞에서 처리한다. 명령은 지난 프레임의
    /// HypoEventFsm.Update가 끝난 시각에 처리되므로, 마감이 지난 답이 정상 답으로 받아지지 않는다.
    /// 주행은 탈락할 때까지 끝없이 이어진다. 거리에 따라 설정의 구간이 차례로 열리고, 장애물에 한 번 부딪히면 탈락한다.
    /// </summary>
    public sealed class RunController
    {
        // 달리는 동안 달리기 감소를 그대로 적용한다. 점프·슬라이드도 같다(동작별 차등 없음).
        private const double RunningActivity = 1;

        private readonly Func<TrackType, RunSession> _sessionFactory;
        private readonly ILogRepository _log;
        private readonly DomainEventBus _bus;
        private readonly Queue<Action<RunSession>> _commands = new Queue<Action<RunSession>>();

        private RunSession _session;
        private int _sectionIndex;
        // 지금 구간이 끝나는 누적 거리(m). 마지막 구간(lengthM 없음)이면 양의 무한대.
        private double _sectionEndM;
        private int _sceneIndex;
        private int _dayIndex;
        private double _sceneEndM;
        // 쉬는 중이 끝나는 worldDt 누적 시각.
        private double _restEnd;
        // 튜토리얼 멈춤(Pause/Resume과 따로 움직인다)과 풀린 뒤 3-2-1 남은 시간.
        private double _tutorialCountdown;

        /// <param name="sessionFactory">유형에 맞는 새 도메인 객체 묶음을 만든다. GameBootstrapper가 주입한다.
        /// runner·구간은 세션의 설정에서 읽는다(구간 0개 검사는 설정 검증이 맡는다).</param>
        public RunController(Func<TrackType, RunSession> sessionFactory, ILogRepository log, DomainEventBus bus)
        {
            _sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public RunState State { get; private set; } = RunState.Idle;

        /// <summary>마지막으로 시작한 주행의 유형. 처음엔 Type1.</summary>
        public TrackType TrackType { get; private set; } = TrackType.Type1;

        /// <summary>2형 쉬는 중. 달리기 감소가 멈춘다.</summary>
        public bool IsResting { get; private set; }

        /// <summary>지금 떠 있는 2형 신호(갈증·지침 등). 없으면 null.</summary>
        public NoticeKind? ActiveSignal => _session?.Director.ActiveSignal;

        private IReadOnlyList<SectionConfig> Sections => _session.Config.Sections;

        /// <summary>열린 주행의 runId(기록 파일 이름). 열린 주행이 없으면 null.</summary>
        public string CurrentRunId { get; private set; }

        /// <summary>worldDt 누적. 세계가 흐른 시간이다.</summary>
        public double WorldTime { get; private set; }

        /// <summary>
        /// 마지막 <see cref="Tick"/>에서 흐른 worldDt. 러너 화면(캐릭터, 트랙)은 이 값으로만 움직인다.
        /// 세계 정지·일시정지·주행 종료 중에는 0.
        /// </summary>
        public double LastWorldDt { get; private set; }

        /// <summary><see cref="StartRun"/>을 부를 때마다 1씩 는다. 화면이 새 주행을 알아채 초기화하는 데 쓴다.</summary>
        public int RunCount { get; private set; }

        /// <summary>이번 주행에서 달린 거리(m). <see cref="AdvanceDistance"/>로만 는다. 주행을 닫은 뒤에도 마지막 값을 둔다.</summary>
        public double DistanceM { get; private set; }

        /// <summary>지금 구간의 id. 열린 주행이 없으면 null.</summary>
        public string CurrentSectionId => _session == null ? null : Sections[_sectionIndex].Id;

        /// <summary>지금 장면. 장면 없는 구간이거나 열린 주행이 없으면 null.</summary>
        public SceneConfig CurrentScene =>
            _session == null || !Sections[_sectionIndex].HasScenes ? null : Sections[_sectionIndex].Scenes[_sceneIndex];

        /// <summary>장면 구간의 하루 번호(첫날 0). 장면이 없으면 0.</summary>
        public int DayIndex => CurrentScene == null ? 0 : _dayIndex;

        // HUD·패널이 읽는 값. 주행이 없으면 기본값.
        public double Displayed => _session?.Model.Displayed ?? 0;
        public double Actual => _session?.Model.Actual ?? 0;
        public bool IsGaugeWarningOn => _session?.Director.IsGaugeWarningOn ?? false;
        public HypoPhase Phase => _session?.Fsm.Phase ?? HypoPhase.Idle;
        public bool IsWorldFrozen => _session?.Fsm.IsWorldFrozen ?? false;
        /// <summary>선택 뒤 결과 문구 창이 열려 있다. 이 동안 패널은 문구만 보여 주고 선택 명령은 무시된다.</summary>
        public bool IsShowingAnswerFeedback => _session?.Fsm.IsShowingAnswerFeedback ?? false;
        /// <summary>재개 카운트다운의 남은 시간(초). 카운트다운 중이 아니면 0.</summary>
        public double ResumeCountdownRemaining =>
            _tutorialCountdown > 0 ? _tutorialCountdown : _session?.Fsm.ResumeCountdownRemaining ?? 0;
        /// <summary>3단계에서 양을 고를 간식.</summary>
        public string DoseSnackId => _session?.Fsm.DoseSnackId;

        /// <summary>지금 점수. 주행 중(일시정지 포함)이 아니면 0.</summary>
        public double Score => State == RunState.Idle || _session == null
            ? 0
            : _session.Scoring.Total(_session.Model.SimTime, DistanceM);

        /// <summary>이번 주행이 기록 파일을 남긴다. 튜토리얼 판은 false.</summary>
        public bool IsRecorded { get; private set; } = true;

        /// <summary>세션 설정. 열린 주행이 없으면 null.</summary>
        public GameConfig CurrentConfig => _session?.Config;

        /// <summary>튜토리얼 설명 창 때문에 멈춰 있다. 카운트다운 중에는 false.</summary>
        public bool IsTutorialHeld { get; private set; }

        /// <summary>튜토리얼 때문에 세계가 서 있다(설명 창, "다시 해봐요", 풀린 뒤 3-2-1). 이 동안 캐릭터 입력을 받지 않는다.</summary>
        public bool IsTutorialStopped => IsTutorialHeld || _tutorialCountdown > 0;

        /// <summary>튜토리얼 안내 주행. 장애물에 부딪혀도 탈락하지 않고 FSM이 연습 모드다.</summary>
        public bool IsTutorialGuided { get; private set; }

        public bool IsGlucoseMoveActive => _session?.Model.IsScriptedMoveActive ?? false;
        public int PracticeRetryCount => _session?.Fsm.PracticeRetryCount ?? 0;

        public void StartRun(TrackType type, bool recorded = true)
        {
            if (State != RunState.Idle) throw new InvalidOperationException($"이미 주행 중이다 ({State})");

            _session = _sessionFactory(type) ?? throw new InvalidOperationException("세션 팩토리가 null을 반환했다");
            if (recorded)
            {
                CurrentRunId = _log.BeginRun(_session.Config.ConfigVersion, _session.Config.TrackType);
            }
            else
            {
                _log.BeginUnrecordedRun();
                CurrentRunId = null;
            }
            IsRecorded = recorded;
            ClearTutorialState();
            TrackType = _session.Config.TrackType;
            IsResting = false;
            _commands.Clear();
            WorldTime = 0;
            LastWorldDt = 0;
            RunCount++;
            State = RunState.Running;

            DistanceM = 0;
            _sectionIndex = 0;
            _sectionEndM = SectionLength(0);
            _sceneIndex = 0;
            _dayIndex = 0;
            _sceneEndM = double.PositiveInfinity;
            var model = _session.Model;
            _bus.Publish(new SectionEntered(Sections[0].Id, 0, model.SimTime, model.DecisionTime));
            if (Sections[0].HasScenes) EnterScene(_session, 0, 0, 0);
            _session.Director.EvaluateTargetRange();
        }

        /// <summary>
        /// 주행을 닫고 기록 파일을 마감한다. 탈락 뒤 결과 화면으로 갈 때, 그리고 앱 종료 시 부른다.
        /// 열린 주행이 없으면 아무것도 하지 않는다.
        /// </summary>
        public void EndRun()
        {
            if (State == RunState.Idle) return;

            _session.Dispose();
            _session = null;
            IsResting = false;
            _commands.Clear();
            ClearTutorialState();
            _log.EndRun();
            CurrentRunId = null;
            State = RunState.Idle;
        }

        public void Pause()
        {
            if (State == RunState.Running) State = RunState.Paused;
        }

        public void Resume()
        {
            if (State == RunState.Paused) State = RunState.Running;
        }

        /// <param name="dt">이번 프레임의 실제 경과 시간(초).</param>
        public void Tick(double dt)
        {
            if (!(dt >= 0) || double.IsInfinity(dt))
                throw new ArgumentOutOfRangeException(nameof(dt), dt, "0 이상의 유한수여야 한다");
            LastWorldDt = 0;
            if (State != RunState.Running) return;

            if (IsTutorialHeld) return;
            if (_tutorialCountdown > 0)
            {
                _tutorialCountdown = Math.Max(0, _tutorialCountdown - dt);
                return;
            }

            var s = _session;
            while (_commands.Count > 0)
            {
                _commands.Dequeue()(s);
                if (CheckEliminated()) return;
            }

            double worldDt = s.Fsm.IsWorldFrozen ? 0 : dt;
            s.Model.Tick(dt, worldDt, dt, IsResting ? 0 : RunningActivity);
            WorldTime += worldDt;
            LastWorldDt = worldDt;
            s.Director.Evaluate(dt);
            s.Fsm.Update();
            if (IsResting && (WorldTime >= _restEnd || s.Fsm.IsWorldFrozen)) EndRest(s);

            CheckEliminated();
        }

        // 튜토리얼 제어. 큐에 넣지 않고 바로 적용한다. 주행이 없으면 아무것도 하지 않는다.

        /// <summary>설명 창을 띄우는 동안 세 시계와 명령 처리를 멈춘다. <see cref="Pause"/>/<see cref="Resume"/>으로는 풀리지 않는다.</summary>
        public void HoldForTutorial()
        {
            if (_session == null) return;

            IsTutorialHeld = true;
            _tutorialCountdown = 0;
        }

        /// <summary>설명 창을 닫는다. 멈춰 있지 않으면 아무것도 하지 않는다.</summary>
        /// <param name="countdownSec">풀린 뒤 시계가 다시 흐르기 전 3-2-1 길이(초).</param>
        public void ReleaseTutorialHold(double countdownSec)
        {
            if (_session == null || !IsTutorialHeld) return;

            IsTutorialHeld = false;
            _tutorialCountdown = Math.Max(0, countdownSec);
        }

        public void SetTutorialGuided(bool guided)
        {
            if (_session == null) return;

            IsTutorialGuided = guided;
            _session.Fsm.SetPractice(guided);
        }

        public void SetTriggerGate(TriggerGate gate) => _session?.Director.SetTriggerGate(gate);

        public void MoveGlucoseTo(double target, double durationSec) =>
            _session?.Model.StartScriptedMove(target, durationSec);

        public void ResetGlucose(double value) => _session?.Model.ResetTo(value);

        private void ClearTutorialState()
        {
            IsTutorialHeld = false;
            IsTutorialGuided = false;
            _tutorialCountdown = 0;
        }

        // 화면 명령. 주행이 열려 있을 때만 받아 다음 Tick에서 처리한다.

        public void PressMeter() => Enqueue(s => s.Fsm.PressMeter());

        public void ChooseSnack(string snackId) => Enqueue(s => s.Fsm.ChooseSnack(snackId));

        public void ChooseKeepRunning() => Enqueue(s => s.Fsm.ChooseKeepRunning());

        /// <summary>2형 tell의 "주스 마시기".</summary>
        public void ChooseJuice() => Enqueue(s => s.Fsm.ChooseJuice());

        /// <summary>
        /// 2형 쉬는 구간에서 앉아서 쉬기를 골랐다. 정해진 시간 동안 달리기 감소가 멈춘다.
        /// 1형이거나 세계가 멈췄거나 이미 쉬는 중이면 무시한다.
        /// </summary>
        public void StartRest() => Enqueue(s =>
        {
            if (s.Config.TrackType != TrackType.Type2 || s.Fsm.IsWorldFrozen || IsResting) return;

            IsResting = true;
            _restEnd = WorldTime + s.Config.Type2.RestDurationSec;
            _bus.Publish(new RestStarted(s.Model.Actual, s.Model.SimTime, s.Model.DecisionTime));
        });

        public void ChooseNoSugar() => Enqueue(s => s.Fsm.ChooseNoSugar());

        public void ChooseDose(DoseLevel level) => Enqueue(s => s.Fsm.ChooseDose(level));

        public void ChooseRecheck() => Enqueue(s => s.Fsm.ChooseRecheck());

        /// <param name="shownPhase">버튼을 누를 때 화면이 보여 주던 단계.</param>
        public void ChooseRunAgain(HypoPhase shownPhase) => Enqueue(s => s.Fsm.ChooseRunAgain(shownPhase));

        /// <param name="shownPhase">버튼을 누를 때 화면이 보여 주던 단계.</param>
        public void PressHelp(HypoPhase shownPhase) => Enqueue(s => s.Fsm.PressHelp(shownPhase));

        /// <summary>
        /// 트랙 간식을 주웠다(PlayerController 충돌 판정). 혈당에 소량 반영하고 대처 단계 판정과는 무관하다.
        /// 세계가 멈춘 동안에는 주울 수 없으므로 무시한다.
        /// </summary>
        public void PickUpSnack(string snackId) => Enqueue(s =>
        {
            if (s.Fsm.IsWorldFrozen) return;

            double servings = s.Config.Runner.TrackSnackServings;
            if (s.Config.TrackType == TrackType.Type2)
            {
                double before = s.Model.Actual;
                s.Model.EatSnack(snackId, servings);
                _bus.Publish(new FoodEaten(snackId, before, s.Model.SimTime, s.Model.DecisionTime));
                return;
            }
            s.Model.EatSnack(snackId, servings);
            _bus.Publish(new TrackSnackEaten(snackId, servings, s.Model.SimTime, s.Model.DecisionTime));
        });

        /// <summary>
        /// 캐릭터가 앞으로 간 거리를 더한다. 구간 경계를 넘으면 다음 구간에 들어간다(한 번에 여러 경계를 넘으면 차례로 모두).
        /// 명령은 처리되는 시점에 판정한다. 세계가 멈춘 뒤 처리되면 무시하므로, 멈추기 직전 프레임에 쌓인 거리는 버려진다(관대하게 처리).
        /// </summary>
        /// <param name="meters">0 이상의 유한수(m).</param>
        public void AdvanceDistance(double meters)
        {
            if (!(meters >= 0) || double.IsInfinity(meters))
                throw new ArgumentOutOfRangeException(nameof(meters), meters, "0 이상의 유한수여야 한다");

            Enqueue(s =>
            {
                if (s.Fsm.IsWorldFrozen) return;

                DistanceM += meters;
                while (DistanceM >= _sectionEndM && _sectionIndex < Sections.Count - 1)
                {
                    double boundary = _sectionEndM;
                    _sectionIndex++;
                    _sectionEndM += SectionLength(_sectionIndex);
                    _bus.Publish(new SectionEntered(Sections[_sectionIndex].Id, DistanceM, s.Model.SimTime,
                        s.Model.DecisionTime));
                    if (Sections[_sectionIndex].HasScenes) EnterScene(s, 0, 0, boundary);
                }

                // 장면 구간은 마지막 구간이라 장면은 끝없이 되풀이된다.
                var scenes = Sections[_sectionIndex].Scenes;
                while (scenes != null && DistanceM >= _sceneEndM)
                {
                    int next = (_sceneIndex + 1) % scenes.Count;
                    EnterScene(s, next, next == 0 ? _dayIndex + 1 : _dayIndex, _sceneEndM);
                }
            });
        }

        /// <summary>
        /// 장애물에 부딪혔다(PlayerController 충돌 판정). 바로 탈락한다. 튜토리얼 안내 주행이면 탈락하지 않는다.
        /// 명령은 처리되는 시점에 판정한다. 세계가 멈춘 뒤 처리되면 무시하므로, 멈추기 직전 프레임에 쌓인 충돌은 버려진다(관대하게 처리).
        /// 안내 주행은 부딪힌 때와 처리할 때 둘 다 본다. 안내 중에 쌓인 충돌이 안내를 마친 뒤 처리되어 탈락시키지 않게 한다.
        /// </summary>
        public void HitObstacle()
        {
            bool guidedAtHit = IsTutorialGuided;
            Enqueue(s =>
            {
                if (s.Fsm.IsWorldFrozen || guidedAtHit || IsTutorialGuided) return;

                s.Fsm.EliminateObstacleHit();
            });
        }

        /// <summary>
        /// 지금 구간의 index번째 장면에 들어간다. 장면 덮어쓰기를 적용하고 SceneEntered를 발행한다.
        /// 경계는 누적 거리라 한 번에 여러 경계를 넘어도 장면 길이가 어긋나지 않는다.
        /// </summary>
        /// <param name="boundaryM">이 장면이 시작하는 거리(m).</param>
        private void EnterScene(RunSession s, int index, int day, double boundaryM)
        {
            var scene = Sections[_sectionIndex].Scenes[index];
            _sceneIndex = index;
            _dayIndex = day;
            _sceneEndM = boundaryM + scene.LengthM.Value;
            s.Model.SetRunDecayPerSec(scene.Overrides.RunDecayPerSec);
            s.Director.SetSuddenDropChancePerSec(scene.Overrides.SuddenDropChancePerSec);
            _bus.Publish(new SceneEntered(scene.Id, scene.TimeOfDay, day, DistanceM, s.Model.SimTime,
                s.Model.DecisionTime));
        }

        /// <summary>i번째 구간의 길이. 마지막 구간(lengthM 없음)이면 양의 무한대라 그 뒤 경계는 없다.</summary>
        private double SectionLength(int index) => Sections[index].LengthM ?? double.PositiveInfinity;

        private void Enqueue(Action<RunSession> command)
        {
            if (State == RunState.Running || State == RunState.Paused) _commands.Enqueue(command);
        }

        private void EndRest(RunSession s)
        {
            IsResting = false;
            _bus.Publish(new RestEnded(s.Model.Actual, s.Model.SimTime, s.Model.DecisionTime));
        }

        private bool CheckEliminated()
        {
            if (_session.Fsm.Phase != HypoPhase.Eliminated) return false;

            _commands.Clear();
            State = RunState.Eliminated;
            return true;
        }
    }
}
