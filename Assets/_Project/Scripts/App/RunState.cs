namespace SugarDash.App
{
    /// <summary>RunController의 주행 상태.</summary>
    public enum RunState
    {
        /// <summary>열린 주행이 없다.</summary>
        Idle,
        Running,
        /// <summary>메뉴 일시정지·앱 백그라운드. 세 시계가 모두 멈춘다.</summary>
        Paused,
        /// <summary>탈락했다(알리기 거절·시간 초과, 혈당 안전 범위 이탈, 장애물 충돌). 더 진행하지 않는다.</summary>
        Eliminated,
    }
}
