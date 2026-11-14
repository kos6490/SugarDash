using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 때별 조명·하늘을 바꾼다. SceneLook이 때 이름(morning, afternoon, evening, night, 장면 없는 구간은 null)을 넘긴다.
    /// 해(Directional Light), 주변광, 하늘, 카메라 배경색, 안개를 blendSec 동안 섞어 바꾼다. 연출만이다.
    /// 바꾼 RenderSettings는 이 오브젝트가 없어질 때 원래 값으로 되돌린다.
    /// </summary>
    // [영역 파일] 담당: 월드
    public sealed class TimeOfDayLighting : MonoBehaviour
    {
        [Tooltip("해 역할의 Directional Light. 비워 두면 Lighting 창의 Sun Source를 쓰고, 그것도 없으면 해는 바꾸지 않는다.")]
        [SerializeField] private Light sun;
        [Tooltip("켜 두면 주변광을 단색(Flat)으로 바꿔 때별 주변광 색이 바로 보이게 한다. 없어질 때 원래 방식으로 되돌린다.")]
        [SerializeField] private bool flatAmbient = true;
        [Tooltip("배경색을 바꿀 카메라. 비워 두면 배경색은 바꾸지 않는다.")]
        [SerializeField] private Camera targetCamera;
        [Tooltip("때별 값. key는 morning, afternoon, evening, night")]
        [SerializeField] private List<LightingPreset> presets = LightingPreset.CreateDefaults();
        [Tooltip("켜 두면 시작할 때 씬의 지금 조명을 아래 fallback 값으로 쓴다. 꺼 두면 아래 fallback 값(낮, 옅은 안개)을 쓴다.")]
        [SerializeField] private bool useSceneLightingAsFallback;
        [Tooltip("장면이 없는 구간(튜토리얼, 1구간)과 목록에 없는 때에 쓰는 값")]
        [SerializeField] private LightingPreset fallback = new LightingPreset
        {
            key = "default",
            sunColor = Color.white,
            sunIntensity = 1f,
            sunEuler = new Vector3(50f, -30f, 0f),
            ambientColor = new Color(0.66f, 0.66f, 0.68f),
            cameraBackground = new Color(0.55f, 0.75f, 0.95f),
            // 안개는 처음부터 켜 둔다. 중간에 켜면 그 순간 셰이더가 새로 준비되며 화면이 번쩍일 수 있다.
            fog = true,
            fogColor = new Color(0.70f, 0.82f, 0.95f),
            fogDensity = LightingPreset.LightFogDensity,
        };
        [Tooltip("다른 때로 바뀔 때 섞는 시간(초). 0이면 바로 바뀐다.")]
        [Min(0f)] [SerializeField] private float blendSec = 1.5f;

        [Header("샌드박스(혼자 시험할 때만. SceneLook이 Apply를 부르면 덮어쓴다)")]
        [Tooltip("비워 두지 않으면 시작할 때 이 때를 바로 적용한다. 예: night")]
        [SerializeField] private string sandboxKey;

        private LightingPreset _from;
        private LightingPreset _to;
        private LightingPreset _current;
        private float _elapsed;
        private bool _blending;
        private bool _applied;
        private SavedRenderSettings _saved;

        /// <summary>지금 목표로 하는 때 이름. 아직 적용한 적이 없으면 null.</summary>
        public string CurrentKey { get; private set; }

        /// <summary>때 이름에 맞는 값을 찾는다. 없으면 fallback.</summary>
        public LightingPreset PresetFor(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                for (int i = 0; i < presets.Count; i++)
                {
                    if (presets[i].key == key) return presets[i];
                }
            }
            return fallback;
        }

        /// <summary>때를 바꾼다. 같은 때를 다시 넘기면 아무것도 하지 않는다.</summary>
        public void Apply(string key)
        {
            if (_applied && CurrentKey == key) return;
            _applied = true;
            CurrentKey = key;
            _to = PresetFor(key);
            _from = _current;
            _elapsed = 0f;
            _blending = blendSec > 0f;
            if (!_blending) Write(_to);
        }

        /// <summary>섞지 않고 바로 바꾼다. 하루가 넘어가는 암전 중처럼 바로 보여야 할 때 쓴다.</summary>
        public void ApplyImmediate(string key)
        {
            _applied = true;
            CurrentKey = key;
            _to = PresetFor(key);
            _blending = false;
            _elapsed = 0f;
            Write(_to);
        }

        private void Awake()
        {
            _saved = SavedRenderSettings.Capture();
            // 씬 조명을 그대로 쓰는 fallback이면 시작할 때 아무것도 바꾸지 않는다.
            if (useSceneLightingAsFallback) fallback = CaptureSceneLighting();
            _current = fallback;
            if (!useSceneLightingAsFallback) Write(fallback);
        }

        /// <summary>씬의 지금 조명을 값 하나로 읽는다(fallback 키는 "default").</summary>
        private LightingPreset CaptureSceneLighting() => new LightingPreset
        {
            key = "default",
            sunColor = Sun != null ? Sun.color : fallback.sunColor,
            sunIntensity = Sun != null ? Sun.intensity : fallback.sunIntensity,
            sunEuler = Sun != null ? Sun.transform.rotation.eulerAngles : fallback.sunEuler,
            ambientColor = RenderSettings.ambientLight,
            skybox = RenderSettings.skybox,
            cameraBackground = targetCamera != null ? targetCamera.backgroundColor : fallback.cameraBackground,
            fog = RenderSettings.fog,
            fogColor = RenderSettings.fogColor,
            fogDensity = RenderSettings.fogDensity,
        };

        private void Start()
        {
            // 게임이 먼저 때를 알려 줬으면(시작 순서는 정해져 있지 않다) 덮어쓰지 않는다.
            if (!_applied && !string.IsNullOrEmpty(sandboxKey)) ApplyImmediate(sandboxKey);
        }

        private void Update()
        {
            if (!_blending) return;
            _elapsed += Time.deltaTime;
            float t = blendSec > 0f ? _elapsed / blendSec : 1f;
            Write(LightingPreset.Lerp(_from, _to, t));
            if (t >= 1f) _blending = false;
        }

        private void Write(LightingPreset p)
        {
            _current = p;
            var light = Sun;
            if (light != null)
            {
                light.color = p.sunColor;
                light.intensity = p.sunIntensity;
                light.transform.rotation = Quaternion.Euler(p.sunEuler);
            }
            if (UsesOwnAmbient(p))
            {
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = p.ambientColor;
            }
            else
            {
                _saved.RestoreAmbient();
            }
            if (targetCamera != null) targetCamera.backgroundColor = p.cameraBackground;
            if (p.skybox != null) RenderSettings.skybox = p.skybox;
            RenderSettings.fog = p.fog;
            RenderSettings.fogColor = p.fogColor;
            RenderSettings.fogDensity = p.fogDensity;
        }

        // 씬 조명을 읽어 둔 fallback은 씬의 주변광 방식(하늘 기반 등)을 그대로 둔다.
        private bool UsesOwnAmbient(LightingPreset p) =>
            flatAmbient && !(useSceneLightingAsFallback && p.key == SceneFallbackKey);

        private const string SceneFallbackKey = "default";

        // 연결한 해가 없으면 Lighting 창의 Sun Source(RenderSettings.sun)를 쓴다. 다른 오브젝트를 찾지는 않는다.
        private Light Sun => sun != null ? sun : RenderSettings.sun;

        private void OnDestroy() => _saved.Restore();

        /// <summary>RenderSettings는 씬 전체 값이라 이 컴포넌트가 바꾼 것을 되돌리기 위해 처음 값을 기억한다.</summary>
        private struct SavedRenderSettings
        {
            private AmbientMode _ambientMode;
            private Color _ambient;
            private Material _skybox;
            private bool _fog;
            private Color _fogColor;
            private float _fogDensity;

            public static SavedRenderSettings Capture() => new SavedRenderSettings
            {
                _ambientMode = RenderSettings.ambientMode,
                _ambient = RenderSettings.ambientLight,
                _skybox = RenderSettings.skybox,
                _fog = RenderSettings.fog,
                _fogColor = RenderSettings.fogColor,
                _fogDensity = RenderSettings.fogDensity,
            };

            public void RestoreAmbient()
            {
                RenderSettings.ambientMode = _ambientMode;
                RenderSettings.ambientLight = _ambient;
            }

            public void Restore()
            {
                RenderSettings.ambientMode = _ambientMode;
                RenderSettings.ambientLight = _ambient;
                RenderSettings.skybox = _skybox;
                RenderSettings.fog = _fog;
                RenderSettings.fogColor = _fogColor;
                RenderSettings.fogDensity = _fogDensity;
            }
        }
    }
}
