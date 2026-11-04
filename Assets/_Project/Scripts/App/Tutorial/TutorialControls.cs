using System;

namespace SugarDash.App
{
    /// <summary>튜토리얼 안내 구간에서 지금 쓸 수 있는 조작(spec 10.1절).</summary>
    [Flags]
    public enum TutorialControls
    {
        None = 0,
        Left = 1,
        Right = 2,
        Jump = 4,
        Slide = 8,
        Meter = 16,
        All = Left | Right | Jump | Slide | Meter,
    }
}
