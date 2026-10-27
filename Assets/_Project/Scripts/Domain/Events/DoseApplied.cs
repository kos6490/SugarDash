namespace SugarDash.Domain.Events
{
    /// <summary>간식이 혈당에 투입됐다.</summary>
    public sealed class DoseApplied
    {
        public DoseApplied(int eventId, string snackId, double servings, bool chosenByChild, double simT, double t)
        {
            EventId = eventId;
            SnackId = snackId;
            Servings = servings;
            ChosenByChild = chosenByChild;
            SimT = simT;
            T = t;
        }

        public int EventId { get; }
        public string SnackId { get; }
        public double Servings { get; }
        /// <summary>아동이 고른 간식·양이면 true, 아동이 고르지 못해 어른이 정한 것이면 false.</summary>
        public bool ChosenByChild { get; }
        /// <summary>simDt 누적 시각.</summary>
        public double SimT { get; }
        /// <summary>decisionDt 누적 시각.</summary>
        public double T { get; }
    }
}
