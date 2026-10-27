namespace SugarDash.Domain.Ports
{
    /// <summary>
    /// 기록 쓰기 포트. 도메인 이벤트는 구현체가 DomainEventBus를 구독해 받는다.
    /// 이 포트는 주행 단위 파일의 시작과 끝만 맡는다.
    /// </summary>
    public interface ILogRepository
    {
        /// <summary>새 주행 기록을 연다. 이미 열린 주행이 있으면 예외를 던진다.</summary>
        /// <returns>주행 식별자(runId).</returns>
        string BeginRun();

        /// <summary>열린 주행 기록을 닫는다. 열린 주행이 없으면 아무것도 하지 않는다.</summary>
        void EndRun();
    }
}
