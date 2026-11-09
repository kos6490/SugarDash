using System;
using System.Collections.Generic;
using System.Linq;
using SugarDash.App;
using SugarDash.Presentation.Runner;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SugarDash.Presentation.Tests
{
    /// <summary>TrackSpawner의 패턴 배치(docs/track-patterns.md).</summary>
    public class TrackSpawnerPatternTests
    {
        private const PatternCell E = PatternCell.Empty, L = PatternCell.Low, H = PatternCell.High, B = PatternCell.Block;
        private const double Speed = 10;
        private const double Dt = 0.02;
        private static readonly string[] Snacks = { "juice", "cracker" };

        private readonly List<Object> _made = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _made) Object.DestroyImmediate(o);
            _made.Clear();
        }

        private static PatternRow Row(PatternCell l, PatternCell c, PatternCell r, float gap = 0) => new PatternRow(l, c, r, gap);

        private static TrackPattern P(string id, bool easy, float start, float weight, bool mirror, params PatternRow[] rows) =>
            new TrackPattern(id, rows, easy, start, weight, mirror);

        private TrackSpawner NewTrack(TrackLayout layout, int seed, params TrackPattern[] patterns)
        {
            var set = TrackPatternSet.Create(patterns, TrackPatternTests.Timing);
            var go = new GameObject("PatternTrack");
            _made.Add(set);
            _made.Add(go);
            var track = go.AddComponent<TrackSpawner>();
            track.SetPatterns(set);
            track.Init(layout, seed, false);
            return track;
        }

        private TrackSpawner NewType1(int seed, params TrackPattern[] patterns) =>
            NewTrack(new TrackLayout(Snacks, 8, 8, false, 0, 0), seed, patterns);

        /// <summary>새로 놓인 물체(달린 거리 기준 위치, 물체). 초기 배치는 빼지 않고 Init 직후 것도 담는다.</summary>
        private static List<(double At, TrackItem Item)> Placed(TrackSpawner track, double seconds,
            Func<int, bool> easyOnlyAtStep = null, Func<int, bool> allowedAtStep = null)
        {
            var seen = new HashSet<TrackItem>();
            var placed = new List<(double, TrackItem)>();
            void Collect(bool initial)
            {
                foreach (var item in track.Items)
                {
                    if (item.Kind == TrackItemKind.Marker || !seen.Add(item)) continue;
                    if (!initial)
                        Assert.That(item.Z, Is.LessThanOrEqualTo(track.SpawnAheadDistance + 1e-6)
                            .And.GreaterThan(track.SpawnAheadDistance - Speed * Dt - 1e-6), "spawnAheadDistance에만 놓는다");
                    placed.Add((track.Distance + item.Z, item));
                }
            }
            Collect(true);
            int steps = (int)Math.Round(seconds / Dt);
            for (int i = 0; i < steps; i++)
            {
                track.Advance(Dt, Speed, allowedAtStep?.Invoke(i) ?? true, easyOnlyAtStep?.Invoke(i) ?? false);
                Collect(false);
            }
            return placed;
        }

        /// <summary>캐릭터 위치(z = 0)를 지난 물체(지난 거리, 물체). 치워진 물체는 빠진다.</summary>
        private static List<(double At, TrackItem Item)> Crossed(TrackSpawner track, double seconds)
        {
            var crossed = new List<(double, TrackItem)>();
            int steps = (int)Math.Round(seconds / Dt);
            for (int i = 0; i < steps; i++)
                foreach (var item in track.Advance(Dt, Speed, true))
                    if (item.Kind != TrackItemKind.Marker) crossed.Add((track.Distance + item.Z, item));
            return crossed;
        }

        private static List<IGrouping<double, (double At, TrackItem Item)>> Rows(IEnumerable<(double At, TrackItem Item)> items) =>
            items.Where(x => x.Item.IsObstacle).GroupBy(x => Math.Round(x.At, 3)).OrderBy(g => g.Key).ToList();

        private static string Shape(IEnumerable<(double At, TrackItem Item)> row)
        {
            var chars = new[] { '.', '.', '.' };
            foreach (var x in row)
                chars[x.Item.Lane + 1] = x.Item.Kind == TrackItemKind.LowObstacle ? 'L' :
                    x.Item.Kind == TrackItemKind.HighObstacle ? 'H' : 'B';
            return new string(chars);
        }

        [Test]
        public void PatternRows_ArePlacedAtSpawnAhead_InShapeAndGaps()
        {
            var track = NewType1(1, P("two", false, 0, 1, false, Row(B, E, B), Row(L, L, L, 20)));

            var rows = Rows(Placed(track, 30));

            Assert.That(rows.Count, Is.GreaterThanOrEqualTo(6));
            Assert.That(rows[0].Key, Is.EqualTo(track.StartClearDistance).Within(1e-6));
            for (int i = 0; i < rows.Count; i++)
            {
                Assert.That(Shape(rows[i]), Is.EqualTo(i % 2 == 0 ? "B.B" : "LLL"));
                if (i == 0) continue;
                double gap = i % 2 == 1 ? 20 : track.ObstacleSpacing;
                Assert.That(rows[i].Key - rows[i - 1].Key, Is.EqualTo(gap).Within(1e-6));
            }
        }

        [Test]
        public void SameSeed_SameTrack()
        {
            TrackPattern[] Set() => new[]
            {
                P("a", true, 0, 1, true, Row(B, E, E)), P("b", true, 0, 2, false, Row(L, L, L)),
                P("c", false, 0, 1, false, Row(B, H, B), Row(E, L, E, 8)),
            };
            string Track(int seed) => string.Join("|", Rows(Placed(NewType1(seed, Set()), 40)).Select(r => r.Key + Shape(r)));

            Assert.That(Track(3), Is.EqualTo(Track(3)));
            Assert.That(Track(3), Is.Not.EqualTo(Track(4)));
        }

        [Test]
        public void Pattern_NotBeforeStartDistance_ZeroWeightNever()
        {
            var track = NewType1(5, P("a", true, 0, 1, false, Row(L, E, E)), P("late", false, 200, 1, false, Row(H, H, H)),
                P("off", false, 0, 0, false, Row(B, E, B)));

            var rows = Rows(Placed(track, 60));

            Assert.That(rows.Where(r => Shape(r) == "HHH").All(r => r.Key >= 200), Is.True);
            Assert.That(rows.Any(r => Shape(r) == "HHH"), Is.True, "200m 뒤에는 나온다");
            Assert.That(rows.Any(r => Shape(r) == "B.B"), Is.False, "weight 0은 나오지 않는다");
        }

        [Test]
        public void EasyOnly_NewPicksAreEasy_PlacedOnesStay()
        {
            var track = NewType1(2, P("easy", true, 0, 1, false, Row(L, E, E)), P("hard", false, 0, 5, false, Row(B, E, B)));
            int initialRows = Rows(track.Items.Select(i => (i.Z, i))).Count;

            var placed = Placed(track, 40, easyOnlyAtStep: _ => true);

            var rows = Rows(placed);
            var later = rows.Skip(initialRows).ToList();
            Assert.That(later.Count, Is.GreaterThan(5));
            Assert.That(later.All(r => Shape(r) == "L.."), Is.True);
        }

        [Test]
        public void Mirror_UsesBothSides()
        {
            var track = NewType1(9, P("side", true, 0, 1, true, Row(B, E, E)));

            var shapes = Rows(Placed(track, 60)).Select(Shape).ToList();

            Assert.That(shapes, Has.Some.EqualTo("B.."));
            Assert.That(shapes, Has.Some.EqualTo("..B"));
            Assert.That(shapes.All(s => s == "B.." || s == "..B"), Is.True);
        }

        [Test]
        public void ObstaclesNotAllowed_DropsPendingRows()
        {
            var track = NewType1(1, P("long", false, 0, 1, false, Row(B, E, B), Row(H, H, H, 40)));
            // 첫 줄은 30m, 둘째 줄은 70m. 0.5초(5m) 뒤 둘째 줄이 놓이기 전에 장애물을 한 번 막는다.
            int blockStep = (int)Math.Round(0.5 / Dt);

            var placed = Placed(track, 20, allowedAtStep: i => i != blockStep);

            var rows = Rows(placed);
            Assert.That(rows.Any(r => Math.Abs(r.Key - 70) < 1e-3), Is.False, "막힌 동안 대기 중이던 줄은 버린다");
            Assert.That(rows.Any(r => Shape(r) == "HHH"), Is.True, "다음 패턴부터 다시 놓는다");
        }

        [Test]
        public void Snack_NeverNearPatternRowInSameLane_AndWaitsWhenAllLanesBlocked()
        {
            var track = NewTrack(new TrackLayout(Snacks, 0.4, 0.4, false, 0, 0), 4,
                P("wall", true, 0, 1, false, Row(L, L, L)), P("two", false, 0, 1, true, Row(L, H, E), Row(E, L, H, 9)));

            var crossed = Crossed(track, 60);

            var snacks = crossed.Where(x => x.Item.Kind == TrackItemKind.Snack).ToList();
            var obstacles = crossed.Where(x => x.Item.IsObstacle).ToList();
            Assert.That(snacks.Count, Is.GreaterThan(20));
            foreach (var s in snacks)
                Assert.That(obstacles.Any(o => o.Item.Lane == s.Item.Lane && Math.Abs(o.At - s.At) < 4 - Speed * Dt), Is.False,
                    $"{s.At:0.0}m 간식이 같은 레인 장애물과 가깝다");
        }

        [Test]
        public void Type2_FoodRowsAndRestSpots_HaveNoPatternRowsNearby()
        {
            var layout = new TrackLayout(new[] { "juice", "water", "cracker" }, 3, 3, true, 12, 12);
            var track = NewTrack(layout, 6, P("wall", true, 0, 1, false, Row(L, L, L)), P("two", false, 0, 1, false, Row(B, E, B)));

            var crossed = Crossed(track, 60);

            var obstacles = crossed.Where(x => x.Item.IsObstacle).ToList();
            var foods = crossed.Where(x => x.Item.Kind == TrackItemKind.Snack).Select(x => x.At).Distinct().ToList();
            var rests = crossed.Where(x => x.Item.Kind == TrackItemKind.RestSpot).Select(x => x.At).ToList();
            Assert.That(foods.Count, Is.GreaterThan(5));
            Assert.That(rests.Count, Is.GreaterThan(1));
            Assert.That(obstacles.Count, Is.GreaterThan(5));
            foreach (double at in foods.Concat(rests))
                Assert.That(obstacles.Any(o => Math.Abs(o.At - at) < 8 - Speed * Dt), Is.False, $"{at:0.0}m 근처에 장애물이 있다");
        }

        [Test]
        public void EmptySet_FallsBackToSingleObstacles()
        {
            var track = NewType1(1);

            Assert.That(track.UsesPatterns, Is.False);
            var rows = Rows(Placed(track, 20));
            Assert.That(rows.All(r => r.Count() == 1), Is.True);
        }

        [Test]
        public void SetPatterns_AppliesFromNextResetRun()
        {
            var go = new GameObject("Track");
            _made.Add(go);
            var track = go.AddComponent<TrackSpawner>();
            track.Init(new TrackLayout(Snacks, 8, 8, false, 0, 0), 1, false);
            var set = TrackPatternSet.Create(new[] { P("wall", true, 0, 1, false, Row(L, L, L)) }, TrackPatternTests.Timing);
            _made.Add(set);

            track.SetPatterns(set);
            Assert.That(track.UsesPatterns, Is.False);
            track.ResetRun(1);
            Assert.That(track.UsesPatterns, Is.True);
        }
    }
}
