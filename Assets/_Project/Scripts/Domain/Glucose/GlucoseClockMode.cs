namespace SugarDash.Domain.Glucose
{
    /// <summary>
    /// GlucoseModel의 시계 모드. 호출자(App, HypoEventFsm)가 대처 단계에 맞춰 지정한다.
    /// realElapsed는 모든 모드에서 흐른다.
    /// </summary>
    public enum GlucoseClockMode
    {
        /// <summary>일반 주행. simTime과 표시 시계가 함께 흐른다.</summary>
        Running,
        /// <summary>발동 ~ 1단계. 실제 혈당은 고정하고 표시 시계만 흐른다.</summary>
        ActualFrozen,
        /// <summary>2 ~ 3단계. 실제·표시 혈당 모두 멈춘다.</summary>
        AllFrozen,
    }
}
