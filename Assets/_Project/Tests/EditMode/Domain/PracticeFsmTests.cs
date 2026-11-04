using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SugarDash.Domain.Config;
using SugarDash.Domain.Director;
using SugarDash.Domain.Events;
using SugarDash.Domain.Glucose;
using SugarDash.Domain.Hypo;
using SugarDash.Domain.Ports;

namespace SugarDash.Domain.Tests
{
    /// <summary>HypoEventFsm 연습 모드: 오답은 부작용 없이 같은 단계를 다시 열고, 시간 초과가 없다.</summary>
    public class PracticeFsmTests
    {
        private const double Eps = 1e-9;

        private sealed class NoRandom : IRandomSource
        {
            public double NextDouble() => throw new InvalidOperationException("난수를 읽지 않아야 한다");
        }

        private sealed class PHarness
        {
            public readonly GlucoseModel Model;
            public readonly EventDirector Director;
            public readonly HypoEventFsm Fsm;
            public readonly List<object> Events = new List<object>();

            public PHarness(bool type2, Action<ConfigBuilder> configure = null, bool practice = true)
            {
                var builder = new ConfigBuilder { RunDecayPerSec = 2 };
                configure?.Invoke(builder);
                var config = type2 ? builder.BuildType2() : builder.Build();
                Assert.That(GameConfigValidator.Validate(config), Is.Empty);
                var bus = new DomainEventBus();
                bus.Subscribe<StepAnswered>(e => Events.Add(e));
                bus.Subscribe<GuardianPromptStarted>(e => Events.Add(e));
                bus.Subscribe<GuardianNotified>(e => Events.Add(e));
                bus.Subscribe<DoseApplied>(e => Events.Add(e));
                bus.Subscribe<RunEliminated>(e => Events.Add(e));
                bus.Subscribe<PracticeRetried>(e => Events.Add(e));
                bus.Subscribe<HypoResolved>(e => Events.Add(e));
                bus.Subscribe<GraceEnded>(e => Events.Add(e));
                Model = new GlucoseModel(config.Glucose);
                Director = type2
                    ? new EventDirector(config.Glucose, Model, new NoRandom(), bus, config.Type2)
                    : new EventDirector(config.Glucose, Model, new NoRandom(), bus);
                Fsm = new HypoEventFsm(config, Model, Director, bus, () => 0);
                Fsm.SetPractice(practice);
                bus.Subscribe<HypoEventTriggered>(Fsm.Begin);
                bus.Subscribe<HyperEventTriggered>(Fsm.BeginHyper);
                bus.Subscribe<LowEventTriggered>(Fsm.BeginLow);
            }

            public IReadOnlyList<T> All<T>() => Events.OfType<T>().ToList();

            public void Frame(double dt)
            {
                Model.Tick(dt, Fsm.IsWorldFrozen ? 0 : dt, dt, 1);
                Director.Evaluate(dt);
                Fsm.Update();
            }

            public void Frames(int n, double dt = 1)
            {
                for (int i = 0; i < n; i++) Frame(dt);
            }

            public void Until(HypoPhase phase)
            {
                for (int i = 0; i < 100 && Fsm.Phase != phase; i++) Frame(1);
                Assert.That(Fsm.Phase, Is.EqualTo(phase));
            }
        }

        private static PHarness Hypo(Action<ConfigBuilder> configure = null, bool practice = true)
        {
            var h = new PHarness(false, configure, practice);
            h.Until(HypoPhase.Recognize);
            return h;
        }

        private static PHarness ToPhase(HypoPhase target, Action<ConfigBuilder> configure = null)
        {
            var h = Hypo(configure);
            h.Fsm.PressMeter();
            if (target == HypoPhase.Respond) return h;
            Assert.That(h.Fsm.ChooseSnack("juice"), Is.True);
            if (target == HypoPhase.Dose) return h;
            Assert.That(h.Fsm.ChooseDose(DoseLevel.Proper), Is.True);
            if (target == HypoPhase.Remeasure) return h;
            Assert.That(h.Fsm.ChooseRecheck(), Is.True);
            return h;
        }

        private static PHarness HyperT2()
        {
            var h = new PHarness(true, b => b.RunDecayPerSec = 0);
            h.Model.EatSnack("juice", 5);
            h.Until(HypoPhase.HyperNotify);
            return h;
        }

        private static PHarness LowT2()
        {
            var h = new PHarness(true, b => { b.RunDecayPerSec = 20; b.Min = 5; b.GlucoseLow = 10; });
            h.Until(HypoPhase.LowNotify);
            return h;
        }

        private static void AssertStayed(PHarness h, HypoPhase phase, HypoStep step, string choice)
        {
            Assert.That(h.Fsm.Phase, Is.EqualTo(phase));
            Assert.That(h.All<PracticeRetried>(), Has.Count.EqualTo(1));
            var r = h.All<PracticeRetried>()[0];
            Assert.That(r.Step, Is.EqualTo(step));
            Assert.That(r.ChoiceId, Is.EqualTo(choice));
            Assert.That(h.All<StepAnswered>().Where(a => !a.IsCorrect), Is.Empty);
            Assert.That(h.All<GuardianPromptStarted>(), Is.Empty);
            Assert.That(h.All<GuardianNotified>(), Is.Empty);
            Assert.That(h.All<RunEliminated>(), Is.Empty);
            Assert.That(h.Fsm.PracticeRetryCount, Is.EqualTo(1));
        }

        [Test]
        public void Practice_DefaultOff()
        {
            var h = Hypo(practice: false);
            Assert.That(h.Fsm.IsPractice, Is.False);
            h.Fsm.SetPractice(true);
            Assert.That(h.Fsm.IsPractice, Is.True);
        }

        [TestCase("cracker")]
        [TestCase("no_sugar")]
        [TestCase("keep_running")]
        [TestCase("help")]
        public void Practice_WrongChoices_NoSideEffects_StayInPhase_Respond(string choice)
        {
            var h = ToPhase(HypoPhase.Respond);
            var control = ToPhase(HypoPhase.Respond);
            control.Fsm.SetPractice(false);
            switch (choice)
            {
                case "cracker": h.Fsm.ChooseSnack("cracker"); break;
                case "no_sugar": h.Fsm.ChooseNoSugar(); break;
                case "keep_running": h.Fsm.ChooseKeepRunning(); break;
                default: h.Fsm.PressHelp(HypoPhase.Respond); break;
            }
            Assert.That(h.Fsm.DoseSnackId, Is.Null);
            AssertStayed(h, HypoPhase.Respond, HypoStep.Respond, choice);
            h.Frames(3); control.Frames(3);
            Assert.That(h.Model.Actual, Is.EqualTo(control.Model.Actual).Within(Eps));
        }

        [TestCase(DoseLevel.Under)]
        [TestCase(DoseLevel.Over)]
        [TestCase(null)]
        public void Practice_WrongChoices_NoSideEffects_StayInPhase_Dose(DoseLevel? level)
        {
            var h = ToPhase(HypoPhase.Dose);
            if (level.HasValue) h.Fsm.ChooseDose(level.Value);
            else h.Fsm.PressHelp(HypoPhase.Dose);
            string choice = level.HasValue ? level.Value.ToString().ToLowerInvariant() : "help";
            AssertStayed(h, HypoPhase.Dose, HypoStep.Dose, choice);
            Assert.That(h.All<DoseApplied>(), Is.Empty);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Practice_WrongChoices_NoSideEffects_StayInPhase_Remeasure(bool help)
        {
            var h = ToPhase(HypoPhase.Remeasure);
            int doses = h.All<DoseApplied>().Count;
            if (help) h.Fsm.PressHelp(HypoPhase.Remeasure);
            else h.Fsm.ChooseRunAgain(HypoPhase.Remeasure);
            AssertStayed(h, HypoPhase.Remeasure, HypoStep.Remeasure, help ? "help" : "run_again");
            Assert.That(h.All<DoseApplied>(), Has.Count.EqualTo(doses));
        }

        [Test]
        public void Practice_WrongChoices_NoSideEffects_StayInPhase_Notify()
        {
            var h = ToPhase(HypoPhase.Remeasure);
            h.Fsm.ChooseRecheck();
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Notify));
            h.Fsm.ChooseRunAgain(HypoPhase.Notify);
            AssertStayed(h, HypoPhase.Notify, HypoStep.Notify, "run_again");
        }

        [Test]
        public void Practice_WrongChoices_NoSideEffects_StayInPhase_HyperNotify()
        {
            var h = HyperT2();
            var control = HyperT2();
            control.Fsm.SetPractice(false);
            Assert.That(h.Fsm.ChooseJuice(), Is.True);
            // 같은 순간의 두 번째 오답은 두 번 누름으로 보고 거르므로 판단 시간을 흘린 뒤 고른다.
            h.Frame(0.1); control.Frame(0.1);
            Assert.That(h.Fsm.ChooseKeepRunning(), Is.True);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.HyperNotify));
            Assert.That(h.All<PracticeRetried>(), Has.Count.EqualTo(2));
            Assert.That(h.Fsm.PracticeRetryCount, Is.EqualTo(2));
            Assert.That(h.All<StepAnswered>(), Is.Empty);
            Assert.That(h.All<DoseApplied>(), Is.Empty);
            Assert.That(h.Fsm.DoseSnackId, Is.Null);
            Assert.That(h.All<GuardianPromptStarted>(), Is.Empty);
            h.Frames(5); control.Frames(5);
            Assert.That(h.Model.Actual, Is.EqualTo(control.Model.Actual).Within(Eps));
        }

        [Test]
        public void Practice_WrongChoices_NoSideEffects_StayInPhase_LowNotify()
        {
            var h = LowT2();
            Assert.That(h.Fsm.ChooseKeepRunning(), Is.True);
            AssertStayed(h, HypoPhase.LowNotify, HypoStep.LowNotify, "keep_running");
            Assert.That(h.All<DoseApplied>(), Is.Empty);
        }

        [Test]
        public void Practice_NotifyPhase_SameInstantSecondWrongChoiceIgnored()
        {
            var hy = HyperT2();
            Assert.That(hy.Fsm.ChooseJuice(), Is.True);
            Assert.That(hy.Fsm.ChooseKeepRunning(), Is.False, "같은 순간의 두 번째 오답은 무시한다");
            Assert.That(hy.Fsm.PracticeRetryCount, Is.EqualTo(1));
            Assert.That(hy.All<PracticeRetried>(), Has.Count.EqualTo(1));
            hy.Frame(0.1);
            Assert.That(hy.Fsm.ChooseKeepRunning(), Is.True, "시간이 흐른 뒤의 오답은 센다");
            Assert.That(hy.Fsm.PracticeRetryCount, Is.EqualTo(2));
            Assert.That(hy.Fsm.Phase, Is.EqualTo(HypoPhase.HyperNotify));

            var lo = LowT2();
            Assert.That(lo.Fsm.ChooseKeepRunning(), Is.True);
            Assert.That(lo.Fsm.ChooseKeepRunning(), Is.False);
            Assert.That(lo.Fsm.PracticeRetryCount, Is.EqualTo(1));
            Assert.That(lo.All<PracticeRetried>(), Has.Count.EqualTo(1));
            lo.Frame(0.1);
            Assert.That(lo.Fsm.ChooseKeepRunning(), Is.True);
            Assert.That(lo.Fsm.PracticeRetryCount, Is.EqualTo(2));
            Assert.That(lo.Fsm.Phase, Is.EqualTo(HypoPhase.LowNotify));
        }

        [Test]
        public void Practice_NoTimeouts()
        {
            foreach (var target in new[] { HypoPhase.Recognize, HypoPhase.Respond, HypoPhase.Dose, HypoPhase.Remeasure })
            {
                var h = target == HypoPhase.Recognize ? Hypo() : ToPhase(target);
                h.Model.Tick(0, 0, 0, 1);
                h.Frames(10, 5);
                Assert.That(h.Fsm.Phase, Is.EqualTo(target));
                Assert.That(h.All<RunEliminated>(), Is.Empty);
            }
            var n = ToPhase(HypoPhase.Remeasure);
            n.Fsm.ChooseRecheck();
            n.Frames(10, 5);
            Assert.That(n.Fsm.Phase, Is.EqualTo(HypoPhase.Notify));
            Assert.That(n.All<RunEliminated>(), Is.Empty);

            var hy = HyperT2();
            hy.Frames(10, 5);
            Assert.That(hy.Fsm.Phase, Is.EqualTo(HypoPhase.HyperNotify));
            Assert.That(hy.All<RunEliminated>(), Is.Empty);

            var lo = LowT2();
            lo.Frames(10, 5);
            Assert.That(lo.Fsm.Phase, Is.EqualTo(HypoPhase.LowNotify));
            Assert.That(lo.All<RunEliminated>(), Is.Empty);
        }

        [Test]
        public void Practice_NoTimeouts_EvenWhenGlucoseBelowEliminationLow()
        {
            var h = Hypo(b => b.GlucoseLow = 69);
            Assert.That(h.Model.Actual, Is.LessThan(69));
            h.Frames(10, 5);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Recognize));
            h.Fsm.PressMeter();
            h.Frames(10, 5);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Respond));
            Assert.That(h.All<RunEliminated>(), Is.Empty);
        }

        [Test]
        public void Practice_CorrectPathCompletes()
        {
            var h = Hypo();
            h.Fsm.PressMeter();
            h.Fsm.ChooseSnack("juice");
            h.Fsm.ChooseDose(DoseLevel.Proper);
            h.Fsm.ChooseRecheck();
            h.Fsm.PressHelp(HypoPhase.Notify);

            Assert.That(h.All<HypoResolved>().Single().AllCorrect, Is.True);
            Assert.That(h.All<PracticeRetried>(), Is.Empty);
            Assert.That(h.All<DoseApplied>(), Has.Count.EqualTo(1));
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.ResumeCountdown));
            h.Frames(1);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Grace));
            h.Frames(3);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Idle));
            Assert.That(h.All<GraceEnded>(), Has.Count.EqualTo(1));
        }

        [Test]
        public void Practice_RetryReopensFeedbackWindow()
        {
            var h = ToPhase(HypoPhase.Respond, b => b.AnswerFeedbackSec = 2);
            h.Frames(3);
            Assert.That(h.Fsm.IsShowingAnswerFeedback, Is.False);

            h.Fsm.ChooseNoSugar();

            Assert.That(h.Fsm.IsShowingAnswerFeedback, Is.True);
            h.Frames(2);
            Assert.That(h.Fsm.IsShowingAnswerFeedback, Is.False);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Respond));
        }
    }
}
