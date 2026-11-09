using SugarDash.Presentation.Runner;
using UnityEngine;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 프리팹으로 트랙 물체의 보이는 모습을 만든다.
    /// TrackSpawner는 돌려준 Transform을 기본 도형 높이(PrimitiveViews.BaseHeight)에 둔다. 그래서 빈 부모를 만들고
    /// 그 아래 프리팹을 그 높이만큼 내려 둔다. 결과적으로 발밑을 기준점으로 만든 프리팹이 바닥에 선다.
    /// 충돌은 계산으로 판정하므로 프리팹의 콜라이더·리지드바디는 지운다.
    /// </summary>
    // [영역 파일] 담당: 월드
    public static class TrackItemViewFactory
    {
        /// <summary>parent 아래에 물체 하나의 모습을 만든다. 지울 때는 돌려준 Transform의 GameObject를 지우면 된다.</summary>
        public static Transform Create(GameObject prefab, TrackItem item, Transform parent)
        {
            var root = new GameObject(RootName(item)).transform;
            root.SetParent(parent, false);

            var instance = Object.Instantiate(prefab, root, false);
            instance.transform.localPosition = GroundOffset(item.Kind);
            StripPhysics(instance);
            return root;
        }

        /// <summary>프리팹을 바닥에 세우려고 내리는 양. TrackSpawner가 부모를 올리는 높이의 반대다.</summary>
        public static Vector3 GroundOffset(TrackItemKind kind) => new Vector3(0f, -PrimitiveViews.BaseHeight(kind), 0f);

        public static string RootName(TrackItem item) =>
            item.Kind == TrackItemKind.Snack ? "Food " + item.SnackId : item.Kind.ToString();

        private static void StripPhysics(GameObject instance)
        {
            foreach (var body in instance.GetComponentsInChildren<Rigidbody>(true)) Object.Destroy(body);
            foreach (var collider in instance.GetComponentsInChildren<Collider>(true)) Object.Destroy(collider);
        }
    }
}
