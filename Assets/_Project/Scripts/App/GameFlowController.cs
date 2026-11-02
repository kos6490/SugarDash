using System;
using SugarDash.Domain.Config;
using SugarDash.Domain.Metrics;
using SugarDash.Domain.Ports;
using SugarDash.Domain.Scoring;

namespace SugarDash.App
{
    /// <summary>
    /// 화면과 게임 상태 흐름. 지금은 주행 화면과 결과 화면 둘만 다룬다.
    /// 탈락하면 기록을 닫고, 닫힌 기록을 다시 읽어 결과를 계산한 뒤 결과 화면으로 간다.
    /// 결과도 기록과 같은 출처에서 계산한다(지표는 저장하지 않고 조회할 때 계산).
    /// </summary>
    public sealed class GameFlowController
    {
        private readonly RunController _run;
        private readonly ILogQuery _query;

        private readonly ScoreConfig _score;
        private readonly IBestScoreStore _best;
        private readonly string _configVersion;

        public GameFlowController(RunController run, ILogQuery query, ScoreConfig score, IBestScoreStore best,
            string configVersion)
        {
            _score = score ?? throw new ArgumentNullException(nameof(score));
            _best = best ?? throw new ArgumentNullException(nameof(best));
            _configVersion = configVersion ?? throw new ArgumentNullException(nameof(configVersion));
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _query = query ?? throw new ArgumentNullException(nameof(query));
        }

        /// <summary>결과 화면을 보여 주는 중이다.</summary>
        public bool IsShowingResult { get; private set; }

        /// <summary>마지막으로 끝난 주행의 요약. 기록 파일을 찾지 못했으면 null.</summary>
        public RunSummary LastResult { get; private set; }

        /// <summary>마지막 주행의 점수. 탈락 기록이 없으면 null.</summary>
        public double? LastScore { get; private set; }

        /// <summary>결과 화면에 보여 줄 최고 점수(이번 점수 포함). 없으면 null.</summary>
        public double? BestScore { get; private set; }

        /// <summary>이번 점수가 최고 점수를 새로 넘었다.</summary>
        public bool IsNewBest { get; private set; }

        /// <summary>결과 화면에서 다시 할 수 있다.</summary>
        public bool CanRetry => IsShowingResult;

        public void Begin()
        {
            if (_run.State == RunState.Idle && !IsShowingResult) _run.StartRun();
        }

        /// <summary>GameBootstrapper가 RunController.Tick 뒤 매 프레임 부른다.</summary>
        public void Tick()
        {
            if (IsShowingResult) return;
            if (_run.State != RunState.Eliminated) return;

            string runId = _run.CurrentRunId;
            _run.EndRun();
            var record = runId == null ? null : _query.LoadRun(runId);
            LastResult = record == null ? null : MetricsCalculator.Summarize(record);
            UpdateScores(record);
            IsShowingResult = true;
        }

        private void UpdateScores(Domain.Records.RunRecord record)
        {
            LastScore = record == null ? null : ScoringService.Calculate(record, _score);
            double? prev = _best.Load(_configVersion);
            IsNewBest = LastScore != null && (prev == null || LastScore > prev);
            if (IsNewBest) _best.Save(_configVersion, LastScore.Value);
            BestScore = IsNewBest ? LastScore : prev;
        }

        /// <summary>결과 화면을 닫고 새 주행을 연다.</summary>
        public void Retry()
        {
            if (!IsShowingResult) return;

            IsShowingResult = false;
            LastResult = null;
            LastScore = null;
            BestScore = null;
            IsNewBest = false;
            _run.StartRun();
        }

        /// <summary>씬 파괴·앱 종료 시 열린 주행의 기록을 닫는다.</summary>
        public void Shutdown() => _run.EndRun();
    }
}
