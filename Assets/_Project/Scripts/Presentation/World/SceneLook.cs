using System;
using SugarDash.Domain.Events;
using UnityEngine;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 2구간 장면(맵 = 하루의 한 때)의 모습. SceneEntered를 받아 맵을 바꾼다. 빈 틀은 바닥 색만 바꾼다.
    /// 연출만이다. 세계·혈당을 바꾸지 않는다.
    /// </summary>
    // [빈 틀] 담당: 월드
    public sealed class SceneLook : MonoBehaviour
    {
        [Tooltip("장면마다 색을 바꿀 바닥. 비워 두면 아무것도 바꾸지 않는다.")]
        [SerializeField] private Renderer ground;
        [Tooltip("장면별 맵 배경을 켜고 끄는 컴포넌트. 비워 두면 배경을 바꾸지 않는다.")]
        [SerializeField] private SceneBackdropSwitcher backdrops;
        [Tooltip("때별 조명·하늘을 바꾸는 컴포넌트. 비워 두면 조명을 바꾸지 않는다.")]
        [SerializeField] private TimeOfDayLighting lighting;

        [Header("개발용(모델·조명 연결 전)")]
        [Tooltip("Backdrops가 비어 있으면 기본 도형으로 만든 개발용 배경(학교, 놀이 기구, 언덕, 집 등)을 보여 준다.")]
        [SerializeField] private bool showPlaceholderBackdrops = true;
        [Tooltip("Lighting이 비어 있으면 실행 중에 TimeOfDayLighting을 붙여 기본 때별 조명(주변광·안개)을 쓴다.")]
        [SerializeField] private bool addLightingIfMissing = true;
        [Tooltip("트랙(TrackSpawner). 연결하면 개발용 배경이 달린 거리만큼 뒤로 흘러간다. 비워 두면 배경이 멈춰 있다.")]
        [SerializeField] private TrackSpawner track;

        private Material _groundCopy;

        /// <summary>장면 id별 개발용 바닥 색. 모르는 id(장면 없는 구간 포함)는 회색.</summary>
        public static Color GroundColorOf(string sceneId)
        {
            switch (sceneId)
            {
                case "schoolyard": return new Color(0.80f, 0.62f, 0.42f);
                case "playground": return new Color(0.45f, 0.72f, 0.40f);
                case "outing": return new Color(0.35f, 0.60f, 0.80f);
                case "home": return new Color(0.30f, 0.27f, 0.45f);
                default: return Color.gray;
            }
        }

        /// <summary>GameBootstrapper가 연결한다. 반환값을 Dispose하면 연결을 해제한다.</summary>
        public IDisposable Bind(DomainEventBus bus)
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            // 구간에 들어가면 장면 없는 모습(바닥 색·배경·조명)으로 되돌린다. 장면 구간이면 바로 뒤의 SceneEntered가 장면 모습을 칠한다.
            var section = bus.Subscribe<SectionEntered>(OnSectionEntered);
            var scene = bus.Subscribe<SceneEntered>(OnSceneEntered);
            return new Unbinder(() =>
            {
                section.Dispose();
                scene.Dispose();
            });
        }

        private void OnSectionEntered(SectionEntered e)
        {
            SetGround(null);
            // 연결된 배경·조명이 없으면 개발용 배경(street)과 기본 조명으로 되돌린다.
            ShowBackdrop(null);
            ApplyLighting(null, true);
            _sceneShown = true;
            // 새 판(거리 0)이면 하루 번호도 처음으로. 판 중간의 구간 경계에서는 하루가 이어지므로 그대로 둔다.
            if (e.DistanceM <= 0) _lastDayIndex = -1;
        }

        private void OnSceneEntered(SceneEntered e)
        {
            SetGround(e.Scene);
            // 장면 id(e.Scene)별 맵 배경(운동장, 놀이터, 나들이, 집)을 켜고 끈다.
            ShowBackdrop(e.Scene);
            // 때(e.TimeOfDay)별 조명·하늘(아침, 오후, 저녁, 밤)을 바꾼다.
            // 다음 날 첫 장면은 수면 전환(암전) 중에 바뀌므로 섞지 않고 바로 바꾼다.
            bool newDay = e.DayIndex > _lastDayIndex && _lastDayIndex >= 0;
            ApplyLighting(TimeOfDayKey(e.TimeOfDay), newDay);
            _lastDayIndex = e.DayIndex;
            _sceneShown = true;
        }

        private void ShowBackdrop(string sceneId)
        {
            if (backdrops != null) { backdrops.Show(sceneId); return; }
            var placeholders = Placeholders();
            if (placeholders != null) placeholders.Show(sceneId);
        }

        // Backdrops가 비어 있을 때 쓰는 개발용 배경. 처음 부를 때 자식으로 만든다.
        private PlaceholderBackdrops Placeholders()
        {
            if (_placeholders == null && showPlaceholderBackdrops)
            {
                var go = new GameObject("Placeholder Backdrops");
                go.transform.SetParent(transform, false);
                _placeholders = go.AddComponent<PlaceholderBackdrops>();
            }
            return _placeholders;
        }

        // 시작부터(튜토리얼, 1구간) 장면 없는 배경과 기본 조명을 보여 준다.
        // 안개 등 조명 설정을 처음부터 켜 두어, 장면이 바뀌는 순간 셰이더가 새로 준비되며 화면이 번쩍이는 일을 줄인다.
        private void Start()
        {
            if (_sceneShown) return; // 게임이 먼저 구간·장면을 알려 줬으면(시작 순서는 정해져 있지 않다) 덮어쓰지 않는다.
            ShowBackdrop(null);
            ApplyLighting(null, true);
        }

        private void ApplyLighting(string key, bool immediate)
        {
            var target = lighting != null ? lighting : RuntimeLighting();
            if (target == null) return;
            if (immediate) target.ApplyImmediate(key);
            else target.Apply(key);
        }

        // Lighting이 비어 있을 때 실행 중에 이 오브젝트에 붙이는 조명. 처음 부를 때 만든다.
        private TimeOfDayLighting RuntimeLighting()
        {
            if (_runtimeLighting == null && addLightingIfMissing)
                _runtimeLighting = gameObject.AddComponent<TimeOfDayLighting>();
            return _runtimeLighting;
        }

        // 트랙이 연결되어 있으면 개발용 배경을 달린 거리만큼 뒤로 흘려보낸다.
        private void LateUpdate()
        {
            if (_placeholders != null && track != null) _placeholders.Scroll(track.Distance);
        }

        private PlaceholderBackdrops _placeholders;
        private TimeOfDayLighting _runtimeLighting;
        // 게임이 구간이나 장면을 한 번이라도 알려 줬는지. Start가 그 모습을 덮어쓰지 않게 한다.
        private bool _sceneShown;

        // 마지막으로 받은 하루 번호. 하루가 넘어가는 순간을 알아보는 데만 쓴다.
        private int _lastDayIndex = -1;

        /// <summary>설정 파일의 때 이름과 같은 소문자 키. TimeOfDayLighting 목록의 key와 맞춘다.</summary>
        private static string TimeOfDayKey(Domain.Config.TimeOfDay timeOfDay)
        {
            switch (timeOfDay)
            {
                case Domain.Config.TimeOfDay.Morning: return "morning";
                case Domain.Config.TimeOfDay.Afternoon: return "afternoon";
                case Domain.Config.TimeOfDay.Evening: return "evening";
                case Domain.Config.TimeOfDay.Night: return "night";
                default: return null;
            }
        }

        private void SetGround(string sceneId)
        {
            if (ground == null) return;
            // ground.material은 처음 쓸 때 머티리얼 사본을 만든다. 사본은 저절로 지워지지 않아 OnDestroy에서 지운다.
            if (_groundCopy == null) _groundCopy = ground.material;
            _groundCopy.color = GroundColorOf(sceneId);
        }

        private void OnDestroy()
        {
            if (_groundCopy != null) Destroy(_groundCopy);
        }
    }
}
