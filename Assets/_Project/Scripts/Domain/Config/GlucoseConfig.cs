using System.Collections.Generic;

namespace SugarDash.Domain.Config
{
    /// <summary>GlucoseModel과 EventDirector가 쓰는 설정.</summary>
    public sealed class GlucoseConfig
    {
        public GlucoseConfig(TimeConfig time, GlucoseLevelConfig levels, RecurrenceConfig recurrence,
            DisplayConfig display, SuddenDropConfig suddenDrop, IReadOnlyList<SnackConfig> snacks)
        {
            Time = time;
            Levels = levels;
            Recurrence = recurrence;
            Display = display;
            SuddenDrop = suddenDrop;
            Snacks = snacks;
        }

        public TimeConfig Time { get; }
        public GlucoseLevelConfig Levels { get; }
        public RecurrenceConfig Recurrence { get; }
        public DisplayConfig Display { get; }
        public SuddenDropConfig SuddenDrop { get; }
        public IReadOnlyList<SnackConfig> Snacks { get; }
    }
}
