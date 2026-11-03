using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SugarDash.Domain.Ports;
using SugarDash.Infrastructure.Progress.Dto;

namespace SugarDash.Infrastructure.Progress
{
    /// <summary>
    /// <see cref="IBestScoreStore"/> 구현. 파일 하나에 최고 점수를 덮어쓴다.
    /// 임시 파일에 쓴 뒤 교체(atomic write)해 쓰는 도중 종료돼도 기존 파일이 깨지지 않는다.
    /// 형식: <c>{"schemaVersion":1,"configVersion":"...","bestScore":123.4}</c>
    /// </summary>
    public sealed class BestScoreRepository : IBestScoreStore
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
        public BestScoreRepository(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("경로가 비었다", nameof(path));
            _path = path;
        }

        public double? Load(string configVersion)
        {
            try
            {
                if (!File.Exists(_path)) return null;

                var dto = JsonConvert.DeserializeObject<BestScoreDto>(File.ReadAllText(_path, Utf8NoBom), Settings);
                if (dto == null || dto.SchemaVersion != SchemaVersion || dto.ConfigVersion != configVersion) return null;
                if (!dto.BestScore.HasValue || double.IsNaN(dto.BestScore.Value)) return null;
                return dto.BestScore;
            }
            catch (Exception e) when (e is JsonException || e is IOException || e is UnauthorizedAccessException)
            {
                // 깨진 파일은 기록이 없는 것으로 본다. 다음 Save가 새 파일로 덮어쓴다.
                return null;
            }
        }

        public void Save(string configVersion, double score)
        {
            string tmp = _path + ".tmp";
            try
            {
                string dir = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                var dto = new BestScoreDto { SchemaVersion = SchemaVersion, ConfigVersion = configVersion, BestScore = score };
                File.WriteAllText(tmp, JsonConvert.SerializeObject(dto, Settings), Utf8NoBom);

                if (File.Exists(_path)) File.Replace(tmp, _path, null);
                else File.Move(tmp, _path);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                // 결과 화면으로 가는 흐름을 막지 않는다. 최고 점수만 잃고 게임은 계속한다(LogRepository와 같은 방침).
                UnityEngine.Debug.LogException(e);
            }
        }
    }
}
