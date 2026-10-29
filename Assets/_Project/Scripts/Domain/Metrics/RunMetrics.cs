using System.Collections.Generic;
using SugarDash.Domain.Hypo;

namespace SugarDash.Domain.Metrics
{
    /// <summary>원시 기록에서 계산한 지표(CLAUDE.md 6절). 저장하지 않고 조회할 때마다 계산한다.</summary>
    public sealed class RunMetrics
    {
        public RunMetrics(Ratio accurateResponse, IReadOnlyDictionary<MetricStep, Ratio> stepAccuracy,
            IReadOnlyDictionary<HypoErrorCode, int> errorCounts, IReadOnlyDictionary<HypoStep, double?> medianDecisionSec,
            Ratio measuredFirst, int unnecessaryMeasureCount)
        {
            AccurateResponse = accurateResponse;
            StepAccuracy = stepAccuracy;
            ErrorCounts = errorCounts;
            MedianDecisionSec = medianDecisionSec;
            MeasuredFirst = measuredFirst;
            UnnecessaryMeasureCount = unnecessaryMeasureCount;
        }

        /// <summary>정확 대응률: 4단계를 모두 보정 없이 맞힌 이벤트 ÷ 끝난 이벤트(대처를 마쳤거나 탈락). 재발 이벤트 포함.</summary>
        public Ratio AccurateResponse { get; }
        /// <summary>단계별 정답률: 맞힌 수 ÷ 그 단계까지 간 수.</summary>
        public IReadOnlyDictionary<MetricStep, Ratio> StepAccuracy { get; }
        /// <summary>오류 코드별 횟수. 없는 코드는 들어 있지 않다.</summary>
        public IReadOnlyDictionary<HypoErrorCode, int> ErrorCounts { get; }
        /// <summary>단계별 판단 시간 중앙값(초). 시간 초과는 판단이 아니므로 뺀다. 답이 없으면 null.</summary>
        public IReadOnlyDictionary<HypoStep, double?> MedianDecisionSec { get; }
        /// <summary>측정 선행률: 게이지 경고 전에 측정한 이벤트 ÷ 1단계에 답이 기록된 이벤트. 불필요 측정 횟수와 함께 본다.</summary>
        public Ratio MeasuredFirst { get; }
        /// <summary>실제 혈당이 hypoThreshold 이상일 때 누른 측정 수.</summary>
        public int UnnecessaryMeasureCount { get; }
    }
}
