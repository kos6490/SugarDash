using SugarDash.Domain.Config;

namespace SugarDash.Domain.Events
{
    /// <summary>주행이 장면 구간 안의 새 장면(맵 = 하루의 한 때)에 들어갔다.</summary>
    public sealed class SceneEntered
    {
        public SceneEntered(string scene, TimeOfDay timeOfDay, int dayIndex, double distanceM, double simT, double t)
        {
            Scene = scene;
            TimeOfDay = timeOfDay;
            DayIndex = dayIndex;
            DistanceM = distanceM;
            SimT = simT;
            T = t;
        }

        /// <summary>설정의 장면 id.</summary>
        public string Scene { get; }
        public TimeOfDay TimeOfDay { get; }
        /// <summary>장면 구간에 들어간 첫날이 0. 마지막 장면에서 첫 장면으로 돌아갈 때 하나 오른다.</summary>
        public int DayIndex { get; }
        /// <summary>장면에 들어간 것을 처리한 시점의 주행 거리(m). 그 프레임에 간 거리까지 더해져 장면 경계보다 클 수 있다.</summary>
        public double DistanceM { get; }
        /// <summary>simDt 누적 시각.</summary>
        public double SimT { get; }
        /// <summary>decisionDt 누적 시각.</summary>
        public double T { get; }
    }
}
