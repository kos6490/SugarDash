using SugarDash.Domain.Config;
using SugarDash.Domain.Director;

namespace SugarDash.Domain.Events
{
    /// <summary>1형 혈당 재기 습관 순간이 끝났다. 점수·탈락과 무관하고 지표(습관 확인률)에만 쓴다.</summary>
    public sealed class HabitCheckEnded
    {
        public HabitCheckEnded(HabitMoment moment, HabitCheckOutcome outcome, double? reactionSec, double simT, double t)
        {
            Moment = moment;
            Outcome = outcome;
            ReactionSec = reactionSec;
            SimT = simT;
            T = t;
        }

        public HabitMoment Moment { get; }
        public HabitCheckOutcome Outcome { get; }
        /// <summary>말풍선이 뜬 뒤 측정기를 누르기까지 걸린 시간(초). Checked일 때만 있다.</summary>
        public double? ReactionSec { get; }
        /// <summary>simDt 누적 시각.</summary>
        public double SimT { get; }
        /// <summary>decisionDt 누적 시각.</summary>
        public double T { get; }
    }
}
