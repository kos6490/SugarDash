using UnityEngine;

namespace SugarDash.Presentation.Runner
{
    /// <summary>트랙 위 물체의 종류.</summary>
    public enum TrackItemKind
    {
        /// <summary>낮은 장애물. 점프로 피한다.</summary>
        LowObstacle,
        /// <summary>높은 장애물. 슬라이드로 피한다.</summary>
        HighObstacle,
        /// <summary>레인을 막는 장애물. 옆 레인으로 피한다.</summary>
        LaneBlock,
        Snack,
        /// <summary>달리는 느낌을 주는 바닥 표시. 충돌하지 않는다.</summary>
        Marker,
    }

    /// <summary>트랙 위 물체 하나. 캐릭터는 z = 0에 있고 물체가 z가 줄어드는 쪽으로 다가온다.</summary>
    public sealed class TrackItem
    {
        public TrackItem(TrackItemKind kind, int lane, double z, string snackId = null)
        {
            Kind = kind;
            Lane = lane;
            Z = z;
            SnackId = snackId;
        }

        public TrackItemKind Kind { get; }
        /// <summary>-1(왼쪽), 0(가운데), 1(오른쪽).</summary>
        public int Lane { get; }
        public double Z { get; set; }
        /// <summary><see cref="TrackItemKind.Snack"/>일 때만 값이 있다.</summary>
        public string SnackId { get; }
        /// <summary>화면 표현. 테스트처럼 화면 없이 돌리면 null.</summary>
        public Transform View { get; set; }

        public bool IsObstacle =>
            Kind == TrackItemKind.LowObstacle || Kind == TrackItemKind.HighObstacle || Kind == TrackItemKind.LaneBlock;
    }
}
