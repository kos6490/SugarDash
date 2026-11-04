using SugarDash.Domain.Config;

namespace SugarDash.Domain.Events
{
    /// <summary>1형 혈당 재기 습관 순간의 말풍선이 떴다. 세계는 멈추지 않는다.</summary>
    public sealed class HabitPromptStarted
    {
        public HabitPromptStarted(HabitMoment moment, double windowSec, double simT, double t)
        {
            Moment = moment;
            WindowSec = windowSec;
            SimT = simT;
            T = t;
        }

        public HabitMoment Moment { get; }
        /// <summary>말풍선이 떠 있는 시간(decisionDt 기준 초).</summary>
        public double WindowSec { get; }
        /// <summary>simDt 누적 시각.</summary>
        public double SimT { get; }
        /// <summary>decisionDt 누적 시각.</summary>
        public double T { get; }
    }
}
