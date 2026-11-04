using SugarDash.Domain.Config;

namespace SugarDash.Domain.Ports
{
    /// <summary>
    /// 기록 쓰기 포트. 도메인 이벤트는 구현체가 DomainEventBus를 구독해 받는다.
    /// 이 포트는 주행 단위 파일의 시작과 끝만 맡는다.
    /// </summary>
    public interface ILogRepository
    {
        /// <summary>새 주행 기록을 연다. 이미 열린 주행이 있으면 예외를 던진다.</summary>
        /// <param name="configVersion">이 주행이 쓰는 설정의 버전. 주행마다 다를 수 있어 여기서 받는다.</param>
        /// <param name="trackType">이 주행의 유형(1형·2형).</param>
        /// <returns>주행 식별자(runId).</returns>
        string BeginRun(string configVersion, TrackType trackType);

        /// <summary>
        /// 기록하지 않는 주행을 연다(튜토리얼 판). 파일을 만들지 않고, 열린 동안 오는 이벤트는 버린다.
        /// 이미 열린 주행이 있으면 예외를 던진다. <see cref="EndRun"/>이 닫는다.
        /// </summary>
        void BeginUnrecordedRun();

        /// <summary>열린 주행 기록을 닫는다. 열린 주행이 없으면 아무것도 하지 않는다.</summary>
        void EndRun();
    }
}
