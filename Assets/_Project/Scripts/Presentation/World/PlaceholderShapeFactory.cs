using System.Collections.Generic;
using UnityEngine;

namespace SugarDash.Presentation
{
    /// <summary>
    /// PlaceholderShape 목록을 실제 오브젝트로 만든다. 콜라이더는 지운다(충돌은 계산으로 판정하고 배경은 부딪히지 않는다).
    /// </summary>
    // [영역 파일] 담당: 월드
    public static class PlaceholderShapeFactory
    {
        /// <summary>parent 아래에 name 루트를 만들고 그 아래 도형을 모두 만든다.</summary>
        public static GameObject Build(string name, IReadOnlyList<PlaceholderShape> shapes, Transform parent,
            ColorMaterialCache materials)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            if (shapes == null) return root;
            for (int i = 0; i < shapes.Count; i++) Create(shapes[i], root.transform, materials);
            return root;
        }

        public static Transform Create(PlaceholderShape shape, Transform parent, ColorMaterialCache materials)
        {
            var go = GameObject.CreatePrimitive(shape.type);
            go.name = shape.type.ToString();
            var collider = go.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);

            var renderer = go.GetComponent<Renderer>();
            var material = materials?.Get(shape.color, renderer.sharedMaterial);
            if (material != null) renderer.sharedMaterial = material;
            // 배경은 그림자를 받기만 하고 드리우지 않는다(저사양 기기 부담을 줄인다).
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = shape.position;
            t.localRotation = Quaternion.Euler(shape.euler);
            t.localScale = shape.scale;
            return t;
        }
    }
}
