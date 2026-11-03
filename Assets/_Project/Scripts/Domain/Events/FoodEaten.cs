namespace SugarDash.Domain.Events
{
    /// <summary>2형 주행 중 트랙의 음식을 먹었다. 정답·오답이나 점수와는 무관하다.</summary>
    public sealed class FoodEaten
    {
        public FoodEaten(string foodId, double actual, double simT, double t)
        {
            FoodId = foodId;
            Actual = actual;
            SimT = simT;
            T = t;
        }

        public string FoodId { get; }
        public double Actual { get; }
        /// <summary>simDt 누적 시각.</summary>
        public double SimT { get; }
        /// <summary>decisionDt 누적 시각.</summary>
        public double T { get; }
    }
}
