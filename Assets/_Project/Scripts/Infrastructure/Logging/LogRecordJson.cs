using SugarDash.Infrastructure.Logging.Dto;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SugarDash.Infrastructure.Logging
{
    /// <summary>.jsonl 한 줄과 <see cref="LogRecordDto"/> 사이의 변환. 쓰기와 조회가 같은 규칙을 쓴다.</summary>
    public static class LogRecordJson
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None,
        };

        /// <summary>줄바꿈 없는 한 줄. 값이 null인 필드는 쓰지 않는다.</summary>
        public static string Serialize(LogRecordDto record) => JsonConvert.SerializeObject(record, Settings);

        /// <summary>형식이 깨진 줄(예: 쓰기 도중 종료된 마지막 줄)이면 <see cref="JsonException"/>을 던진다.</summary>
        public static LogRecordDto Deserialize(string line) =>
            JsonConvert.DeserializeObject<LogRecordDto>(line, Settings);
    }
}
