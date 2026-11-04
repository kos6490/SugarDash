using SugarDash.Domain.Hypo;

namespace SugarDash.Domain.Events
{
    /// <summary>연습 모드(튜토리얼)에서 오답을 골라 같은 단계가 다시 열렸다. 기록·점수·혈당에 영향이 없다.</summary>
    public sealed class PracticeRetried
    {
        public PracticeRetried(int eventId, HypoStep step, string choiceId, double simT, double t)
        {
            EventId = eventId;
            Step = step;
            ChoiceId = choiceId;
            SimT = simT;
            T = t;
        }

        public int EventId { get; }
        public HypoStep Step { get; }
        /// <summary>고른 선택지.</summary>
        public string ChoiceId { get; }
        /// <summary>simDt 누적 시각.</summary>
        public double SimT { get; }
        /// <summary>decisionDt 누적 시각.</summary>
        public double T { get; }
    }
}
