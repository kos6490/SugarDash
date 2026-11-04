using System;
using System.Collections.Generic;

namespace SugarDash.App
{
    /// <summary>튜토리얼 안내가 트랙에 놓는 대상의 종류.</summary>
    public enum TutorialObject
    {
        LaneBlock,
        LowObstacle,
        HighObstacle,
        Snack,
        FoodRow,
        RestSpot,
    }

    /// <summary>안내 하나가 놓을 대상. 레인은 −1(왼쪽), 0(가운데), 1(오른쪽)이다. 음식 줄은 레인을 비워 두고 스포너가 정한다.</summary>
    public sealed class TutorialPlacement
    {
        public TutorialPlacement(TutorialObject kind, IReadOnlyList<int> lanes)
        {
            Kind = kind;
            Lanes = lanes ?? throw new ArgumentNullException(nameof(lanes));
        }

        public TutorialObject Kind { get; }
        public IReadOnlyList<int> Lanes { get; }
    }
}
