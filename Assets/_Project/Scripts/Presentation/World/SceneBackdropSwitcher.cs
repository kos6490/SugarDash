using System.Collections.Generic;
using UnityEngine;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 장면별 맵 배경(운동장, 놀이터, 나들이, 집)을 켜고 끈다. 배경은 씬 안에 미리 놓아 둔 오브젝트이고,
    /// 한 번에 하나만 켠다. SceneLook이 장면 id(장면 없는 구간은 null)를 넘긴다. 연출만이다.
    /// </summary>
    // [영역 파일] 담당: 월드
    public sealed class SceneBackdropSwitcher : MonoBehaviour
    {
        [Tooltip("장면 id(schoolyard, playground, outing, home)별 배경 루트 오브젝트")]
        [SerializeField] private List<WorldKeyedObject> backdrops = new List<WorldKeyedObject>();
        [Tooltip("장면이 없는 구간(튜토리얼, 1구간)과 목록에 없는 장면에 켜는 배경. 비워 두면 모두 끈다.")]
        [SerializeField] private GameObject defaultBackdrop;

        [Header("샌드박스(혼자 시험할 때만. SceneLook이 Show를 부르면 덮어쓴다)")]
        [Tooltip("비워 두지 않으면 시작할 때 이 장면 배경을 켠다. 예: playground")]
        [SerializeField] private string sandboxSceneId;

        /// <summary>지금 켜 둔 배경. 없으면 null.</summary>
        public GameObject Active { get; private set; }

        /// <summary>장면 id의 배경만 켜고 나머지는 끈다.</summary>
        public void Show(string sceneId)
        {
            _shown = true;
            var target = WorldKeyedObject.Find(backdrops, sceneId);
            if (target == null) target = defaultBackdrop;

            for (int i = 0; i < backdrops.Count; i++) SetActive(backdrops[i].target, backdrops[i].target == target);
            SetActive(defaultBackdrop, defaultBackdrop == target);
            Active = target;
        }

        private bool _shown;

        private void Start()
        {
            // 게임이 먼저 장면을 알려 줬으면(시작 순서는 정해져 있지 않다) 덮어쓰지 않는다.
            if (_shown) return;
            Show(string.IsNullOrEmpty(sandboxSceneId) ? null : sandboxSceneId);
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active) go.SetActive(active);
        }
    }
}
