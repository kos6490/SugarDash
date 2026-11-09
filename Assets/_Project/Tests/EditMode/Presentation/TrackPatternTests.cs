using System.Collections.Generic;
using System.Linq;
using SugarDash.Presentation.Runner;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SugarDash.Presentation.Tests
{
    public class TrackPatternTests
    {
        // 최고 속도 20, 점프 0.7, 레인 이동 0.15, 반응 0.2.
        // 같은 레인 Low→Low: 20 × (0.2 + 0.7) = 18m. 두 레인 이동: 20 × (0.2 + 0.3) = 10m. Low→High: 20 × 0.2 = 4m.
        internal static readonly PatternTiming Timing = new PatternTiming(20f, 0.7f, 0.15f, 0.2f);

        private const PatternCell E = PatternCell.Empty, L = PatternCell.Low, H = PatternCell.High, B = PatternCell.Block;

        internal static PatternRow Row(PatternCell left, PatternCell center, PatternCell right, float gap = 0) =>
            new PatternRow(left, center, right, gap);

        internal static TrackPattern Pattern(string id, bool easy, params PatternRow[] rows) =>
            new TrackPattern(id, rows, easy, 0, 1, false);

        private static readonly TrackPattern StartEasy = Pattern("start", true, Row(L, E, E));

        private static List<string> Check(params TrackPattern[] patterns) =>
            TrackPatternRules.Validate(patterns.Concat(new[] { StartEasy }).ToList(), Timing);

        [Test]
        public void AllBlockRow_Fails()
        {
            var problems = Check(Pattern("wall", false, Row(B, B, B)));
            Assert.That(problems, Has.Count.EqualTo(1));
            Assert.That(problems[0], Does.Contain("wall"));
        }

        [Test]
        public void AllLow_AllHigh_AndMixedRows_Pass()
        {
            Assert.That(Check(Pattern("jump", true, Row(L, L, L)), Pattern("duck", true, Row(H, H, H)),
                Pattern("mix", false, Row(B, L, H))), Is.Empty);
        }

        [Test]
        public void SameLaneLowThenLow_NeedsLandingTime()
        {
            Assert.That(Check(Pattern("short", false, Row(B, L, B), Row(B, L, B, 17))), Has.Count.EqualTo(1));
            Assert.That(Check(Pattern("ok", false, Row(B, L, B), Row(B, L, B, 18))), Is.Empty);
        }

        [Test]
        public void LowThenHigh_ShortGapPasses_BecauseSlideCancelsJump()
        {
            Assert.That(Check(Pattern("jump_duck", false, Row(B, L, B), Row(B, H, B, 4))), Is.Empty);
            Assert.That(Check(Pattern("too_close", false, Row(B, L, B), Row(B, H, B, 3))), Has.Count.EqualTo(1));
        }

        [Test]
        public void FarLaneMove_NeedsLaneChangeTime()
        {
            Assert.That(Check(Pattern("zig", false, Row(E, B, B), Row(B, B, E, 9))), Has.Count.EqualTo(1));
            Assert.That(Check(Pattern("zig", false, Row(E, B, B), Row(B, B, E, 10))), Is.Empty);
        }

        [Test]
        public void PathMustContinueThroughEveryRow()
        {
            // 2번째 줄은 가운데로만 이어지고(7m), 3번째 줄은 오른쪽뿐이라 가운데에서 7m면 된다.
            Assert.That(Check(Pattern("chain", false, Row(E, B, B), Row(B, E, B, 7), Row(B, B, E, 7))), Is.Empty);
            // 2번째 줄에서 왼쪽은 이어지지 않는다(Block). 오른쪽은 너무 멀다(10m 필요).
            Assert.That(Check(Pattern("broken", false, Row(E, B, B), Row(B, B, E, 7))), Has.Count.EqualTo(1));
        }

        [Test]
        public void EasyPattern_MustHaveOneRow()
        {
            var problems = Check(Pattern("easy_two", true, Row(L, E, E), Row(E, E, H, 20)));
            Assert.That(problems.Single(), Does.Contain("easy_two"));
        }

        [Test]
        public void NeedsStartingEasyPattern()
        {
            var late = new TrackPattern("late", new[] { Row(L, E, E) }, true, 100, 1, false);
            var zero = new TrackPattern("zero", new[] { Row(L, E, E) }, true, 0, 0, false);
            var hard = Pattern("hard", false, Row(B, E, B));
            Assert.That(TrackPatternRules.Validate(new[] { late, zero, hard }, Timing), Has.Count.EqualTo(1));
        }

        [Test]
        public void DuplicateOrEmptyId_EmptyRows_NegativeWeight_Fail()
        {
            Assert.That(Check(Pattern("start", false, Row(B, E, B))).Single(), Does.Contain("겹친다"));
            Assert.That(Check(Pattern("", false, Row(B, E, B))).Single(), Does.Contain("id"));
            Assert.That(Check(Pattern("none", false)).Single(), Does.Contain("none"));
            var negative = new TrackPattern("neg", new[] { Row(B, E, B) }, false, 0, -1, false);
            Assert.That(Check(negative).Single(), Does.Contain("neg"));
        }

        [Test]
        public void EmptySet_Fails()
        {
            Assert.That(TrackPatternRules.Validate(new TrackPattern[0], Timing), Is.Not.Empty);
        }

        [Test]
        public void Spacing_18mPasses_17mFails()
        {
            // 최악: 반응 0.2 + max(두 레인 0.3, 점프 0.7) = 0.9초 × 20 = 18m.
            Assert.That(TrackPatternRules.ValidateSpacing(18, Timing), Is.Null);
            Assert.That(TrackPatternRules.ValidateSpacing(17, Timing), Is.Not.Null);
        }

        [Test]
        public void Set_Validate_UsesItsTiming()
        {
            var set = TrackPatternSet.Create(new[] { StartEasy, Pattern("wall", false, Row(B, B, B)) }, Timing);
            try
            {
                Assert.That(set.Validate().Single(), Does.Contain("wall"));
                Assert.That(set.Timing, Is.SameAs(Timing));
            }
            finally
            {
                Object.DestroyImmediate(set);
            }
        }

        private const string ProjectAssetPath = "Assets/_Project/Art/World/TrackPatterns.asset";

        /// <summary>DevRun에 연결된 패턴 묶음이 규칙에 맞는지. 월드 담당이 패턴을 고친 뒤 push 전에 이 테스트로 확인한다.</summary>
        [Test]
        public void ProjectAsset_PassesRules_AndSpacing()
        {
            var set = UnityEditor.AssetDatabase.LoadAssetAtPath<TrackPatternSet>(ProjectAssetPath);
            Assert.That(set, Is.Not.Null, ProjectAssetPath + "가 없다");
            Assert.That(set.Validate(), Is.Empty);

            var go = new GameObject("Track");
            try
            {
                var track = go.AddComponent<TrackSpawner>();
                Assert.That(TrackPatternRules.ValidateSpacing(track.ObstacleSpacing, set.Timing), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ProjectAsset_TimingMatchesPlayerDefaults()
        {
            var set = UnityEditor.AssetDatabase.LoadAssetAtPath<TrackPatternSet>(ProjectAssetPath);
            Assert.That(set, Is.Not.Null, ProjectAssetPath + "가 없다");
            AssertTimingMatchesPlayer(set.Timing);
        }

        [Test]
        public void DefaultTiming_MatchesPlayerControllerDefaults()
        {
            var set = ScriptableObject.CreateInstance<TrackPatternSet>();
            try
            {
                AssertTimingMatchesPlayer(set.Timing);
            }
            finally
            {
                Object.DestroyImmediate(set);
            }
        }

        private static void AssertTimingMatchesPlayer(PatternTiming timing)
        {
            var go = new GameObject("Player");
            try
            {
                var player = go.AddComponent<PlayerController>();
                Assert.That(timing.MaxRunSpeed, Is.EqualTo(player.MaxRunSpeed));
                Assert.That(timing.JumpSec, Is.EqualTo(player.JumpSec));
                Assert.That(timing.LaneChangeSec, Is.EqualTo(player.LaneChangeSec));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
