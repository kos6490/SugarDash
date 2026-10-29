using System;
using System.Collections.Generic;

namespace SugarDash.Domain.Records
{
    /// <summary>
    /// 주행 하나의 원시 기록. 기록 파일의 레코드를 발행 순서 그대로 도메인 이벤트 객체(StepAnswered 등)로 되돌린 것이다.
    /// </summary>
    public sealed class RunRecord
    {
        public RunRecord(string runId, string configVersion, DateTime? startedAtUtc, DateTime? endedAtUtc,
            IReadOnlyList<object> events, int skippedLineCount)
        {
            RunId = runId ?? throw new ArgumentNullException(nameof(runId));
            ConfigVersion = configVersion;
            StartedAtUtc = startedAtUtc;
            EndedAtUtc = endedAtUtc;
            Events = events ?? throw new ArgumentNullException(nameof(events));
            SkippedLineCount = skippedLineCount;
        }

        public string RunId { get; }
        public string ConfigVersion { get; }
        public DateTime? StartedAtUtc { get; }
        /// <summary>RunEnded 기록이 없으면(중간 종료) null.</summary>
        public DateTime? EndedAtUtc { get; }
        /// <summary>도메인 이벤트 객체. 주행 시작·종료 레코드는 들어 있지 않다.</summary>
        public IReadOnlyList<object> Events { get; }
        /// <summary>형식이 깨져 읽지 못한 줄 수(예: 쓰기 도중 종료된 마지막 줄).</summary>
        public int SkippedLineCount { get; }
    }
}
