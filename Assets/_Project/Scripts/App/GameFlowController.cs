using System;
using System.Collections.Generic;
using SugarDash.Domain.Config;
using SugarDash.Domain.Metrics;
using SugarDash.Domain.Ports;
using SugarDash.Domain.Scoring;

namespace SugarDash.App
{
    /// <summary>
    /// 화면과 게임 상태 흐름: 유형 선택 → 타이틀 → (설정) → 주행 → 결과.
    /// 탈락하면 기록을 닫고, 닫힌 기록을 다시 읽어 결과를 계산한 뒤 결과 화면으로 간다.
    /// 결과도 기록과 같은 출처에서 계산한다(지표는 저장하지 않고 조회할 때 계산).
    /// 점수 설정·최고 점수 저장소·설정 버전은 방금 달린 유형의 프로필을 쓴다.
    /// 잘못된 화면에서 부른 명령은 무시한다.
    /// </summary>
    public sealed class GameFlowController
    {
        private readonly RunController _run;
        private readonly ILogQuery _query;
        private readonly ISettingsStore _settings;
        private readonly IReadOnlyDictionary<TrackType, TrackTypeProfile> _profiles;
        private readonly TutorialController _tutorial;
        private bool _begun;
        private bool _tutorialPending;

        public GameFlowController(RunController run, ILogQuery query, ISettingsStore settings,
            IReadOnlyDictionary<TrackType, TrackTypeProfile> profiles, TutorialController tutorial)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _query = query ?? throw new ArgumentNullException(nameof(query));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _tutorial = tutorial ?? throw new ArgumentNullException(nameof(tutorial));
            if (profiles == null) throw new ArgumentNullException(nameof(profiles));
            foreach (TrackType type in Enum.GetValues(typeof(TrackType)))
            {
                if (!profiles.ContainsKey(type) || profiles[type] == null)
                    throw new ArgumentException("유형 프로필이 없다: " + type, nameof(profiles));
            }
            _profiles = profiles;
        }

        /// <summary>지금 보여 주는 화면.</summary>
        public FlowScreen Screen { get; private set; } = FlowScreen.TypeSelect;

        /// <summary>고른 유형. 아직 고르지 않았으면 null.</summary>
        public TrackType? CurrentType { get; private set; }

        /// <summary>결과 화면을 보여 주는 중이다.</summary>
        public bool IsShowingResult => Screen == FlowScreen.Result;

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

        /// <summary>튜토리얼 안내와 상태.</summary>
        public TutorialController Tutorial => _tutorial;

        /// <summary>결과 화면이 튜토리얼 판의 결과다. 점수·최고 점수·요약은 모두 null이다.</summary>
        public bool IsTutorialResult { get; private set; }

        /// <summary>저장된 유형이 없으면 유형 선택, 있으면 타이틀로 시작한다. 주행은 바로 시작하지 않는다.</summary>
        public void Begin()
        {
            if (_begun) return;

            _begun = true;
            CurrentType = _settings.LoadTrackType();
            _tutorialPending = _settings.LoadTutorialPending();
            Screen = CurrentType == null ? FlowScreen.TypeSelect : FlowScreen.Title;
        }

        /// <summary>유형을 고르고 저장한 뒤 타이틀로 간다. 유형 선택·설정 화면에서만 받는다.</summary>
        public void SelectType(TrackType type)
        {
            // 나중에 이 앞에 보호자 확인 절차를 끼운다.
            if (Screen != FlowScreen.TypeSelect && Screen != FlowScreen.Settings) return;

            // 처음 고르거나 다른 유형으로 바꾸면 다음 시작하기가 튜토리얼 판이다. 같은 유형을 다시 고르면 바꾼 것이 아니다.
            if (CurrentType != type)
            {
                _tutorialPending = true;
                _settings.SaveTutorialPending(true);
            }
            _settings.SaveTrackType(type);
            CurrentType = type;
            Screen = FlowScreen.Title;
        }

        public void OpenSettings()
        {
            if (Screen == FlowScreen.Title) Screen = FlowScreen.Settings;
        }

        public void CloseSettings()
        {
            if (Screen == FlowScreen.Settings) Screen = FlowScreen.Title;
        }

        /// <summary>타이틀에서 고른 유형으로 주행을 시작한다.</summary>
        public void StartRun()
        {
            if (Screen != FlowScreen.Title || CurrentType == null) return;

            Screen = FlowScreen.Running;
            if (_tutorialPending) BeginTutorialRun(CurrentType.Value);
            else _run.StartRun(CurrentType.Value);
        }

        /// <summary>설정 화면에서 지금 유형의 튜토리얼 판을 바로 시작한다.</summary>
        public void StartTutorial()
        {
            if (Screen != FlowScreen.Settings || CurrentType == null) return;

            Screen = FlowScreen.Running;
            BeginTutorialRun(CurrentType.Value);
        }

        /// <summary>
        /// 건너뛰기: 튜토리얼 판을 닫고(기록 없음) 같은 유형의 기록하는 일반 판을 새로 연다.
        /// 안내 구간 중이고 이 유형의 튜토리얼을 끝낸 적이 있을 때만 받는다.
        /// </summary>
        public void SkipTutorial()
        {
            if (Screen != FlowScreen.Running || CurrentType == null) return;
            if (!_tutorial.IsGuided || !_tutorial.CanSkip) return;

            _tutorial.Stop();
            _run.EndRun();
            SetPending(false);
            _run.StartRun(CurrentType.Value);
        }

        private void BeginTutorialRun(TrackType type)
        {
            _run.StartRun(type, false);
            _tutorial.Begin(type, _settings.LoadTutorialDone(type), () =>
            {
                _settings.SaveTutorialDone(type);
                SetPending(false);
            });
        }

        private void SetPending(bool pending)
        {
            _tutorialPending = pending;
            _settings.SaveTutorialPending(pending);
        }

        /// <summary>GameBootstrapper가 RunController.Tick 뒤 매 프레임 부른다.</summary>
        public void Tick()
        {
            if (Screen != FlowScreen.Running) return;
            if (_run.State != RunState.Eliminated) return;

            if (!_run.IsRecorded)
            {
                // 튜토리얼 판: 기록·최고 점수를 건드리지 않고 점수 없는 결과 화면으로 간다.
                _tutorial.Stop();
                _run.EndRun();
                ClearResult();
                IsTutorialResult = true;
                Screen = FlowScreen.Result;
                return;
            }

            var profile = _profiles[_run.TrackType];
            string runId = _run.CurrentRunId;
            _run.EndRun();
            var record = runId == null ? null : _query.LoadRun(runId);
            LastResult = record == null ? null : MetricsCalculator.Summarize(record);
            UpdateScores(record, profile);
            Screen = FlowScreen.Result;
        }

        private void UpdateScores(Domain.Records.RunRecord record, TrackTypeProfile profile)
        {
            LastScore = record == null ? null : ScoringService.Calculate(record, profile.Score);
            double? prev = profile.Best.Load(profile.ConfigVersion);
            IsNewBest = LastScore != null && (prev == null || LastScore > prev);
            if (IsNewBest) profile.Best.Save(profile.ConfigVersion, LastScore.Value);
            BestScore = IsNewBest ? LastScore : prev;
        }

        /// <summary>결과 화면을 닫고 같은 유형으로 새 주행을 연다.</summary>
        public void Retry()
        {
            if (Screen != FlowScreen.Result || CurrentType == null) return;

            ClearResult();
            Screen = FlowScreen.Running;
            _run.StartRun(CurrentType.Value);
        }

        /// <summary>결과 화면을 닫고 타이틀로 간다.</summary>
        public void BackToTitle()
        {
            if (Screen != FlowScreen.Result) return;

            ClearResult();
            Screen = FlowScreen.Title;
        }

        private void ClearResult()
        {
            LastResult = null;
            LastScore = null;
            BestScore = null;
            IsNewBest = false;
            IsTutorialResult = false;
        }

        /// <summary>씬 파괴·앱 종료 시 열린 주행의 기록을 닫는다.</summary>
        public void Shutdown()
        {
            _tutorial.Stop();
            _run.EndRun();
        }
    }
}
