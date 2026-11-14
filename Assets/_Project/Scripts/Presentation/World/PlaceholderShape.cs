using System;
using UnityEngine;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 개발용 배경을 이루는 기본 도형 하나(상자, 공, 원기둥 등). 위치·크기·각도·색만 있다.
    /// 모델이 나오기 전까지 PlaceholderBackdropRecipes가 장면별로 이 목록을 만든다. 연출만이다.
    /// </summary>
    // [영역 파일] 담당: 월드
    [Serializable]
    public struct PlaceholderShape
    {
        public PrimitiveType type;
        public Vector3 position;
        public Vector3 scale;
        public Vector3 euler;
        public Color color;

        public PlaceholderShape(PrimitiveType type, Vector3 position, Vector3 scale, Color color, Vector3 euler = default)
        {
            this.type = type;
            this.position = position;
            this.scale = scale;
            this.color = color;
            this.euler = euler;
        }

        /// <summary>좌우(x)를 뒤집은 사본. 길 양쪽에 같은 것을 세울 때 쓴다.</summary>
        public PlaceholderShape Mirrored() =>
            new PlaceholderShape(type, new Vector3(-position.x, position.y, position.z), scale, color,
                new Vector3(euler.x, -euler.y, -euler.z));
    }
}
