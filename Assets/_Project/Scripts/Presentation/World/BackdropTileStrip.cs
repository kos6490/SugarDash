using System.Collections.Generic;
using UnityEngine;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 같은 배경 한 칸(길이 tileLength)을 앞뒤로 이어 붙여, 달린 거리만큼 뒤로 흘려보내는 줄.
    /// 트랙은 물체가 플레이어 쪽으로 다가오는 방식이라 배경도 같은 거리만큼 -z로 움직이고, 한 칸을 지나면 처음 자리로 돌아간다.
    /// 연출만이다. 거리 값을 바꾸지 않고 읽기만 한다.
    /// </summary>
    // [영역 파일] 담당: 월드
    public sealed class BackdropTileStrip
    {
        private readonly List<Transform> _tiles;
        private readonly float _tileLength;
        private readonly int _tilesBehind;

        public BackdropTileStrip(List<Transform> tiles, float tileLength, int tilesBehind)
        {
            _tiles = tiles ?? new List<Transform>();
            _tileLength = Mathf.Max(1f, tileLength);
            _tilesBehind = Mathf.Max(0, tilesBehind);
        }

        public int Count => _tiles.Count;

        /// <summary>달린 거리(미터)에 맞춰 칸들을 놓는다.</summary>
        public void Place(double distance)
        {
            for (int i = 0; i < _tiles.Count; i++)
            {
                if (_tiles[i] == null) continue;
                _tiles[i].localPosition = new Vector3(0f, 0f, TileZ(i, _tilesBehind, _tileLength, distance));
            }
        }

        /// <summary>
        /// index번째 칸의 시작 z. 거리를 칸 길이로 나눈 나머지만큼 뒤로 민다.
        /// 나머지만 쓰므로 오래 달려도 float 자리수가 흐려지지 않는다.
        /// </summary>
        public static float TileZ(int index, int tilesBehind, float tileLength, double distance)
        {
            double offset = distance % tileLength;
            if (offset < 0) offset += tileLength;
            return (float)((index - tilesBehind) * (double)tileLength - offset);
        }
    }
}
