using System;
using System.Collections.Generic;
using SugarDash.Presentation.Runner;
using UnityEngine;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 트랙 물체 종류별 프리팹 묶음(Inspector에서 채움). 빈 칸은 null을 돌려주고, 그러면
    /// TrackItemLook이 다음 묶음이나 기본 도형으로 넘어간다. 프리팹은 발밑(바닥)을 기준점으로 만든다.
    /// </summary>
    // [영역 파일] 담당: 월드
    [Serializable]
    public sealed class TrackItemPrefabSet
    {
        [Tooltip("점프로 넘는 낮은 장애물")]
        public GameObject lowObstacle;
        [Tooltip("슬라이드로 밑을 지나가는 높은 장애물")]
        public GameObject highObstacle;
        [Tooltip("옆 레인으로 피하는 레인 막이")]
        public GameObject laneBlock;
        [Tooltip("2형 쉬는 곳(벤치, 게임기 등)")]
        public GameObject restSpot;
        [Tooltip("달리는 느낌을 주는 바닥 표시")]
        public GameObject marker;
        [Tooltip("음식 id별 프리팹. 예: water, juice, soda, apple, chicken, pizza, burger, cracker")]
        public List<WorldKeyedObject> foods = new List<WorldKeyedObject>();
        [Tooltip("foods에 없는 음식에 쓰는 프리팹. 비워 두면 다음 묶음이나 기본 도형으로 넘어간다.")]
        public GameObject fallbackFood;

        /// <summary>종류(와 음식 id)에 맞는 프리팹. 이 묶음에 없으면 null.</summary>
        public GameObject For(TrackItemKind kind, string foodId)
        {
            switch (kind)
            {
                case TrackItemKind.LowObstacle: return lowObstacle;
                case TrackItemKind.HighObstacle: return highObstacle;
                case TrackItemKind.LaneBlock: return laneBlock;
                case TrackItemKind.RestSpot: return restSpot;
                case TrackItemKind.Marker: return marker;
                case TrackItemKind.Snack:
                    var food = WorldKeyedObject.Find(foods, foodId);
                    return food != null ? food : fallbackFood;
                default: return null;
            }
        }
    }

    /// <summary>
    /// 장면 하나(운동장, 놀이터 등)에서만 쓰는 프리팹 묶음. 장면별 장애물 모양(운동장 축구공, 놀이터 미끄럼틀 등)을 여기에 넣는다.
    /// 빈 칸은 기본 묶음을 쓴다. 장애물 종류 3개(낮은 것, 높은 것, 레인 막이)의 피하는 방법은 모양이 바뀌어도 그대로다.
    /// </summary>
    // [영역 파일] 담당: 월드
    [Serializable]
    public sealed class SceneTrackItemSet
    {
        [Tooltip("설정의 장면 id: schoolyard, playground, outing, home")]
        public string sceneId;
        public TrackItemPrefabSet prefabs = new TrackItemPrefabSet();

        /// <summary>장면 id가 같은 첫 묶음. 없으면 null.</summary>
        public static TrackItemPrefabSet Find(IReadOnlyList<SceneTrackItemSet> sets, string sceneId)
        {
            if (sets == null || string.IsNullOrEmpty(sceneId)) return null;
            for (int i = 0; i < sets.Count; i++)
            {
                if (sets[i] != null && sets[i].sceneId == sceneId) return sets[i].prefabs;
            }
            return null;
        }
    }
}
