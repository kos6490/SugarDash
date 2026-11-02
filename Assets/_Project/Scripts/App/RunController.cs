using System;
using System.Collections.Generic;
using SugarDash.Domain.Config;
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
    /// 주행은 탈락할 때까지 끝없이 이어진다. 거리에 따라 설정의 구간이 차례로 열리고, 장애물에 부딪히면 하트를 잃는다.
    /// </summary>
    public sealed class RunController
    {
        // 달리는 동안 달리기 감소를 그대로 적용한다. 점프·슬라이드도 같다(동작별 차등 없음).
        private const double RunningActivity = 1;

        private readonly RunnerConfig _runner;
        private readonly IReadOnlyList<SectionConfig> _sections;
        private readonly Func<RunSession> _sessionFactory;
        private readonly ILogRepository _log;
        private readonly DomainEventBus _bus;
        private readonly Queue<Action<RunSession>> _commands = new Queue<Action<RunSession>>();

        private RunSession _session;
        private int _sectionIndex;
        // 지금 구간이 끝나는 누적 거리(m). 마지막 구간(lengthM 없음)이면 양의 무한대.
        private double _sectionEndM;

        /// <param name="sections">구간 순서. 마지막 구간만 lengthM이 없다(설정 검증에서 확인).</param>
        /// <param name="sessionFactory">주행마다 새 도메인 객체 묶음을 만든다. GameBootstrapper가 주입한다.</param>
        public RunController(RunnerConfig runner, IReadOnlyList<SectionConfig> sections,
            Func<RunSession> sessionFactory, ILogRepository log, DomainEventBus bus)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _sections = sections ?? throw new ArgumentNullException(nameof(sections));
            if (_sections.Count == 0) throw new ArgumentException("구간이 하나 이상 있어야 한다", nameof(sections));
            _sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public RunState State { get; private set; } = RunState.Idle;

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

        /// <summary>남은 하트. 주행을 닫은 뒤에도 마지막 값을 둔다.</summary>
        public int Hearts { get; private set; }

        /// <summary>지금 구간의 id. 열린 주행이 없으면 null.</summary>
        public string CurrentSectionId => _session == null ? null : _sections[_sectionIndex].Id;

        // HUD·패널이 읽는 값. 주행이 없으면 기본값.
        public double Displayed => _session?.Model.Displayed ?? 0;
        public double Actual => _session?.Model.Actual ?? 0;
        public bool IsGaugeWarningOn => _session?.Director.IsGaugeWarningOn ?? false;
        public HypoPhase Phase => _session?.Fsm.Phase ?? HypoPhase.Idle;
        public bool IsWorldFrozen => _session?.Fsm.IsWorldFrozen ?? false;
        /// <summary>3단계에서 양을 고를 간식.</summary>
        public string DoseSnackId => _session?.Fsm.DoseSnackId;

        /// <summary>지금 점수. 주행 중(일시정지 포함)이 아니면 0.</summary>
        public double Score => State == RunState.Idle || _session == null
            ? 0
            : _session.Scoring.Total(_session.Model.SimTime, DistanceM);

        public void StartRun()
        {
            if (State != RunState.Idle) throw new InvalidOperationException($"이미 주행 중이다 ({State})");

            _session = _sessionFactory() ?? throw new InvalidOperationException("세션 팩토리가 null을 반환했다");
            CurrentRunId = _log.BeginRun();
            _commands.Clear();
            WorldTime = 0;
            LastWorldDt = 0;
            RunCount++;
            State = RunState.Running;

            Hearts = _runner.Hearts;
            DistanceM = 0;
            _sectionIndex = 0;
            _sectionEndM = SectionLength(0);
            var model = _session.Model;
            _bus.Publish(new SectionEntered(_sections[0].Id, 0, model.SimTime, model.DecisionTime));
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
            _commands.Clear();
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

            var s = _session;
            while (_commands.Count > 0)
            {
                _commands.Dequeue()(s);
                if (CheckEliminated()) return;
            }

            double worldDt = s.Fsm.IsWorldFrozen ? 0 : dt;
            s.Model.Tick(dt, worldDt, dt, RunningActivity);
            WorldTime += worldDt;
            LastWorldDt = worldDt;
            s.Director.Evaluate(dt);
            s.Fsm.Update();

            CheckEliminated();
        }

        // 화면 명령. 주행이 열려 있을 때만 받아 다음 Tick에서 처리한다.

        public void PressMeter() => Enqueue(s => s.Fsm.PressMeter());

        public void ChooseSnack(string snackId) => Enqueue(s => s.Fsm.ChooseSnack(snackId));

        public void ChooseKeepRunning() => Enqueue(s => s.Fsm.ChooseKeepRunning());

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

            double servings = _runner.TrackSnackServings;
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
                while (DistanceM >= _sectionEndM && _sectionIndex < _sections.Count - 1)
                {
                    _sectionIndex++;
                    _sectionEndM += SectionLength(_sectionIndex);
                    _bus.Publish(new SectionEntered(_sections[_sectionIndex].Id, DistanceM, s.Model.SimTime,
                        s.Model.DecisionTime));
                }
            });
        }

        /// <summary>
        /// 장애물에 부딪혔다(PlayerController 충돌 판정). 하트를 하나 잃고, 모두 잃으면 탈락한다.
        /// 명령은 처리되는 시점에 판정한다. 세계가 멈춘 뒤 처리되면 무시하므로, 멈추기 직전 프레임에 쌓인 충돌은 버려진다(관대하게 처리).
        /// </summary>
        public void HitObstacle() => Enqueue(s =>
        {
            if (s.Fsm.IsWorldFrozen) return;

            Hearts--;
            _bus.Publish(new ObstacleHit(Hearts, DistanceM, s.Model.SimTime, s.Model.DecisionTime));
            if (Hearts <= 0) s.Fsm.EliminateHeartsLost();
        });

        /// <summary>i번째 구간의 길이. 마지막 구간(lengthM 없음)이면 양의 무한대라 그 뒤 경계는 없다.</summary>
        private double SectionLength(int index) => _sections[index].LengthM ?? double.PositiveInfinity;

        private void Enqueue(Action<RunSession> command)
        {
            if (State == RunState.Running || State == RunState.Paused) _commands.Enqueue(command);
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
