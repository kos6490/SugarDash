using System;
using System.Collections.Generic;
using SugarDash.Domain.Config;
using SugarDash.Domain.Hypo;
using SugarDash.Domain.Metrics;
using SugarDash.Domain.Ports;
using SugarDash.Domain.Scoring;

namespace SugarDash.App
{
    /// <summary>
    /// 화면과 게임 상태 흐름: 유형 선택 → 타이틀 → (설정) → 주행 → 탈락 장면 → 결과.
    /// 탈락하면 주행을 연 채로 탈락 장면을 보여 주고, 장면이 끝나면 기록을 닫고 닫힌 기록을 다시 읽어 결과를 계산한 뒤 결과 화면으로 간다.
    /// 주행 중에는 일시정지 메뉴(계속하기, 타이틀로)를 연다.
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
        private readonly double _pauseCountdownSec;
        private readonly double _eliminationSceneSec;
        private bool _begun;
        private bool _tutorialPending;
        // 메뉴를 열 때 세계가 달리고 있었다. 계속하기에서 3-2-1을 보여 줄지 정한다.
        private bool _pausedWhileWorldRunning;
        private double _eliminationSceneRemaining;

        public GameFlowController(RunController run, ILogQuery query, ISettingsStore settings,
            IReadOnlyDictionary<TrackType, TrackTypeProfile> profiles, TutorialController tutorial,
            double pauseCountdownSec, double eliminationSceneSec)
        {
            if (!(pauseCountdownSec >= 0) || double.IsInfinity(pauseCountdownSec))
                throw new ArgumentOutOfRangeException(nameof(pauseCountdownSec), pauseCountdownSec, "0 이상의 유한수여야 한다");
            if (!(eliminationSceneSec >= 0) || double.IsInfinity(eliminationSceneSec))
                throw new ArgumentOutOfRangeException(nameof(eliminationSceneSec), eliminationSceneSec, "0 이상의 유한수여야 한다");
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
            _pauseCountdownSec = pauseCountdownSec;
            _eliminationSceneSec = eliminationSceneSec;
        }

        /// <summary>지금 보여 주는 화면.</summary>
        public FlowScreen Screen { get; private set; } = FlowScreen.TypeSelect;

        /// <summary>고른 유형. 아직 고르지 않았으면 null.</summary>
        public TrackType? CurrentType { get; private set; }

        /// <summary>탈락 장면을 보여 주는 중이다.</summary>
        public bool IsShowingElimination => Screen == FlowScreen.Elimination;

        /// <summary>탈락 장면에서 보여 줄 탈락 방식. 탈락 장면이 아니면 null.</summary>
        public EliminationCause? EliminatedBy { get; private set; }

        /// <summary>일시정지 메뉴가 떠 있다.</summary>
        public bool IsPauseMenuOpen { get; private set; }

        /// <summary>
        /// 일시정지 버튼을 보여 준다. 주행 화면에서 세계가 달리고 있을 때만 참이다(1단계, 유예, 2형 쉬는 중 포함).
        /// 대처·알리기 패널, 재개 카운트다운, 튜토리얼 설명 창이 떠 있으면 거짓이다.
        /// </summary>
        public bool CanPause => Screen == FlowScreen.Running && !IsPauseMenuOpen && _run.State == RunState.Running &&
                                !_run.IsWorldFrozen && !_run.IsTutorialStopped;

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
            if (Screen != FlowScreen.Running || CurrentType == null || IsPauseMenuOpen) return;
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

        /// <summary>
        /// 일시정지 메뉴를 열고 주행의 세 시계를 멈춘다. 일시정지 버튼은 <see cref="CanPause"/>일 때만 받는다.
        /// </summary>
        /// <param name="force">앱이 백그라운드로 갈 때 true. 패널이 떠 있어도 연다. 주행 중(탈락 전)일 때만 연다.</param>
        public void OpenPauseMenu(bool force = false)
        {
            if (Screen != FlowScreen.Running || IsPauseMenuOpen || _run.State != RunState.Running) return;
            if (!force && !CanPause) return;

            _pausedWhileWorldRunning = !_run.IsWorldFrozen && !_run.IsTutorialStopped;
            _run.Pause();
            IsPauseMenuOpen = true;
        }

        /// <summary>메뉴를 닫고 이어 간다. 세계가 달리던 중이었으면 3-2-1 뒤에 흐른다.</summary>
        public void ContinueFromPause()
        {
            if (!IsPauseMenuOpen) return;

            IsPauseMenuOpen = false;
            _run.Resume(_pausedWhileWorldRunning ? _pauseCountdownSec : 0);
        }

        /// <summary>
        /// 메뉴에서 판을 그만두고 타이틀로 간다. 기록 파일을 닫지만 결과·점수·최고 점수는 계산하지 않는다
        /// (탈락 기록이 없는 판으로 남는다). 튜토리얼 판이면 다음 시작하기도 튜토리얼 판이다.
        /// </summary>
        public void QuitToTitle()
        {
            if (!IsPauseMenuOpen) return;

            IsPauseMenuOpen = false;
            _tutorial.Stop();
            _run.EndRun();
            ClearResult();
            Screen = FlowScreen.Title;
        }

        /// <summary>GameBootstrapper가 RunController.Tick 뒤 매 프레임 부른다.</summary>
        /// <param name="dt">이번 프레임의 실제 경과 시간(초). 탈락 장면 길이를 센다.</param>
        public void Tick(double dt)
        {
            if (!(dt >= 0) || double.IsInfinity(dt))
                throw new ArgumentOutOfRangeException(nameof(dt), dt, "0 이상의 유한수여야 한다");

            if (Screen == FlowScreen.Running)
            {
                if (_run.State != RunState.Eliminated) return;

                // 탈락한 프레임의 dt는 주행에 이미 썼으므로 장면 시간은 다음 프레임부터 센다.
                Screen = FlowScreen.Elimination;
                EliminatedBy = _run.EliminatedBy;
                _eliminationSceneRemaining = _eliminationSceneSec;
            }
            else if (Screen == FlowScreen.Elimination)
            {
                _eliminationSceneRemaining -= dt;
            }
            else
            {
                return;
            }

            if (_eliminationSceneRemaining > 0) return;

            EliminatedBy = null;
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
            IsPauseMenuOpen = false;
            _tutorial.Stop();
            _run.EndRun();
        }
    }
}
