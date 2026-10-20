namespace SugarDash.Domain.Events
{
    /// <summary>급하강 이벤트가 시작됐다.</summary>
    public sealed class SuddenDropStarted
    {
        public SuddenDropStarted(double simTime, double realElapsed)
        {
            SimTime = simTime;
            RealElapsed = realElapsed;
        }

        public double SimTime { get; }
        public double RealElapsed { get; }
    }
}
