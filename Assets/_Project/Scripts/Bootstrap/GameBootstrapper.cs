using System;
using System.Collections.Generic;
using System.IO;
using SugarDash.App;
using SugarDash.Domain.Config;
using SugarDash.Domain.Director;
using SugarDash.Domain.Events;
using SugarDash.Domain.Glucose;
using SugarDash.Domain.Hypo;
using SugarDash.Infrastructure;
using SugarDash.Infrastructure.Config;
using SugarDash.Infrastructure.Logging;
using SugarDash.Presentation;
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

        [SerializeField] private TextAsset buildConfig;
        [Tooltip("에디터에서 실행할 때 개발용 임시 설정(실제 수치 아님)을 쓴다. 빌드에서는 무시한다.")]
        [SerializeField] private bool useDevConfigInEditor = true;
        [SerializeField] private HudPresenter hud;
        [SerializeField] private HypoChoicePanel choicePanel;

        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private GameConfig _config;
        private DomainEventBus _bus;
        private RunController _run;
        private GameFlowController _flow;

        private void Awake()
        {
            try
            {
                _config = new ConfigProvider(ReadConfigJson()).Load();
            }
            catch (ConfigValidationException e)
            {
                Debug.LogError("설정을 불러오지 못했다. 빈 값: " + string.Join(", ", e.MissingPaths) +
                               " / 위반: " + string.Join(" | ", e.Violations));
                enabled = false;
                return;
            }

            _bus = new DomainEventBus();
            var log = new LogRepository(Path.Combine(Application.persistentDataPath, "logs"), _config.ConfigVersion);
            _subscriptions.Add(log.Subscribe(_bus));

            _run = new RunController(_config.Runner, CreateSession, log, _bus);
            _flow = new GameFlowController(_run);

            _subscriptions.Add(hud.Bind(_run, _flow, _bus, _config.Glucose.Levels));
            _subscriptions.Add(choicePanel.Bind(_run, _bus, _config.Glucose.Snacks));
        }

        private void Start() => _flow.Begin();

        private void Update() => _run.Tick(Time.deltaTime);

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

        /// <summary>주행마다 새 도메인 객체를 만들고 저혈당 발동을 상태 기계에 연결한다.</summary>
        private RunSession CreateSession()
        {
            var model = new GlucoseModel(_config.Glucose);
            var director = new EventDirector(_config.Glucose, model,
                new SystemRandomSource(Environment.TickCount), _bus);
            var fsm = new HypoEventFsm(_config, model, director, _bus);
            return new RunSession(model, director, fsm, _bus.Subscribe<HypoEventTriggered>(fsm.Begin));
        }

        private string ReadConfigJson()
        {
            if (useDevConfigInEditor && Application.isEditor)
            {
                Debug.LogWarning("개발용 임시 설정(실제 수치 아님)으로 실행한다: " + DevConfigPath);
                return ReadDevConfig();
            }
            return buildConfig.text;
        }

        private static string ReadDevConfig()
        {
#if UNITY_EDITOR
            return File.ReadAllText(Path.Combine(Application.dataPath, DevConfigPath));
#else
            throw new InvalidOperationException("개발용 설정은 에디터 전용이다");
#endif
        }
    }
}
