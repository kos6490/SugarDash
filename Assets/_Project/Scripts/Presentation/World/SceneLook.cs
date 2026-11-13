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
            if (backdrops != null) backdrops.Show(null);
            if (lighting != null) lighting.ApplyImmediate(null);
            // 새 판(거리 0)이면 하루 번호도 처음으로. 판 중간의 구간 경계에서는 하루가 이어지므로 그대로 둔다.
            if (e.DistanceM <= 0) _lastDayIndex = -1;
        }

        private void OnSceneEntered(SceneEntered e)
        {
            SetGround(e.Scene);
            // 장면 id(e.Scene)별 맵 배경(운동장, 놀이터, 나들이, 집)을 켜고 끈다.
            if (backdrops != null) backdrops.Show(e.Scene);
            // 때(e.TimeOfDay)별 조명·하늘(아침, 오후, 저녁, 밤)을 바꾼다.
            // 다음 날 첫 장면은 수면 전환(암전) 중에 바뀌므로 섞지 않고 바로 바꾼다.
            if (lighting == null) return;
            string key = TimeOfDayKey(e.TimeOfDay);
            if (e.DayIndex > _lastDayIndex && _lastDayIndex >= 0) lighting.ApplyImmediate(key);
            else lighting.Apply(key);
            _lastDayIndex = e.DayIndex;
        }

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
