using System;
using System.Linq;
using SugarDash.Domain.Config;
using SugarDash.Domain.Director;
using SugarDash.Domain.Events;
using SugarDash.Domain.Glucose;
using SugarDash.Domain.Hypo;
using SugarDash.Domain.Ports;
using NUnit.Framework;

namespace SugarDash.Domain.Tests
{
    /// <summary>1형 혈당 재기 습관 순간(docs/section2.md 7.1절). 판정은 EventDirector, 측정은 HypoEventFsm.PressMeter.</summary>
    public class HabitCheckTests
    {
        private const double Eps = 1e-9;
        private const double Window = 5;

        private static HypoHarness Started(HabitMoment moment = HabitMoment.PreExercise)
        {
            var h = new HypoHarness();
            h.Director.StartHabitPrompt(moment, Window);
            return h;
        }

        [Test]
        public void Start_ShowsPrompt_WorldKeepsRunning()
        {
            var h = Started(HabitMoment.Bedtime);

            var started = h.Last<HabitPromptStarted>();
            Assert.That(started.Moment, Is.EqualTo(HabitMoment.Bedtime));
            Assert.That(started.WindowSec, Is.EqualTo(Window));
            Assert.That(h.Director.ActiveHabitMoment, Is.EqualTo(HabitMoment.Bedtime));
            Assert.That(h.Fsm.IsWorldFrozen, Is.False);
            Assert.That(h.All<HabitCheckEnded>(), Is.Empty);
        }

        [Test]
        public void PressMeter_InWindow_IsChecked_AndNotUnnecessary()
        {
            var h = Started();
            h.Frames(2);

            h.Fsm.PressMeter();

            var meter = h.Last<MeterPressed>();
            Assert.That(meter.HabitMoment, Is.EqualTo(HabitMoment.PreExercise));
            Assert.That(meter.Unnecessary, Is.False, "혈당이 기준 이상이어도 습관 순간의 측정은 불필요 측정이 아니다");
            Assert.That(meter.EventId, Is.EqualTo(0));
            var ended = h.Last<HabitCheckEnded>();
            Assert.That(ended.Outcome, Is.EqualTo(HabitCheckOutcome.Checked));
            Assert.That(ended.ReactionSec, Is.EqualTo(2).Within(Eps));
            Assert.That(h.Director.ActiveHabitMoment, Is.Null);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Idle));
        }

        [Test]
        public void WindowPasses_IsMissed_ThenMeterIsOrdinary()
        {
            var h = Started();
            h.Frames(4);
            Assert.That(h.All<HabitCheckEnded>(), Is.Empty);

            h.Frame(1);

            var ended = h.All<HabitCheckEnded>().Single();
            Assert.That(ended.Outcome, Is.EqualTo(HabitCheckOutcome.Missed));
            Assert.That(ended.ReactionSec, Is.Null);
            Assert.That(h.Director.ActiveHabitMoment, Is.Null);

            h.Fsm.PressMeter();
            Assert.That(h.Last<MeterPressed>().HabitMoment, Is.Null);
            Assert.That(h.Last<MeterPressed>().Unnecessary, Is.True);
            Assert.That(h.All<HabitCheckEnded>(), Has.Count.EqualTo(1));
        }

        [Test]
        public void PauseDoesNotUseUpWindow()
        {
            var h = Started();
            h.Pause(10);

            Assert.That(h.Director.ActiveHabitMoment, Is.Not.Null);
        }

        [Test]
        public void HypoStartsDuringPrompt_IsSkipped_MeterGoesToRecognize()
        {
            var h = Started();
            h.Model.ResetTo(72);
            h.Frames(2); // 달리기 감소 2/s → 68에서 발동

            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Recognize));
            Assert.That(h.Last<HabitCheckEnded>().Outcome, Is.EqualTo(HabitCheckOutcome.Skipped));
            Assert.That(h.Director.ActiveHabitMoment, Is.Null);

            h.Fsm.PressMeter();
            Assert.That(h.Last<MeterPressed>().HabitMoment, Is.Null);
            Assert.That(h.Last<MeterPressed>().EventId, Is.EqualTo(1));
            Assert.That(h.All<HabitCheckEnded>(), Has.Count.EqualTo(1));
        }

        [Test]
        public void StartDuringHyperNotify_IsSkipped()
        {
            var h = new HypoHarness();
            h.Model.EatSnack("juice", 5);
            h.Frames(3);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.HyperNotify));

            h.Director.StartHabitPrompt(HabitMoment.Bedtime, Window);

            Assert.That(h.All<HabitPromptStarted>(), Is.Empty);
            var ended = h.Last<HabitCheckEnded>();
            Assert.That(ended.Moment, Is.EqualTo(HabitMoment.Bedtime));
            Assert.That(ended.Outcome, Is.EqualTo(HabitCheckOutcome.Skipped));
            Assert.That(h.Director.ActiveHabitMoment, Is.Null);
        }

        [Test]
        public void StartDuringGrace_IsSkipped()
        {
            var h = new HypoHarness();
            h.Model.EatSnack("juice", 5);
            h.Frames(3);
            h.Fsm.PressHelp(HypoPhase.HyperNotify);
            h.Frames(1);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Grace));

            h.Director.StartHabitPrompt(HabitMoment.PreExercise, Window);

            Assert.That(h.All<HabitPromptStarted>(), Is.Empty);
            Assert.That(h.Last<HabitCheckEnded>().Outcome, Is.EqualTo(HabitCheckOutcome.Skipped));
        }

        [Test]
        public void StartWhilePreviousOpen_EndsPreviousAsMissed()
        {
            var h = Started(HabitMoment.PreExercise);
            h.Frames(1);

            h.Director.StartHabitPrompt(HabitMoment.Bedtime, Window);

            var ended = h.All<HabitCheckEnded>().Single();
            Assert.That(ended.Moment, Is.EqualTo(HabitMoment.PreExercise));
            Assert.That(ended.Outcome, Is.EqualTo(HabitCheckOutcome.Missed));
            Assert.That(h.Director.ActiveHabitMoment, Is.EqualTo(HabitMoment.Bedtime));
        }

        [Test]
        public void Type2Director_Throws()
        {
            var config = new ConfigBuilder().BuildType2();
            var model = new GlucoseModel(config.Glucose);
            var director = new EventDirector(config.Glucose, model, new ZeroRandom(), new DomainEventBus(), config.Type2);

            Assert.Throws<InvalidOperationException>(() => director.StartHabitPrompt(HabitMoment.Bedtime, Window));
        }

        private sealed class ZeroRandom : IRandomSource
        {
            public double NextDouble() => 0;
        }
    }
}
