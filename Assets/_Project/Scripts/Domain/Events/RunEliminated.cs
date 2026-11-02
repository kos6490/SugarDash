using SugarDash.Domain.Hypo;

namespace SugarDash.Domain.Events
{
    /// <summary>
    /// 주행에서 탈락했다. 오류 뒤 어른에게 알리지 않았거나, 실제 혈당이 안전 범위를 벗어났거나, 장애물에 부딪혔다.
    /// 어른이 도와주러 오는 장면 뒤 결과 화면으로 간다.
    /// </summary>
    public sealed class RunEliminated
    {
        public RunEliminated(int eventId, HypoStep? step, HypoErrorCode reason, EliminationCause cause,
            double distanceM, double simT, double t)
        {
            EventId = eventId;
            Step = step;
            Reason = reason;
            Cause = cause;
            DistanceM = distanceM;
            SimT = simT;
            T = t;
        }

        /// <summary>진행 중인 이벤트 번호. 이벤트가 없을 때 탈락하면 0.</summary>
        public int EventId { get; }
        /// <summary>탈락으로 이어진 단계. 주행 중 탈락(혈당·장애물)이면 null.</summary>
        public HypoStep? Step { get; }
        /// <summary>탈락으로 이어진 그 단계의 오류. 주행 중 탈락이면 None.</summary>
        public HypoErrorCode Reason { get; }
        public EliminationCause Cause { get; }
        /// <summary>탈락한 순간의 주행 거리(m).</summary>
        public double DistanceM { get; }
        /// <summary>simDt 누적 시각.</summary>
        public double SimT { get; }
        /// <summary>decisionDt 누적 시각.</summary>
        public double T { get; }
    }
}
