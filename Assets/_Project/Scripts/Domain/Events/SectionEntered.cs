namespace SugarDash.Domain.Events
{
    /// <summary>주행이 새 구간에 들어갔다.</summary>
    public sealed class SectionEntered
    {
        public SectionEntered(string section, double distanceM, double simT, double t)
        {
            Section = section;
            DistanceM = distanceM;
            SimT = simT;
            T = t;
        }

        /// <summary>설정의 구간 이름.</summary>
        public string Section { get; }
        /// <summary>구간에 들어간 것을 처리한 시점의 주행 거리(m). 그 프레임에 간 거리까지 더해져 구간 경계보다 클 수 있다.</summary>
        public double DistanceM { get; }
        /// <summary>simDt 누적 시각.</summary>
        public double SimT { get; }
        /// <summary>decisionDt 누적 시각.</summary>
        public double T { get; }
    }
}
