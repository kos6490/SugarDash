using System;
using SugarDash.Domain.Config;
using SugarDash.Domain.Ports;

namespace SugarDash.App
{
    /// <summary>유형마다 따로 쓰는 결과 계산 값: 점수 설정, 최고 점수 저장소, 설정 버전.</summary>
    public sealed class TrackTypeProfile
    {
        public TrackTypeProfile(ScoreConfig score, IBestScoreStore best, string configVersion)
        {
            Score = score ?? throw new ArgumentNullException(nameof(score));
            Best = best ?? throw new ArgumentNullException(nameof(best));
            ConfigVersion = configVersion ?? throw new ArgumentNullException(nameof(configVersion));
        }

        public ScoreConfig Score { get; }
        public IBestScoreStore Best { get; }
        public string ConfigVersion { get; }
    }
}
