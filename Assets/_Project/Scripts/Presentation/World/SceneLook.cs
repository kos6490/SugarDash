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
            var subscription = bus.Subscribe<SceneEntered>(OnSceneEntered);
            return new Unbinder(() => subscription.Dispose());
        }

        private void OnSceneEntered(SceneEntered e)
        {
            if (ground != null) ground.material.color = GroundColorOf(e.Scene);
            // TODO(월드): 장면 id(e.Scene)별 맵 배경(운동장, 놀이터, 나들이, 집)을 켜고 끈다.
            // TODO(월드): 때(e.TimeOfDay)별 조명·하늘(아침, 오후, 저녁, 밤)을 바꾼다.
        }
    }
}
