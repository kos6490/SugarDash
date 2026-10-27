namespace SugarDash.Domain.Events
{
    /// <summary>주행 중 트랙 간식을 주워 먹었다. 대처 단계 판정과는 무관하다.</summary>
    public sealed class TrackSnackEaten
    {
        public TrackSnackEaten(string snackId, double servings, double simT, double t)
        {
            SnackId = snackId;
            Servings = servings;
            SimT = simT;
            T = t;
        }

        public string SnackId { get; }
        public double Servings { get; }
        /// <summary>simDt 누적 시각.</summary>
        public double SimT { get; }
        /// <summary>decisionDt 누적 시각.</summary>
        public double T { get; }
    }
}
