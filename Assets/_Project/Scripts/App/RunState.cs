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
        /// <summary>오류 뒤 어른에게 알리지 않아 탈락했다. 더 진행하지 않는다.</summary>
        Eliminated,
        /// <summary>주행 길이만큼 달려 마쳤다. 더 진행하지 않는다.</summary>
        Completed,
    }
}
