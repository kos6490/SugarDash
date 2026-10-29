using System.Collections.Generic;

namespace SugarDash.Domain.Metrics
{
    /// <summary>주행이 어떻게 끝났는지.</summary>
    public enum RunOutcome
    {
        Completed,
        Eliminated,
        /// <summary>완주·탈락 기록 없이 끝났다(앱 종료 등).</summary>
        Unfinished,
    }

    /// <summary>결과 화면용 저혈당 이벤트 하나의 결과.</summary>
    public sealed class EventResult
    {
        public EventResult(int eventId, bool isRecurrence, IReadOnlyDictionary<MetricStep, bool?> steps,
            bool allCorrect, bool isFinished)
        {
            EventId = eventId;
            IsRecurrence = isRecurrence;
            Steps = steps;
            AllCorrect = allCorrect;
            IsFinished = isFinished;
        }

        public int EventId { get; }
        public bool IsRecurrence { get; }
        /// <summary>단계별 정답 여부. 그 단계까지 가지 못했으면 null.</summary>
        public IReadOnlyDictionary<MetricStep, bool?> Steps { get; }
        /// <summary>4단계를 모두 보정 없이 맞혔다.</summary>
        public bool AllCorrect { get; }
        /// <summary>대처를 마쳤거나 탈락했다. false면 기록이 중간에 끊긴 이벤트다.</summary>
        public bool IsFinished { get; }
    }

    /// <summary>결과 화면에 보여 줄 주행 하나의 요약.</summary>
    public sealed class RunSummary
    {
        public RunSummary(string runId, RunOutcome outcome, IReadOnlyList<EventResult> events, RunMetrics metrics)
        {
            RunId = runId;
            Outcome = outcome;
            Events = events;
            Metrics = metrics;
        }

        public string RunId { get; }
        public RunOutcome Outcome { get; }
        /// <summary>발동 순서대로.</summary>
        public IReadOnlyList<EventResult> Events { get; }
        /// <summary>이 주행 하나의 지표.</summary>
        public RunMetrics Metrics { get; }
    }
}
