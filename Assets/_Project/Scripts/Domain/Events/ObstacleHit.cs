namespace SugarDash.Domain.Events
{
    /// <summary>장애물에 부딪혀 하트를 하나 잃었다.</summary>
    public sealed class ObstacleHit
    {
        public ObstacleHit(int heartsLeft, double distanceM, double simT, double t)
        {
            HeartsLeft = heartsLeft;
            DistanceM = distanceM;
            SimT = simT;
            T = t;
        }

        /// <summary>부딪힌 뒤 남은 하트 수.</summary>
        public int HeartsLeft { get; }
        /// <summary>부딪힌 순간의 주행 거리(m).</summary>
        public double DistanceM { get; }
        /// <summary>simDt 누적 시각.</summary>
        public double SimT { get; }
        /// <summary>decisionDt 누적 시각.</summary>
        public double T { get; }
    }
}
