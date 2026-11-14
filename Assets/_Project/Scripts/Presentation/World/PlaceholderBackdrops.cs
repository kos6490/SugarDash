using System.Collections.Generic;
using UnityEngine;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 모델이 나오기 전에 쓰는 개발용 배경. 장면 id를 받으면 그 장면의 도형 묶음만 켠다(처음 볼 때 만든다).
    /// 장면이 없는 구간(튜토리얼, 1구간)에는 noSceneId 배경을 보여 준다.
    /// 배경 한 칸을 앞뒤로 이어 붙이고 Scroll로 받은 달린 거리만큼 뒤로 흘려보낸다.
    /// SceneLook이 맵 배경(SceneBackdropSwitcher)이 연결되지 않았을 때 자식으로 만들어 쓴다. 연출만이다.
    /// 만든 머티리얼은 이 컴포넌트가 없어질 때 지운다.
    /// </summary>
    // [영역 파일] 담당: 월드
    public sealed class PlaceholderBackdrops : MonoBehaviour
    {
        [Tooltip("장면별 도형 목록. 처음 값은 PlaceholderBackdropRecipes에 있다.")]
        [SerializeField] private List<PlaceholderScene> scenes = PlaceholderBackdropRecipes.CreateDefaults();
        [Tooltip("장면이 없는 구간(튜토리얼, 1구간)과 목록에 없는 장면에 보여 줄 장면 id")]
        [SerializeField] private string noSceneId = PlaceholderBackdropRecipes.NoSceneId;
        [Tooltip("배경 한 칸의 길이(m). 도형 목록은 z 0 ~ 이 길이 안에 둔다.")]
        [Min(1f)] [SerializeField] private float tileLength = PlaceholderBackdropRecipes.TileLength;
        [Tooltip("플레이어 뒤쪽에 둘 칸 수")]
        [Min(0)] [SerializeField] private int tilesBehind = 1;
        [Tooltip("플레이어 앞쪽에 둘 칸 수(지금 칸 포함)")]
        [Min(1)] [SerializeField] private int tilesAhead = 2;

        private readonly Dictionary<string, GameObject> _roots = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, BackdropTileStrip> _strips = new Dictionary<string, BackdropTileStrip>();
        private readonly ColorMaterialCache _materials = new ColorMaterialCache();
        private BackdropTileStrip _active;
        private double _distance;

        /// <summary>지금 켜 둔 장면 id(장면 없는 구간이면 noSceneId). 보여 줄 것이 없으면 null.</summary>
        public string ActiveSceneId { get; private set; }

        /// <summary>장면 id의 배경만 켠다. null이나 모르는 id면 장면 없는 구간 배경을 켠다.</summary>
        public void Show(string sceneId)
        {
            foreach (var pair in _roots) pair.Value.SetActive(false);
            _active = null;
            ActiveSceneId = null;

            var scene = FindScene(sceneId) ?? FindScene(noSceneId);
            if (scene == null) return;

            if (!_roots.TryGetValue(scene.sceneId, out var root) || root == null) root = Build(scene);
            root.SetActive(true);
            _active = _strips[scene.sceneId];
            _active.Place(_distance);
            ActiveSceneId = scene.sceneId;
        }

        /// <summary>달린 거리(m)를 받아 켜 둔 배경을 그만큼 뒤로 흘려보낸다.</summary>
        public void Scroll(double distance)
        {
            _distance = distance;
            _active?.Place(distance);
        }

        private GameObject Build(PlaceholderScene scene)
        {
            var root = new GameObject("Placeholder " + scene.sceneId);
            root.transform.SetParent(transform, false);
            var tiles = new List<Transform>();
            int count = tilesBehind + tilesAhead;
            for (int i = 0; i < count; i++)
                tiles.Add(PlaceholderShapeFactory.Build("Tile " + i, scene.shapes, root.transform, _materials).transform);
            _roots[scene.sceneId] = root;
            _strips[scene.sceneId] = new BackdropTileStrip(tiles, tileLength, tilesBehind);
            return root;
        }

        private PlaceholderScene FindScene(string sceneId)
        {
            if (string.IsNullOrEmpty(sceneId) || scenes == null) return null;
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i] != null && scenes[i].sceneId == sceneId) return scenes[i];
            }
            return null;
        }

        private void OnDestroy() => _materials.Dispose();
    }
}
