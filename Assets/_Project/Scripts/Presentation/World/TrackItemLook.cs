using System.Collections.Generic;
using SugarDash.Presentation.Runner;
using UnityEngine;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 트랙 물체(장애물, 음식, 쉬는 곳)의 모습을 만든다. TrackSpawner의 look 필드에 연결하면
    /// 이 클래스로 만들고, 비어 있으면 TrackSpawner가 PrimitiveViews로 만든다.
    /// </summary>
    // [빈 틀] 담당: 월드
    public sealed class TrackItemLook : MonoBehaviour
    {
        [Tooltip("모든 구간에서 쓰는 기본 프리팹 묶음. 빈 칸은 기본 도형으로 나온다.")]
        [SerializeField] private TrackItemPrefabSet defaultPrefabs = new TrackItemPrefabSet();
        [Tooltip("장면별로 바꿔 쓸 프리팹 묶음(운동장 축구공, 놀이터 미끄럼틀 등). 빈 칸은 기본 묶음을 쓴다.")]
        [SerializeField] private List<SceneTrackItemSet> scenePrefabs = new List<SceneTrackItemSet>();

        /// <summary>지금 장면 id. 장면 없는 구간이면 null.</summary>
        public string SceneId { get; private set; }

        /// <summary>TrackSpawner가 장면이 바뀔 때 부른다.</summary>
        public void SetScene(string sceneId) => SceneId = sceneId;

        /// <summary>물체의 보이는 모습을 만들어 parent 아래에 둔다.</summary>
        public Transform CreateView(TrackItem item, Transform parent)
        {
            // 물체 종류(item.Kind)와 음식 id(item.SnackId)별 프리팹으로 만든다.
            // 장면 id(SceneId)별 묶음 → 기본 묶음 → 기본 도형(PrimitiveViews) 순서로 찾는다.
            var prefab = ResolvePrefab(item);
            return prefab != null
                ? TrackItemViewFactory.Create(prefab, item, parent)
                : PrimitiveViews.CreateItem(item, parent);
        }

        private GameObject ResolvePrefab(TrackItem item)
        {
            var sceneSet = SceneTrackItemSet.Find(scenePrefabs, SceneId);
            var prefab = sceneSet?.For(item.Kind, item.SnackId);
            if (prefab != null) return prefab;
            return defaultPrefabs?.For(item.Kind, item.SnackId);
        }
    }
}
