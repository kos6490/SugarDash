using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace SugarDash.Infrastructure.Config.Dto
{
    // 2형 설정 파일 전용 DTO. 1형 전용 키(측정기, 표시 지연, 4단계, 급하강, 흔들림)는 두지 않는다.
    // 그런 키가 있으면 MissingMemberHandling.Error로 거부된다. 값이 없음(PLACEHOLDER)은 nullable로 표현한다.

    [Preserve]
    public sealed class Type2ConfigDto
    {
        public string ConfigVersion { get; set; }
        public string TrackType { get; set; }
        public string Unit { get; set; }
        public Type2TimeDto Time { get; set; }
        public GlucoseDto Glucose { get; set; }
        public Type2SectionDto Type2 { get; set; }
        public List<FoodDto> Foods { get; set; }
        public Type2RunnerDto Runner { get; set; }
        public EliminationDto Elimination { get; set; }
        public Type2ScoreDto Score { get; set; }
        public List<SectionDto> Sections { get; set; }
    }

    [Preserve]
    public sealed class Type2TimeDto
    {
        public double? TimeScale { get; set; }
    }

    [Preserve]
    public sealed class Type2SectionDto
    {
        public double? HighHoldSec { get; set; }
        public double? LowHoldSec { get; set; }
        public double? NoticeSec { get; set; }
        public double? TellTimeLimitSec { get; set; }
        public double? GuardianPromptTimeLimitSec { get; set; }
        public double? AnswerFeedbackSec { get; set; }
        public double? ResumeCountdownSec { get; set; }
        public double? GraceSec { get; set; }
        public string GuardianSnackId { get; set; }
        public string JuiceChoiceFoodId { get; set; }
    }

    /// <summary>시간은 실제 시간 기준 분 단위다. 로드할 때 게임 초로 바꾼다.</summary>
    [Preserve]
    public sealed class FoodDto
    {
        public string Id { get; set; }
        public double? TotalRise { get; set; }
        public double? OnsetRealMin { get; set; }
        public double? AbsorbRealMin { get; set; }
        public string Source { get; set; }
    }

    [Preserve]
    public sealed class Type2RunnerDto
    {
        public double? FoodRowIntervalMinSec { get; set; }
        public double? FoodRowIntervalMaxSec { get; set; }
        public double? RestIntervalMinSec { get; set; }
        public double? RestIntervalMaxSec { get; set; }
        public double? RestDurationSec { get; set; }
    }

    [Preserve]
    public sealed class Type2ScoreDto
    {
        public double? InRangePerSec { get; set; }
        public double? PerMeter { get; set; }
        public Type2ScoreCorrectDto Correct { get; set; }
        public Type2ScoreErrorsDto Errors { get; set; }
    }

    [Preserve]
    public sealed class Type2ScoreCorrectDto
    {
        public double? HyperNotify { get; set; }
        public double? LowNotify { get; set; }
    }

    /// <summary>키는 LogCodes.Error와 같은 문자열이다. 테스트로 대응을 확인한다.</summary>
    [Preserve]
    public sealed class Type2ScoreErrorsDto
    {
        [JsonProperty("HY_KEEP_RUNNING")] public double? HyKeepRunning { get; set; }
        [JsonProperty("HY_JUICE")] public double? HyJuice { get; set; }
        [JsonProperty("HY_TIMEOUT")] public double? HyTimeout { get; set; }
        [JsonProperty("LO_KEEP_RUNNING")] public double? LoKeepRunning { get; set; }
        [JsonProperty("LO_TIMEOUT")] public double? LoTimeout { get; set; }
    }
}
