using System.Collections.Generic;
using Newtonsoft.Json;
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
        public HyperDto Hyper { get; set; }
        public SuddenDropDto SuddenDrop { get; set; }
        public List<SnackDto> Snacks { get; set; }
        public RunnerDto Runner { get; set; }
        public EliminationDto Elimination { get; set; }
        public ScoreDto Score { get; set; }
        public List<SectionDto> Sections { get; set; }
    }

    [Preserve]
    public sealed class TimeDto
    {
        public double? TimeScale { get; set; }
        public double? RecheckIntervalRealMin { get; set; }
        public string RecheckIntervalSource { get; set; }
        public double? FastForwardTickSimSec { get; set; }
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
        public double? HyperReleaseMargin { get; set; }
        public double? BaseDecayPerSec { get; set; }
        public double? RunDecayPerSec { get; set; }
        public double? NormalRangeMin { get; set; }
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
        public double? Step2TimeLimitSec { get; set; }
        public double? Step3TimeLimitSec { get; set; }
        public double? Step4TimeLimitSec { get; set; }
        public double? GuardianPromptTimeLimitSec { get; set; }
        public double? ResumeCountdownSec { get; set; }
        public double? GraceSec { get; set; }
        public string GuardianSnackId { get; set; }
        public DosesDto Doses { get; set; }
    }

    [Preserve]
    public sealed class HyperDto
    {
        public double? NotifyTimeLimitSec { get; set; }
    }

    [Preserve]
    public sealed class DosesDto
    {
        public double? Under { get; set; }
        public double? Proper { get; set; }
        public double? Over { get; set; }
    }

    [Preserve]
    public sealed class SuddenDropDto
    {
        public double? ChancePerSec { get; set; }
        public double? Amount { get; set; }
        public double? DurationSec { get; set; }
    }

    [Preserve]
    public sealed class RunnerDto
    {
        public double? TrackSnackServings { get; set; }
        public double? RunDurationSec { get; set; }
        public double? TrackSnackIntervalMinSec { get; set; }
        public double? TrackSnackIntervalMaxSec { get; set; }
        public int? Hearts { get; set; }
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

    [Preserve]
    public sealed class EliminationDto
    {
        public double? GlucoseLow { get; set; }
        public double? GlucoseHigh { get; set; }
    }

    [Preserve]
    public sealed class ScoreDto
    {
        public double? InRangePerSec { get; set; }
        public double? PerMeter { get; set; }
        public ScoreCorrectDto Correct { get; set; }
        public ScoreErrorsDto Errors { get; set; }
    }

    [Preserve]
    public sealed class ScoreCorrectDto
    {
        public double? Recognize { get; set; }
        public double? Respond { get; set; }
        public double? Dose { get; set; }
        public double? Remeasure { get; set; }
        public double? Notify { get; set; }
        public double? HyperNotify { get; set; }
    }

    /// <summary>키는 LogCodes.Error와 같은 문자열이다. 테스트로 대응을 확인한다.</summary>
    [Preserve]
    public sealed class ScoreErrorsDto
    {
        [JsonProperty("S_MISSED")] public double? SMissed { get; set; }
        [JsonProperty("RS_SLOW")] public double? RsSlow { get; set; }
        [JsonProperty("RS_KEEP_RUNNING")] public double? RsKeepRunning { get; set; }
        [JsonProperty("RS_NONE")] public double? RsNone { get; set; }
        [JsonProperty("RS_TIMEOUT")] public double? RsTimeout { get; set; }
        [JsonProperty("HELP_INSTEAD")] public double? HelpInstead { get; set; }
        [JsonProperty("DS_UNDER")] public double? DsUnder { get; set; }
        [JsonProperty("DS_OVER")] public double? DsOver { get; set; }
        [JsonProperty("DS_TIMEOUT")] public double? DsTimeout { get; set; }
        [JsonProperty("RC_NO_RECHECK")] public double? RcNoRecheck { get; set; }
        [JsonProperty("RC_NO_NOTIFY")] public double? RcNoNotify { get; set; }
        [JsonProperty("HY_KEEP_RUNNING")] public double? HyKeepRunning { get; set; }
        [JsonProperty("HY_TIMEOUT")] public double? HyTimeout { get; set; }
    }

    [Preserve]
    public sealed class SectionDto
    {
        public string Id { get; set; }
        public double? LengthM { get; set; }
    }
}
