using System;
using System.Collections.Generic;
using SugarDash.Domain.Hypo;

namespace SugarDash.Domain.Config
{
    /// <summary>점수 설정. 선택지 가중치와 주행 점수 비율.</summary>
    public sealed class ScoreConfig
    {
        public ScoreConfig(double inRangePerSec, double perMeter,
            IReadOnlyDictionary<HypoStep, double> correctWeights,
            IReadOnlyDictionary<HypoErrorCode, double> errorWeights)
        {
            InRangePerSec = inRangePerSec;
            PerMeter = perMeter;
            CorrectWeights = correctWeights ?? throw new ArgumentNullException(nameof(correctWeights));
            ErrorWeights = errorWeights ?? throw new ArgumentNullException(nameof(errorWeights));
        }

        /// <summary>적정 범위에 머문 시간 1초당 점수.</summary>
        public double InRangePerSec { get; }
        /// <summary>달린 거리 1m당 점수.</summary>
        public double PerMeter { get; }
        /// <summary>단계를 맞혔을 때의 가중치. 모든 단계가 있어야 한다.</summary>
        public IReadOnlyDictionary<HypoStep, double> CorrectWeights { get; }
        /// <summary>오류별 가중치. None을 뺀 모든 오류 코드가 있어야 한다.</summary>
        public IReadOnlyDictionary<HypoErrorCode, double> ErrorWeights { get; }
    }
}
