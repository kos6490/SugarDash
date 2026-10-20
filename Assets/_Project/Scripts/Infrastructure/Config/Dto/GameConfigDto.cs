using System.Collections.Generic;
using UnityEngine.Scripting;

namespace SugarDash.Infrastructure.Config.Dto
{
    // Newtonsoft 역직렬화 전용 DTO. IL2CPP 스트리핑으로 지워지지 않게 [Preserve]를 붙인다.
    // 값이 없음(PLACEHOLDER)을 표현하려고 모두 nullable로 둔다.

    [Preserve]
    public sealed class GameConfigDto
    {
        public string ConfigVersion { get; set; }
        public string Unit { get; set; }
        public TimeDto Time { get; set; }
        public GlucoseDto Glucose { get; set; }
        public RecurrenceDto Recurrence { get; set; }
        public DisplayDto Display { get; set; }
        public HypoDto Hypo { get; set; }
        public SuddenDropDto SuddenDrop { get; set; }
        public List<SnackDto> Snacks { get; set; }
    }

    [Preserve]
    public sealed class TimeDto
    {
        public double? TimeScale { get; set; }
        public double? RecheckIntervalRealMin { get; set; }
        public string RecheckIntervalSource { get; set; }
    }

    [Preserve]
    public sealed class GlucoseDto
    {
        public double? Initial { get; set; }
        public double? Min { get; set; }
        public double? Max { get; set; }
        public double? HypoThreshold { get; set; }
        public double? HypoReleaseMargin { get; set; }
        public double? HyperThreshold { get; set; }
        public double? BaseDecayPerSec { get; set; }
        public double? RunDecayPerSec { get; set; }
    }

    [Preserve]
    public sealed class RecurrenceDto
    {
        public double? CooldownSec { get; set; }
    }

    [Preserve]
    public sealed class DisplayDto
    {
        public double? DelaySec { get; set; }
        public double? WarningThreshold { get; set; }
    }

    [Preserve]
    public sealed class HypoDto
    {
        public double? Step1TimeLimitSec { get; set; }
    }

    [Preserve]
    public sealed class SuddenDropDto
    {
        public double? ChancePerSec { get; set; }
        public double? Amount { get; set; }
        public double? DurationSec { get; set; }
    }

    [Preserve]
    public sealed class SnackDto
    {
        public string Id { get; set; }
        public string Kind { get; set; }
        public double? TotalRise { get; set; }
        public double? OnsetSec { get; set; }
        public double? AbsorbSec { get; set; }
        public string Source { get; set; }
    }
}
