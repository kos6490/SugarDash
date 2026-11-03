using System.Collections.Generic;
using NUnit.Framework;
using SugarDash.Domain.Events;
using SugarDash.Domain.Hypo;
using SugarDash.Domain.Records;
using SugarDash.Domain.Scoring;

namespace SugarDash.Domain.Tests
{
    public class ScoringServiceTests
    {
        private const double Eps = 1e-9;

        private static ScoringService Make(ConfigBuilder builder = null) =>
            new ScoringService((builder ?? new ConfigBuilder()).Build().Score);

        private static StepAnswered Answer(HypoStep step, bool correct, HypoErrorCode code) =>
            new StepAnswered(1, step, null, correct, code, false, 1, false, 0, 0);

        [Test]
        public void InRange_AccumulatesUntilExit()
        {
            var s = Make();
            s.Apply(new GlucoseRangeEntered(120, 10, 10));
            s.Apply(new GlucoseRangeExited(60, 25, 25));
            Assert.That(s.Total(40, 0), Is.EqualTo(15).Within(Eps));
        }

        [Test]
        public void OpenRange_CountsUntilNow()
        {
            var s = Make();
            s.Apply(new GlucoseRangeEntered(120, 10, 10));
            Assert.That(s.Total(30, 0), Is.EqualTo(20).Within(Eps));
        }

        [Test]
        public void FastForwardInRange_IsExcluded()
        {
            var s = Make();
            s.Apply(new GlucoseRangeEntered(120, 0, 0));
            s.Apply(new RecheckFastForward(1, 6, 100, 90, 16, 16));
            Assert.That(s.Total(20, 0), Is.EqualTo(14).Within(Eps));
        }

        [Test]
        public void FastForwardOutOfRange_ChangesNothing()
        {
            var s = Make();
            s.Apply(new RecheckFastForward(1, 6, 60, 50, 6, 6));
            s.Apply(new GlucoseRangeEntered(120, 20, 20));
            Assert.That(s.Total(30, 0), Is.EqualTo(10).Within(Eps));
        }

        [Test]
        public void Choices_UseWeights()
        {
            var b = new ConfigBuilder();
            b.ErrorWeightOverrides[HypoErrorCode.DsUnder] = 3;
            var s = Make(b);
            s.Apply(Answer(HypoStep.Recognize, true, HypoErrorCode.None));
            s.Apply(Answer(HypoStep.Dose, false, HypoErrorCode.DsUnder));
            s.Apply(Answer(HypoStep.Respond, false, HypoErrorCode.RsTimeout));
            Assert.That(s.ChoicePoints, Is.EqualTo(13).Within(Eps));
        }

        [Test]
        public void Distance_UsesPerMeter()
        {
            Assert.That(Make().Total(0, 250), Is.EqualTo(25).Within(Eps));
        }

        [Test]
        public void IgnoresOtherEvents()
        {
            var s = Make();
            s.Apply("not an event");
            Assert.That(s.Total(0, 0), Is.EqualTo(0));
        }

        private static List<object> Events() => new List<object>
        {
            new GlucoseRangeEntered(120, 0, 0),
            Answer(HypoStep.Recognize, true, HypoErrorCode.None),
            new RecheckFastForward(1, 6, 100, 90, 16, 16),
            new GlucoseRangeExited(60, 30, 30),
        };

        [Test]
        public void Calculate_MatchesLiveTotal()
        {
            var config = new ConfigBuilder().Build().Score;
            var events = Events();
            var all = new List<object>(events) { new RunEliminated(0, null, HypoErrorCode.None, EliminationCause.GlucoseLow, 100, 50, 50) };
            var run = new RunRecord("r", "test", null, null, all, 0);

            var live = new ScoringService(config);
            foreach (var e in events) live.Apply(e);

            Assert.That(ScoringService.Calculate(run, config), Is.EqualTo(live.Total(50, 100)).Within(Eps));
        }

        [Test]
        public void Calculate_WithoutElimination_IsNull()
        {
            var run = new RunRecord("r", "test", null, null, Events(), 0);
            Assert.That(ScoringService.Calculate(run, new ConfigBuilder().Build().Score), Is.Null);
        }

        [Test]
        public void Score_Type2_Recalculation()
        {
            var b = new ConfigBuilder();
            b.ErrorWeightOverrides[HypoErrorCode.HyJuice] = 2;
            var config = b.BuildType2().Score;
            var events = new List<object>
            {
                new RestStarted(200, 0, 0),
                new FoodEaten("juice", 210, 5, 5),
                new RestEnded(200, 8, 8),
                new GlucoseRangeEntered(120, 10, 10),
                Answer(HypoStep.LowNotify, true, HypoErrorCode.None),
                Answer(HypoStep.HyperNotify, false, HypoErrorCode.HyJuice),
                new GlucoseRangeExited(260, 30, 30),
                new RunEliminated(0, null, HypoErrorCode.None, EliminationCause.ObstacleHit, 200, 40, 40),
            };
            var run = new RunRecord("r", "test", null, null, events, 0, SugarDash.Domain.Config.TrackType.Type2);

            // 선택 10 + 2, 범위 20초 * 1, 거리 200m * 0.1 = 52. 먹기·쉬기는 점수에 영향 없음.
            Assert.That(ScoringService.Calculate(run, config), Is.EqualTo(52).Within(Eps));
        }
    }
}
