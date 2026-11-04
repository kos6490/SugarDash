namespace SugarDash.Domain.Config
{
    /// <summary>장면 구간 안의 장면(맵 = 하루의 한 때) 하나의 설정.</summary>
    public sealed class SceneConfig
    {
        public SceneConfig(string id, TimeOfDay timeOfDay, double? lengthM, bool spawns, SceneOverrides overrides,
            HabitMoment? habitCheck = null)
        {
            Id = id;
            TimeOfDay = timeOfDay;
            LengthM = lengthM;
            Spawns = spawns;
            Overrides = overrides ?? SceneOverrides.None;
            HabitCheck = habitCheck;
        }

        /// <summary>schoolyard, playground, outing, home 등.</summary>
        public string Id { get; }
        public TimeOfDay TimeOfDay { get; }
        /// <summary>장면 길이(m). 장면은 되풀이되므로 유한해야 한다.</summary>
        public double? LengthM { get; }
        /// <summary>false면 장애물·음식·간식·쉬는 곳을 놓지 않는다(집 장면).</summary>
        public bool Spawns { get; }
        public SceneOverrides Overrides { get; }
        /// <summary>장면에 들어갈 때 여는 혈당 재기 습관 순간. 1형만. 없으면 null.</summary>
        public HabitMoment? HabitCheck { get; }
    }
}
