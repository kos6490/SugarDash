using System.Collections.Generic;
using SugarDash.Domain.Records;

namespace SugarDash.Domain.Ports
{
    /// <summary>기록 조회 포트. 원시 기록만 돌려주고 계산은 하지 않는다(지표는 MetricsCalculator).</summary>
    public interface ILogQuery
    {
        /// <summary>기록된 주행의 runId. 오래된 것부터.</summary>
        IReadOnlyList<string> ListRunIds();

        /// <summary>주행 하나의 원시 기록. 없으면 null.</summary>
        RunRecord LoadRun(string runId);
    }
}
