namespace SugarDash.Domain.Hypo
{
    /// <summary>단계별 오류 코드.</summary>
    public enum HypoErrorCode
    {
        None,
        /// <summary>S_MISSED: 1단계 제한 시간 안에 측정기를 누르지 않았다.</summary>
        SMissed,
        /// <summary>RS_SLOW: 느린 당을 골랐다.</summary>
        RsSlow,
        /// <summary>RS_KEEP_RUNNING: 계속 달리기를 골랐다(위험 선택).</summary>
        RsKeepRunning,
        /// <summary>RS_NONE: 당 없음을 골랐다.</summary>
        RsNone,
        /// <summary>RS_TIMEOUT: 2단계 제한 시간 초과.</summary>
        RsTimeout,
        /// <summary>DS_UNDER: 과소 처치.</summary>
        DsUnder,
        /// <summary>DS_OVER: 과잉 처치.</summary>
        DsOver,
        /// <summary>DS_TIMEOUT: 3단계 제한 시간 초과.</summary>
        DsTimeout,
        /// <summary>RC_NO_RECHECK: 재측정을 건너뛰었다.</summary>
        RcNoRecheck,
        /// <summary>RC_NO_NOTIFY: 어른에게 알리지 않았다.</summary>
        RcNoNotify,
        /// <summary>HELP_INSTEAD: 2·3단계에서 선택 대신 알리기 버튼을 눌렀다.</summary>
        HelpInstead,
    }
}
