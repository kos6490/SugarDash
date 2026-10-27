namespace SugarDash.App
{
    /// <summary>
    /// 화면과 게임 상태 흐름. 지금은 주행 화면 하나만 다룬다: 시작하면 주행을 열고,
    /// 탈락·완주 뒤에는 "다시 하기"를 기다린다.
    /// </summary>
    public sealed class GameFlowController
    {
        private readonly RunController _run;

        public GameFlowController(RunController run) =>
            _run = run ?? throw new System.ArgumentNullException(nameof(run));

        /// <summary>탈락하거나 완주해서 다시 할 수 있다.</summary>
        public bool CanRetry => _run.State == RunState.Eliminated || _run.State == RunState.Completed;

        public void Begin()
        {
            if (_run.State == RunState.Idle) _run.StartRun();
        }

        /// <summary>끝난 주행의 기록을 닫고 새 주행을 연다.</summary>
        public void Retry()
        {
            if (!CanRetry) return;

            _run.EndRun();
            _run.StartRun();
        }

        /// <summary>씬 파괴·앱 종료 시 열린 주행의 기록을 닫는다.</summary>
        public void Shutdown() => _run.EndRun();
    }
}
