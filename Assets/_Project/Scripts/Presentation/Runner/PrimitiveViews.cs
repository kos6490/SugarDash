using UnityEngine;

namespace SugarDash.Presentation.Runner
{
    /// <summary>
    /// 그래픽 에셋이 생기기 전까지 쓰는 기본 도형 표현. 실행 중에 코드로 만든다.
    /// 에셋이 생기면 이 클래스만 프리팹 생성으로 바꾼다.
    /// </summary>
    public static class PrimitiveViews
    {
        public static Transform CreatePlayer(Transform parent) =>
            Create(PrimitiveType.Capsule, parent, "Player", new Vector3(0.8f, 1f, 0.8f), new Color(0.2f, 0.45f, 0.9f));

        public static Transform CreateGround(Transform parent, float width, float length)
        {
            var ground = Create(PrimitiveType.Cube, parent, "Ground", new Vector3(width, 0.1f, length),
                new Color(0.35f, 0.35f, 0.38f));
            ground.localPosition = new Vector3(0, -0.05f, length / 2 - 10);
            return ground;
        }

        public static Transform CreateItem(TrackItem item, Transform parent)
        {
            switch (item.Kind)
            {
                case TrackItemKind.LowObstacle:
                    return Create(PrimitiveType.Cube, parent, "LowObstacle", new Vector3(2f, 0.6f, 0.6f),
                        new Color(0.9f, 0.3f, 0.25f));
                case TrackItemKind.HighObstacle:
                    return Create(PrimitiveType.Cube, parent, "HighObstacle", new Vector3(2f, 0.5f, 0.6f),
                        new Color(0.95f, 0.6f, 0.2f));
                case TrackItemKind.LaneBlock:
                    return Create(PrimitiveType.Cube, parent, "LaneBlock", new Vector3(2f, 2.2f, 1f),
                        new Color(0.55f, 0.25f, 0.6f));
                case TrackItemKind.Snack:
                    return Create(PrimitiveType.Sphere, parent, "Snack " + item.SnackId, Vector3.one * 0.6f,
                        new Color(1f, 0.85f, 0.2f));
                default:
                    return Create(PrimitiveType.Cube, parent, "Marker", new Vector3(0.15f, 0.02f, 1.2f),
                        new Color(0.85f, 0.85f, 0.85f));
            }
        }

        /// <summary>도형의 기준 높이. 높은 장애물은 슬라이드로 밑을 지나가도록 공중에 띄운다.</summary>
        public static float BaseHeight(TrackItemKind kind)
        {
            switch (kind)
            {
                case TrackItemKind.LowObstacle: return 0.3f;
                case TrackItemKind.HighObstacle: return 1.6f;
                case TrackItemKind.LaneBlock: return 1.1f;
                case TrackItemKind.Snack: return 0.8f;
                default: return 0.01f;
            }
        }

        private static Transform Create(PrimitiveType type, Transform parent, string name, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            // 충돌은 계산으로 판정하므로 물리 콜라이더는 쓰지 않는다.
            Object.Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().material.color = color;
            var t = go.transform;
            t.SetParent(parent, false);
            t.localScale = scale;
            return t;
        }
    }
}
