using System;
using System.Collections.Generic;
using UnityEngine;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 색 하나에 머티리얼 하나만 만들어 나눠 쓴다. 기본 도형마다 머티리얼 사본이 생기지 않게 한다.
    /// 만든 머티리얼은 저절로 지워지지 않으므로 다 쓰면 Dispose한다.
    /// </summary>
    // [영역 파일] 담당: 월드
    public sealed class ColorMaterialCache : IDisposable
    {
        private readonly Dictionary<Color, Material> _materials = new Dictionary<Color, Material>();

        /// <summary>color 머티리얼. 처음이면 template을 복사해 만든다(기본 도형의 기본 머티리얼을 넘기면 된다).</summary>
        public Material Get(Color color, Material template)
        {
            if (_materials.TryGetValue(color, out var material) && material != null) return material;
            material = template != null ? new Material(template) : null;
            if (material == null) return null;
            material.name = "Placeholder " + ColorUtility.ToHtmlStringRGB(color);
            material.color = color;
            _materials[color] = material;
            return material;
        }

        public int Count => _materials.Count;

        public void Dispose()
        {
            foreach (var material in _materials.Values)
            {
                if (material != null) UnityEngine.Object.Destroy(material);
            }
            _materials.Clear();
        }
    }
}
