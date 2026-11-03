using System;
using System.Collections.Generic;

namespace SugarDash.Presentation.Runner
{
    /// <summary>
    /// 트랙에 무엇을 어떤 간격으로 놓을지 정한 값. 1형은 간식 한 개씩(foodRows false)이고 쉬는 곳이 없다(간격 0).
    /// 2형은 음식 줄(서로 다른 음식 2개)과 쉬는 곳을 놓는다. 간격은 worldDt 기준 초다.
    /// </summary>
    public sealed class TrackLayout
    {
        public TrackLayout(IReadOnlyList<string> snackIds, double snackIntervalMinSec, double snackIntervalMaxSec,
            bool foodRows, double restIntervalMinSec, double restIntervalMaxSec)
        {
            SnackIds = snackIds;
            SnackIntervalMinSec = snackIntervalMinSec;
            SnackIntervalMaxSec = snackIntervalMaxSec;
            FoodRows = foodRows;
            RestIntervalMinSec = restIntervalMinSec;
            RestIntervalMaxSec = restIntervalMaxSec;
        }

        /// <summary>트랙에 놓을 간식·음식 id.</summary>
        public IReadOnlyList<string> SnackIds { get; }
        public double SnackIntervalMinSec { get; }
        public double SnackIntervalMaxSec { get; }
        /// <summary>true면 간식 대신 음식 줄(서로 다른 음식 2개, 빈 레인 1개)을 놓는다.</summary>
        public bool FoodRows { get; }
        /// <summary>쉬는 곳 간격. 0이면 놓지 않는다.</summary>
        public double RestIntervalMinSec { get; }
        public double RestIntervalMaxSec { get; }

        public bool HasRestSpots => RestIntervalMinSec > 0;

        /// <summary>값이 올바르지 않으면 예외를 던진다.</summary>
        public void Validate()
        {
            if (!(SnackIntervalMinSec > 0)) throw new ArgumentOutOfRangeException(nameof(SnackIntervalMinSec));
            if (!(SnackIntervalMaxSec >= SnackIntervalMinSec)) throw new ArgumentOutOfRangeException(nameof(SnackIntervalMaxSec));
            if (SnackIds == null || SnackIds.Count == 0) throw new ArgumentException("간식이 없다", nameof(SnackIds));
            if (FoodRows && SnackIds.Count < 2) throw new ArgumentException("음식 줄에는 음식이 2개 이상 필요하다", nameof(SnackIds));
            if (RestIntervalMinSec < 0) throw new ArgumentOutOfRangeException(nameof(RestIntervalMinSec));
            if (HasRestSpots && !(RestIntervalMaxSec >= RestIntervalMinSec))
                throw new ArgumentOutOfRangeException(nameof(RestIntervalMaxSec));
        }
    }
}
