using SugarDash.Domain.Config;

namespace SugarDash.App
{
    /// <summary>튜토리얼이 혈당을 옮길 목표. 새 수치를 만들지 않고 설정 값의 가운데로 정한다.</summary>
    public static class TutorialTargets
    {
        /// <summary>저혈당 쪽: <c>elimination.glucoseLow</c>와 <c>glucose.hypoThreshold</c>의 가운데.</summary>
        public static double Low(GameConfig c) => (c.Elimination.GlucoseLow + c.Glucose.Levels.HypoThreshold) / 2;

        /// <summary>고혈당 쪽: <c>glucose.hyperThreshold</c>와 <c>elimination.glucoseHigh</c>의 가운데.</summary>
        public static double High(GameConfig c) => (c.Glucose.Levels.HyperThreshold + c.Elimination.GlucoseHigh) / 2;
    }
}
