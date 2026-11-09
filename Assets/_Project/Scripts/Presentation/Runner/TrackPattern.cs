using System;
using System.Collections.Generic;
using UnityEngine;

namespace SugarDash.Presentation.Runner
{
    /// <summary>패턴 줄의 한 레인에 놓을 것.</summary>
    public enum PatternCell
    {
        /// <summary>아무것도 없다. 그냥 지나간다.</summary>
        Empty,
        /// <summary>낮은 장애물. 점프로 지나간다.</summary>
        Low,
        /// <summary>높은 장애물. 슬라이드로 지나간다.</summary>
        High,
        /// <summary>레인 막이. 이 레인으로는 못 지나간다.</summary>
        Block,
    }

    /// <summary>패턴의 줄 하나. 세 레인에 놓을 것과 앞 줄과의 거리.</summary>
    [Serializable]
    public sealed class PatternRow
    {
        [SerializeField] private PatternCell left;
        [SerializeField] private PatternCell center;
        [SerializeField] private PatternCell right;
        [Tooltip("앞 줄과의 거리(m). 첫 줄은 무시한다.")]
        [SerializeField] private float gapBeforeM;

        public PatternRow(PatternCell left, PatternCell center, PatternCell right, float gapBeforeM = 0)
        {
            this.left = left;
            this.center = center;
            this.right = right;
            this.gapBeforeM = gapBeforeM;
        }

        public float GapBeforeM => gapBeforeM;

        /// <param name="lane">-1(왼쪽), 0(가운데), 1(오른쪽).</param>
        public PatternCell CellAt(int lane)
        {
            if (lane < 0) return left;
            return lane == 0 ? center : right;
        }
    }

    /// <summary>
    /// 장애물 패턴 하나(docs/track-patterns.md 2절).
    /// 줄을 앞에서부터 차례로 놓는다. 고르기만 무작위이고 모양은 정한 그대로다.
    /// </summary>
    [Serializable]
    public sealed class TrackPattern
    {
        [Tooltip("패턴 이름(영어 소문자). 규칙 확인 메시지에 나온다.")]
        [SerializeField] private string id;
        [SerializeField] private List<PatternRow> rows = new List<PatternRow>();
        [Tooltip("쉬운 패턴. 줄이 하나여야 한다. 시작 직후와 저혈당 1단계 동안 쓴다.")]
        [SerializeField] private bool easy;
        [Tooltip("이 판의 누적 거리(m)가 이 값 이상일 때부터 나온다.")]
        [SerializeField] private float startDistanceM;
        [Tooltip("뽑힐 비율. 0이면 나오지 않는다.")]
        [SerializeField] private float weight = 1;
        [Tooltip("켜면 좌우를 뒤집은 모양도 같은 비율로 나온다.")]
        [SerializeField] private bool mirror;

        public TrackPattern(string id, IEnumerable<PatternRow> rows, bool easy, float startDistanceM, float weight,
            bool mirror)
        {
            this.id = id;
            this.rows = rows != null ? new List<PatternRow>(rows) : new List<PatternRow>();
            this.easy = easy;
            this.startDistanceM = startDistanceM;
            this.weight = weight;
            this.mirror = mirror;
        }

        public string Id => id;
        public IReadOnlyList<PatternRow> Rows => rows ?? (rows = new List<PatternRow>());
        public bool Easy => easy;
        public float StartDistanceM => startDistanceM;
        public float Weight => weight;
        public bool Mirror => mirror;
    }

    /// <summary>
    /// 지나갈 수 있는지 볼 때 쓰는 캐릭터 기준 값. PlayerController 값과 같아야 한다.
    /// 반응 시간은 한 동작이 끝난 뒤 다음 동작을 시작할 때까지 주는 여유다.
    /// </summary>
    [Serializable]
    public sealed class PatternTiming
    {
        [SerializeField] private float maxRunSpeed = 20f;
        [SerializeField] private float jumpSec = 0.7f;
        [SerializeField] private float laneChangeSec = 0.15f;
        [SerializeField] private float reactionSec = 0.2f;

        public PatternTiming(float maxRunSpeed, float jumpSec, float laneChangeSec, float reactionSec)
        {
            this.maxRunSpeed = maxRunSpeed;
            this.jumpSec = jumpSec;
            this.laneChangeSec = laneChangeSec;
            this.reactionSec = reactionSec;
        }

        public float MaxRunSpeed => maxRunSpeed;
        public float JumpSec => jumpSec;
        public float LaneChangeSec => laneChangeSec;
        public float ReactionSec => reactionSec;
    }
}
