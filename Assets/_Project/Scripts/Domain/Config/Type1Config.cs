namespace SugarDash.Domain.Config
{
    /// <summary>1형에만 있는 설정 중 다른 묶음에 속하지 않는 값.</summary>
    public sealed class Type1Config
    {
        public Type1Config(double habitCheckWindowSec)
        {
            HabitCheckWindowSec = habitCheckWindowSec;
        }

        /// <summary>혈당 재기 습관 순간의 말풍선이 떠 있는 시간(decisionDt 기준 초).</summary>
        public double HabitCheckWindowSec { get; }
    }
}
