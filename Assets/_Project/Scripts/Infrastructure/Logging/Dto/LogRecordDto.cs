using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace SugarDash.Infrastructure.Logging.Dto
{
    // .jsonl 한 줄에 대응하는 DTO. 레코드 종류(type)마다 쓰는 필드만 채우고 나머지는 null로 두어 쓰지 않는다.
    // 조회 시 Newtonsoft로 역직렬화하므로 IL2CPP 스트리핑으로 지워지지 않게 [Preserve]를 붙인다.
    // 필드 의미는 docs/log-format.md에 있다.

    [Preserve]
    public sealed class LogRecordDto
    {
        [JsonProperty(Order = -5)] public int SchemaVersion { get; set; }
        [JsonProperty(Order = -4)] public string ConfigVersion { get; set; }
        [JsonProperty(Order = -3)] public string RunId { get; set; }
        [JsonProperty(Order = -2)] public int Seq { get; set; }
        [JsonProperty(Order = -1)] public string Type { get; set; }

        public string AtUtc { get; set; }
        public int? EventId { get; set; }
        public double? SimT { get; set; }
        public double? T { get; set; }

        public bool? IsRecurrence { get; set; }
        public double? Actual { get; set; }
        public double? Displayed { get; set; }
        public double? Measured { get; set; }
        public bool? Unnecessary { get; set; }
        public bool? InGrace { get; set; }

        public string Step { get; set; }
        public string ChoiceId { get; set; }
        public bool? IsCorrect { get; set; }
        public string ErrorCode { get; set; }
        public bool? HelpInstead { get; set; }
        public double? DecisionSec { get; set; }
        public bool? MeasuredBeforeWarning { get; set; }

        public string SnackId { get; set; }
        public double? Servings { get; set; }
        public bool? ChosenByChild { get; set; }

        public double? SimSecSkipped { get; set; }
        public double? GlucoseBefore { get; set; }
        public double? GlucoseAfter { get; set; }

        public bool? AllCorrect { get; set; }
        public string Cause { get; set; }
    }
}
