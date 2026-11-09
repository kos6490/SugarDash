using System.Collections.Generic;
using SugarDash.Presentation.Runner;
using UnityEngine;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 장애물 패턴 묶음 에셋(docs/track-patterns.md). TrackSpawner가 여기서 패턴을 골라 놓는다.
    /// 패턴 목록(Patterns)은 월드 담당이 Inspector에서 만들고 고친다. 기준 값(Timing)은 구조 담당만 바꾼다.
    /// 고칠 때마다 규칙을 확인해 문제가 있으면 Console에 경고를 띄운다.
    /// </summary>
    [CreateAssetMenu(menuName = "SugarDash/Track Pattern Set", fileName = "TrackPatterns")]
    public sealed class TrackPatternSet : ScriptableObject
    {
        [Tooltip("장애물 패턴 목록. 월드 담당이 만든다.")]
        [SerializeField] private List<TrackPattern> patterns = new List<TrackPattern>();
        [Tooltip("지나갈 수 있는지 볼 때 쓰는 캐릭터 기준 값. PlayerController 값과 같아야 한다. 구조 담당만 바꾼다.")]
        [SerializeField] private PatternTiming timing = new PatternTiming(20f, 0.7f, 0.15f, 0.2f);

        public IReadOnlyList<TrackPattern> Patterns => patterns;
        public PatternTiming Timing => timing;

        /// <summary>코드와 테스트에서 묶음을 만든다.</summary>
        public static TrackPatternSet Create(IEnumerable<TrackPattern> patterns, PatternTiming timing)
        {
            var set = CreateInstance<TrackPatternSet>();
            set.patterns = new List<TrackPattern>(patterns);
            set.timing = timing;
            return set;
        }

        /// <summary>규칙 문제마다 한 줄. 문제가 없으면 빈 목록.</summary>
        public List<string> Validate() => TrackPatternRules.Validate(patterns, timing);

        private void OnValidate()
        {
            foreach (string problem in Validate()) Debug.LogWarning($"{name}: {problem}", this);
        }
    }
}
