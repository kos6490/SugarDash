using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SugarDash.App;
using SugarDash.Domain.Config;
using SugarDash.Domain.Director;
using SugarDash.Domain.Events;
using SugarDash.Domain.Glucose;
using SugarDash.Domain.Hypo;
using SugarDash.Domain.Scoring;
using SugarDash.Infrastructure;
using SugarDash.Infrastructure.Config;
using SugarDash.Infrastructure.Logging;
using SugarDash.Infrastructure.Progress;
using SugarDash.Presentation;
using SugarDash.Presentation.Runner;
using UnityEngine;

namespace SugarDash.Bootstrap
{
    /// <summary>
    /// 조립 지점. 설정을 읽고 인프라 구현체를 포트에 연결한 뒤 서비스를 만들어 주입한다.
    /// 매 프레임 RunController를 진행하고, 씬이 파괴되면 기록을 닫고 구독을 모두 해제한다.
    /// </summary>
    public sealed class GameBootstrapper : MonoBehaviour
    {
        /// <summary>에디터 전용 개발 설정. 실제 게임 수치가 아니며 빌드에 포함하지 않는다.</summary>
        private const string DevConfigPath = "_Project/Config/Dev/game-config.dev.json";

        private const string DevConfigType2Path = "_Project/Config/Dev/game-config.type2.dev.json";

        [SerializeField] private TextAsset buildConfig;
        [SerializeField] private TextAsset buildConfigType2;
        [Tooltip("에디터에서 실행할 때 개발용 임시 설정(실제 수치 아님)을 쓴다. 빌드에서는 무시한다.")]
        [SerializeField] private bool useDevConfigInEditor = true;
        [SerializeField] private HudPresenter hud;
        [SerializeField] private HypoChoicePanel choicePanel;
        [SerializeField] private ResultScreenView resultScreen;
        [SerializeField] private PlayerController player;
        [SerializeField] private TrackSpawner track;
        [SerializeField] private InputAdapter input;
        [SerializeField] private SymptomEffectPresenter symptomEffect;
        [Header("빈 틀 화면(씬에 아직 없으면 비워 둔다)")]
        [SerializeField] private TitleScreenView titleScreen;
        [SerializeField] private TypeSelectView typeSelect;
        [SerializeField] private SettingsView settings;
        [SerializeField] private CharacterStateView characterState;
        [SerializeField] private NoticeSignalView noticeSignal;
        [SerializeField] private SceneLook sceneLook;
        [SerializeField] private SleepTransitionView sleepTransition;

        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private GameConfig _config;
        private GameConfig _config2;
        private Dictionary<TrackType, GameConfig> _configs;
        private DomainEventBus _bus;
        private RunController _run;
        private GameFlowController _flow;
        private int _shownRunCount;

        private void Awake()
        {
            if (!TryLoadConfig("1형", buildConfig, DevConfigPath, out _config) ||
                !TryLoadConfig("2형", buildConfigType2, DevConfigType2Path, out _config2))
            {
                enabled = false;
                return;
            }

            _configs = new Dictionary<TrackType, GameConfig>
            {
                { TrackType.Type1, _config },
                { TrackType.Type2, _config2 },
            };

            if (_config.TrackType != TrackType.Type1)
            {
                Debug.LogError("buildConfig(1형 칸)의 trackType이 Type1이 아니다.");
                enabled = false;
                return;
            }
            if (_config2.TrackType != TrackType.Type2)
            {
                Debug.LogError("buildConfigType2(2형 칸)의 trackType이 Type2가 아니다.");
                enabled = false;
                return;
            }

            _bus = new DomainEventBus();
            string logDirectory = Path.Combine(Application.persistentDataPath, "logs");
            var log = new LogRepository(logDirectory);
            _subscriptions.Add(log.Subscribe(_bus));

            _run = new RunController(CreateSession, log, _bus);
            var profiles = new Dictionary<TrackType, TrackTypeProfile>
            {
                {
                    TrackType.Type1, new TrackTypeProfile(_config.Score,
                        new BestScoreRepository(Path.Combine(Application.persistentDataPath, "best-score.json")),
                        _config.ConfigVersion)
                },
                {
                    TrackType.Type2, new TrackTypeProfile(_config2.Score,
                        new BestScoreRepository(Path.Combine(Application.persistentDataPath, "best-score.type2.json")),
                        _config2.ConfigVersion)
                },
            };
            _flow = new GameFlowController(_run, new ReportQueryService(logDirectory),
                new SettingsRepository(Path.Combine(Application.persistentDataPath, "settings.json")), profiles);

            // HUD 막대 범위는 1형 levels를 쓴다. 2형의 min/max가 다르면 주행마다 다시 묶어야 한다.
            _subscriptions.Add(hud.Bind(_run, _flow, _bus, _config.Glucose.Levels));
            _subscriptions.Add(choicePanel.Bind(_run, _bus, _config.Glucose.Snacks, Environment.TickCount));
            _subscriptions.Add(resultScreen.Bind(_flow));
            if (symptomEffect != null)
                _subscriptions.Add(symptomEffect.Bind(_run, _config.Symptom.ShakeStartGlucose, _config.Elimination.GlucoseLow));

            // 빈 틀 화면은 씬에 연결된 것만 묶는다. 해제는 구독 목록에서 한다.
            if (titleScreen == null) Debug.LogWarning("titleScreen이 비어 있다. 화면이 보이지 않는다. docs/dev-scene.md 6.6절을 본다.");
            if (typeSelect == null) Debug.LogWarning("typeSelect가 비어 있다. 유형 선택 화면이 보이지 않는다. docs/dev-scene.md 6.6절을 본다.");
            if (titleScreen != null) _subscriptions.Add(titleScreen.Bind(_flow));
            if (typeSelect != null) _subscriptions.Add(typeSelect.Bind(_flow));
            if (settings != null) _subscriptions.Add(settings.Bind(_flow));
            if (characterState != null) _subscriptions.Add(characterState.Bind(_run));
            if (noticeSignal != null) _subscriptions.Add(noticeSignal.Bind(_run));
            if (sceneLook != null) _subscriptions.Add(sceneLook.Bind(_bus));
            if (sleepTransition != null) _subscriptions.Add(sleepTransition.Bind(_bus));

            // 장면이 바뀌면 트랙 배치를 그 장면 값으로 바로 바꾼다. 발행 시점에 CurrentScene은 이미 새 장면이다.
            _subscriptions.Add(_bus.Subscribe<SceneEntered>(e =>
            {
                track.ChangeLayout(BuildLayout(_configs[_run.TrackType], _run.CurrentScene));
                track.SetScene(e.Scene);
            }));

            player.Init(_run, true);
            track.Init(BuildLayout(_config), Environment.TickCount, true);
            input.Init(_run, player);
        }

        private void Start() => _flow.Begin();

        /// <summary>
        /// 한 프레임의 순서를 여기서 고정한다: 도메인 진행 → 주행 종료 확인(결과 화면) → 캐릭터 → 트랙 → 충돌 판정.
        /// 러너 화면은 RunController가 정한 worldDt로만 움직인다.
        /// </summary>
        private void Update()
        {
            _run.Tick(Time.deltaTime);
            _flow.Tick();

            if (_run.RunCount != _shownRunCount)
            {
                _shownRunCount = _run.RunCount;
                player.ResetRun();
                // 첫 구간에 장면이 있으면 첫 장면 배치로 시작한다(전역 배치로 덮어쓰지 않는다).
                track.SetLayout(BuildLayout(_configs[_run.TrackType], _run.CurrentScene));
                track.ResetRun(Environment.TickCount);
                track.SetScene(_run.CurrentScene?.Id);
            }

            double worldDt = _run.LastWorldDt;
            player.Advance(worldDt);
            _run.AdvanceDistance(player.CurrentSpeed * worldDt);
            var phase = _run.Phase;
            bool obstaclesAllowed = phase != HypoPhase.ResumeCountdown && phase != HypoPhase.Grace;
            foreach (var item in track.Advance(worldDt, player.CurrentSpeed, obstaclesAllowed))
            {
                var outcome = player.Resolve(item);
                // 쉬는 곳(Rested)은 지나갈 때까지 트랙에 남긴다.
                if (outcome == PlayerController.Outcome.Collected || outcome == PlayerController.Outcome.Hit)
                    track.Remove(item);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (_run == null) return;

            if (paused) _run.Pause();
            else _run.Resume();
        }

        private void OnDestroy()
        {
            _flow?.Shutdown();
            for (int i = _subscriptions.Count - 1; i >= 0; i--) _subscriptions[i].Dispose();
            _subscriptions.Clear();
        }

        /// <summary>
        /// 설정에서 트랙 배치 값을 만든다. 2형은 음식 줄과 쉬는 곳을 놓고 1형은 간식만 놓는다.
        /// 장면이 있으면 그 장면의 덮어쓰기 값을 쓰고(없는 키는 전역 값), 물체를 놓지 않는 장면이면 배치를 끈다.
        /// </summary>
        public static TrackLayout BuildLayout(GameConfig config, SceneConfig scene = null)
        {
            var o = scene?.Overrides ?? SceneOverrides.None;
            bool spawns = scene?.Spawns ?? true;
            var t2 = config.Type2;
            if (t2 == null)
            {
                var ids = o.TrackSnackIds ?? config.Glucose.Snacks.Select(s => s.Id).ToList();
                return new TrackLayout(ids, o.TrackSnackIntervalMinSec ?? config.Runner.TrackSnackIntervalMinSec,
                    o.TrackSnackIntervalMaxSec ?? config.Runner.TrackSnackIntervalMaxSec, false, 0, 0, spawns);
            }
            var foods = config.Glucose.Snacks.Select(s => s.Id).ToList();
            return new TrackLayout(foods, o.FoodRowIntervalMinSec ?? t2.FoodRowIntervalMinSec,
                o.FoodRowIntervalMaxSec ?? t2.FoodRowIntervalMaxSec, true,
                o.RestIntervalMinSec ?? t2.RestIntervalMinSec, o.RestIntervalMaxSec ?? t2.RestIntervalMaxSec, spawns);
        }

        /// <summary>주행마다 그 유형의 설정으로 새 도메인 객체를 만들고 이벤트 발동을 상태 기계에 연결한다.</summary>
        private RunSession CreateSession(TrackType type)
        {
            var config = _configs[type];
            var model = new GlucoseModel(config.Glucose);
            var director = new EventDirector(config.Glucose, model,
                new SystemRandomSource(Environment.TickCount), _bus, config.Type2);
            var fsm = new HypoEventFsm(config, model, director, _bus, () => _run.DistanceM);
            var scoring = new ScoringService(config.Score);
            return new RunSession(config, model, director, fsm, scoring, _bus.Subscribe<HypoEventTriggered>(fsm.Begin),
                _bus.Subscribe<HyperEventTriggered>(fsm.BeginHyper), _bus.Subscribe<LowEventTriggered>(fsm.BeginLow),
                scoring.Subscribe(_bus));
        }

        private bool TryLoadConfig(string label, TextAsset buildAsset, string devPath, out GameConfig config)
        {
            config = null;
            try
            {
                string json = ReadConfigJson(buildAsset, devPath);
                if (json == null)
                {
                    Debug.LogError(label + " 설정 TextAsset이 연결되지 않았다. Bootstrapper의 " +
                                   (label == "1형" ? "buildConfig" : "buildConfigType2") + "에 설정 파일을 연결해야 한다.");
                    return false;
                }
                config = new ConfigProvider(json).Load();
                return true;
            }
            catch (ConfigValidationException e)
            {
                Debug.LogError(label + " 설정을 불러오지 못했다. 빈 값: " + string.Join(", ", e.MissingPaths) +
                               " / 위반: " + string.Join(" | ", e.Violations));
                config = null;
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError(label + " 설정을 읽지 못했다: " + e.GetType().Name + " " + e.Message);
                config = null;
                return false;
            }
        }

        private string ReadConfigJson(TextAsset buildAsset, string devPath)
        {
            if (useDevConfigInEditor && Application.isEditor)
            {
                Debug.LogWarning("개발용 임시 설정(실제 수치 아님)으로 실행한다: " + devPath);
                return ReadDevConfig(devPath);
            }
            return buildAsset != null ? buildAsset.text : null;
        }

        private static string ReadDevConfig(string devPath)
        {
#if UNITY_EDITOR
            return File.ReadAllText(Path.Combine(Application.dataPath, devPath));
#else
            throw new InvalidOperationException("개발용 설정은 에디터 전용이다");
#endif
        }
    }
}
