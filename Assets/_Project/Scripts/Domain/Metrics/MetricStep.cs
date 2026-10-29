namespace SugarDash.Domain.Metrics
{
    /// <summary>지표의 단계 단위. 4단계(재확인)는 재측정과 알리기를 묶어 하나로 센다.</summary>
    public enum MetricStep
    {
        /// <summary>1단계 위험 인지.</summary>
        Recognize,
        /// <summary>2단계 대응 선택.</summary>
        Respond,
        /// <summary>3단계 처치량 결정.</summary>
        Dose,
        /// <summary>4단계 재확인. 재측정과 알리기를 모두 맞혀야 정답.</summary>
        Recheck,
    }
}
