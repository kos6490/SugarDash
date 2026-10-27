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
    /// </summary>
    public sealed class RunController
    {
        // 달리는 동안 달리기 감소를 그대로 적용한다. 점프·슬라이드도 같다(동작별 차등 없음).
        private const double RunningActivity = 1;

        private readonly RunnerConfig _runner;
        private readonly Func<RunSession> _sessionFactory;
        private readonly ILogRepository _log;
        private readonly DomainEventBus _bus;
        private readonly Queue<Action<RunSession>> _commands = new Queue<Action<RunSession>>();

        private RunSession _session;

        /// <param name="sessionFactory">주행마다 새 도메인 객체 묶음을 만든다. GameBootstrapper가 주입한다.</param>
        public RunController(RunnerConfig runner, Func<RunSession> sessionFactory, ILogRepository log,
            DomainEventBus bus)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public RunState State { get; private set; } = RunState.Idle;

        /// <summary>worldDt 누적. 주행 길이를 잰다.</summary>
        public double WorldTime { get; private set; }

        // HUD·패널이 읽는 값. 주행이 없으면 기본값.
        public double Displayed => _session?.Model.Displayed ?? 0;
        public double Actual => _session?.Model.Actual ?? 0;
        public bool IsGaugeWarningOn => _session?.Director.IsGaugeWarningOn ?? false;
        public HypoPhase Phase => _session?.Fsm.Phase ?? HypoPhase.Idle;
        public bool IsWorldFrozen => _session?.Fsm.IsWorldFrozen ?? false;
        /// <summary>3단계에서 양을 고를 간식.</summary>
        public string DoseSnackId => _session?.Fsm.DoseSnackId;

        public void StartRun()
        {
            if (State != RunState.Idle) throw new InvalidOperationException($"이미 주행 중이다 ({State})");

            _session = _sessionFactory() ?? throw new InvalidOperationException("세션 팩토리가 null을 반환했다");
            _log.BeginRun();
            _commands.Clear();
            WorldTime = 0;
            State = RunState.Running;
        }

        /// <summary>
        /// 주행을 닫고 기록 파일을 마감한다. 탈락·완주 뒤 결과 화면으로 갈 때, 그리고 앱 종료 시 부른다.
        /// 열린 주행이 없으면 아무것도 하지 않는다.
        /// </summary>
        public void EndRun()
        {
            if (State == RunState.Idle) return;

            _session.Dispose();
            _session = null;
            _commands.Clear();
            _log.EndRun();
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
            s.Director.Evaluate(dt);
            s.Fsm.Update();

            if (CheckEliminated()) return;
            CheckCompleted();
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

        /// <summary>대처 중에는 끝내지 않는다. 진행 중인 이벤트의 기록이 잘리지 않게 대처를 마친 뒤 끝낸다.</summary>
        private void CheckCompleted()
        {
            if (WorldTime < _runner.RunDurationSec) return;
            var phase = _session.Fsm.Phase;
            if (phase != HypoPhase.Idle && phase != HypoPhase.Grace) return;

            var model = _session.Model;
            _bus.Publish(new RunCompleted(WorldTime, model.SimTime, model.DecisionTime));
            _commands.Clear();
            State = RunState.Completed;
        }
    }
}
