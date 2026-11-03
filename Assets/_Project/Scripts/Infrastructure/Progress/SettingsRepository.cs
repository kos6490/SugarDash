using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SugarDash.Domain.Config;
using SugarDash.Domain.Ports;
using SugarDash.Infrastructure.Logging;
using SugarDash.Infrastructure.Progress.Dto;

namespace SugarDash.Infrastructure.Progress
{
    /// <summary>
    /// <see cref="ISettingsStore"/> 구현. 파일 하나에 고른 유형을 덮어쓴다.
    /// 임시 파일에 쓴 뒤 교체(atomic write)해 쓰는 도중 종료돼도 기존 파일이 깨지지 않는다.
    /// 형식: <c>{"schemaVersion":1,"trackType":"type1"}</c>
    /// </summary>
    public sealed class SettingsRepository : ISettingsStore
    {
        /// <summary>파일 형식을 바꾸면 올린다.</summary>
        public const int SchemaVersion = 1;

        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.None,
        };

        private readonly string _path;

        /// <param name="path">파일 경로. Bootstrap이 <c>Application.persistentDataPath</c> 아래 경로를 넘긴다.</param>
        public SettingsRepository(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("경로가 비었다", nameof(path));
            _path = path;
        }

        public TrackType? LoadTrackType()
        {
            try
            {
                if (!File.Exists(_path)) return null;

                var dto = JsonConvert.DeserializeObject<SettingsDto>(File.ReadAllText(_path, Utf8NoBom), Settings);
                if (dto == null || dto.SchemaVersion != SchemaVersion) return null;

                // 알 수 없는 값은 1형으로 보지 않고 없는 것으로 본다(유형 선택 화면으로 돌아간다).
                if (dto.TrackType == LogCodes.TrackType(TrackType.Type1)) return TrackType.Type1;
                if (dto.TrackType == LogCodes.TrackType(TrackType.Type2)) return TrackType.Type2;
                return null;
            }
            catch (Exception e) when (e is JsonException || e is IOException || e is UnauthorizedAccessException)
            {
                // 깨진 파일은 기록이 없는 것으로 본다. 다음 Save가 새 파일로 덮어쓴다.
                return null;
            }
        }

        public void SaveTrackType(TrackType type)
        {
            string tmp = _path + ".tmp";
            try
            {
                string dir = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                var dto = new SettingsDto { SchemaVersion = SchemaVersion, TrackType = LogCodes.TrackType(type) };
                File.WriteAllText(tmp, JsonConvert.SerializeObject(dto, Settings), Utf8NoBom);

                if (File.Exists(_path)) File.Replace(tmp, _path, null);
                else File.Move(tmp, _path);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                // 유형 선택만 잃고 게임은 계속한다(BestScoreRepository와 같은 방침).
                UnityEngine.Debug.LogException(e);
            }
        }
    }
}
