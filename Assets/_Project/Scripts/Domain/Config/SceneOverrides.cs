using System.Collections.Generic;

namespace SugarDash.Domain.Config
{
    /// <summary>
    /// 장면에 있는 동안 전역 값 대신 쓰는 값. null인 키는 전역 값을 그대로 쓴다.
    /// 1형 키는 suddenDropChancePerSec, trackSnack*, 2형 키는 foodRow*(foodRowIds 포함), rest*다. runDecayPerSec는 두 유형 공통.
    /// </summary>
    public sealed class SceneOverrides
    {
        public static readonly SceneOverrides None = new SceneOverrides();

        public SceneOverrides(double? runDecayPerSec = null, double? suddenDropChancePerSec = null,
            IReadOnlyList<string> trackSnackIds = null, double? trackSnackIntervalMinSec = null,
            double? trackSnackIntervalMaxSec = null, double? foodRowIntervalMinSec = null,
            double? foodRowIntervalMaxSec = null, double? restIntervalMinSec = null, double? restIntervalMaxSec = null,
            IReadOnlyList<string> foodRowIds = null, double? emptyBagChance = null)
        {
            RunDecayPerSec = runDecayPerSec;
            SuddenDropChancePerSec = suddenDropChancePerSec;
            TrackSnackIds = trackSnackIds;
            TrackSnackIntervalMinSec = trackSnackIntervalMinSec;
            TrackSnackIntervalMaxSec = trackSnackIntervalMaxSec;
            FoodRowIntervalMinSec = foodRowIntervalMinSec;
            FoodRowIntervalMaxSec = foodRowIntervalMaxSec;
            RestIntervalMinSec = restIntervalMinSec;
            RestIntervalMaxSec = restIntervalMaxSec;
            FoodRowIds = foodRowIds;
            EmptyBagChance = emptyBagChance;
        }

        public double? RunDecayPerSec { get; }
        public double? SuddenDropChancePerSec { get; }
        /// <summary>1형: 이 장면에서 저혈당이 발동할 때 "빈 간식 가방"(빠른 당 없음)일 확률(docs/section3.md 5절). 없으면 0.</summary>
        public double? EmptyBagChance { get; }
        /// <summary>트랙에 나오는 간식 id 목록. 같은 id를 여러 번 적으면 더 자주 나온다.</summary>
        public IReadOnlyList<string> TrackSnackIds { get; }
        public double? TrackSnackIntervalMinSec { get; }
        public double? TrackSnackIntervalMaxSec { get; }
        public double? FoodRowIntervalMinSec { get; }
        public double? FoodRowIntervalMaxSec { get; }
        public double? RestIntervalMinSec { get; }
        public double? RestIntervalMaxSec { get; }
        /// <summary>음식 줄에 나오는 음식 id 목록(2형). 같은 id를 여러 번 적으면 더 자주 나온다.</summary>
        public IReadOnlyList<string> FoodRowIds { get; }
    }
}
