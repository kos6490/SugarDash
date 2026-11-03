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
    /// <summary>2형 tell: 저혈당 LowNotify, 고혈당 "주스 마시기"(설계 4.4~4.5절).</summary>
    public class Type2TellFsmTests
    {
        private sealed class NoRandom : IRandomSource
        {
            public double NextDouble() => throw new InvalidOperationException("2형은 난수를 읽지 않는다");
        }

        private sealed class T2Harness
        {
            public readonly GlucoseModel Model;
            public readonly EventDirector Director;
            public readonly HypoEventFsm Fsm;
            public readonly List<object> Events = new List<object>();

            public T2Harness(Action<ConfigBuilder> configure)
            {
                var builder = new ConfigBuilder();
                configure(builder);
                var config = builder.BuildType2();
                Assert.That(GameConfigValidator.Validate(config), Is.Empty);
                var bus = new DomainEventBus();
                bus.Subscribe<StepAnswered>(e => Events.Add(e));
                bus.Subscribe<GuardianPromptStarted>(e => Events.Add(e));
                bus.Subscribe<GuardianNotified>(e => Events.Add(e));
                bus.Subscribe<DoseApplied>(e => Events.Add(e));
                bus.Subscribe<RunEliminated>(e => Events.Add(e));
                bus.Subscribe<LowResolved>(e => Events.Add(e));
                bus.Subscribe<HyperResolved>(e => Events.Add(e));
                Model = new GlucoseModel(config.Glucose);
                Director = new EventDirector(config.Glucose, Model, new NoRandom(), bus, config.Type2);
                Fsm = new HypoEventFsm(config, Model, Director, bus, () => 0);
                bus.Subscribe<LowEventTriggered>(Fsm.BeginLow);
                bus.Subscribe<HyperEventTriggered>(Fsm.BeginHyper);
            }

            public IReadOnlyList<T> All<T>() => Events.OfType<T>().ToList();
            public T Last<T>() => Events.OfType<T>().Last();

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
        }

        // 달리기 감소 20/s로 120에서 내려가 저혈당 hold 2초 + notice 1초 뒤 tell. 이때 혈당은 20(Min 5, GlucoseLow 10이면 범위 안,
        // 기본 GlucoseLow 40이면 범위 밖).
        private static T2Harness LowTell(bool glucoseInRange = true)
        {
            var h = new T2Harness(b =>
            {
                b.RunDecayPerSec = 20;
                if (glucoseInRange)
                {
                    b.Min = 5;
                    b.GlucoseLow = 10;
                }
            });
            for (int i = 0; i < 20 && h.Fsm.Phase == HypoPhase.Idle; i++) h.Frame(1);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.LowNotify));
            Assert.That(h.Model.Actual, Is.EqualTo(20).Within(1e-9));
            return h;
        }

        // 주스 5인분(+150)으로 250 위로 올라가 hold 2초 + notice 1초 뒤 tell.
        private static T2Harness HighTell()
        {
            var h = new T2Harness(b => { });
            h.Model.EatSnack("juice", 5);
            for (int i = 0; i < 20 && h.Fsm.Phase == HypoPhase.Idle; i++) h.Frame(1);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.HyperNotify));
            return h;
        }

        [Test]
        public void LowNotify_FreezesWorld()
        {
            var h = LowTell();
            Assert.That(h.Fsm.IsWorldFrozen, Is.True);
            Assert.That(h.Fsm.IsShowingAnswerFeedback, Is.False);
        }

        [Test]
        public void BeginLow_WhenNotIdle_Throws()
        {
            var h = LowTell();
            Assert.Throws<InvalidOperationException>(() => h.Fsm.BeginLow(new LowEventTriggered(9, 0, 0, 0)));
        }

        [Test]
        public void LowTell_Help_IsCorrect_GuardianSnack_ThenCountdown()
        {
            var h = LowTell();
            double before = h.Model.Actual;

            Assert.That(h.Fsm.PressHelp(HypoPhase.LowNotify), Is.True);

            var answer = h.Last<StepAnswered>();
            Assert.That(answer.Step, Is.EqualTo(HypoStep.LowNotify));
            Assert.That(answer.ChoiceId, Is.EqualTo("help"));
            Assert.That(answer.IsCorrect, Is.True);
            var dose = h.Last<DoseApplied>();
            Assert.That(dose.SnackId, Is.EqualTo("juice"));
            Assert.That(dose.Servings, Is.EqualTo(1));
            Assert.That(dose.ChosenByChild, Is.False);
            Assert.That(h.Last<LowResolved>().AllCorrect, Is.True);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.ResumeCountdown));
            Assert.That(h.Director.ActiveSignal, Is.Null);
            Assert.That(h.Model.Actual, Is.EqualTo(before), "먹은 직후라 아직 흡수 전이다");
        }

        [Test]
        public void LowTell_KeepRunning_ErrorThenPrompt()
        {
            var h = LowTell();

            Assert.That(h.Fsm.ChooseKeepRunning(), Is.True);

            var answer = h.Last<StepAnswered>();
            Assert.That(answer.IsCorrect, Is.False);
            Assert.That(answer.ErrorCode, Is.EqualTo(HypoErrorCode.LoKeepRunning));
            Assert.That(h.Last<GuardianPromptStarted>().Step, Is.EqualTo(HypoStep.LowNotify));
            Assert.That(h.All<DoseApplied>(), Is.Empty);

            Assert.That(h.Fsm.PressHelp(HypoPhase.GuardianPrompt), Is.True);

            Assert.That(h.Last<DoseApplied>().ChosenByChild, Is.False);
            Assert.That(h.Last<LowResolved>().AllCorrect, Is.False);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.ResumeCountdown));
        }

        [Test]
        public void LowTell_Timeout_InRange_GoesToPrompt()
        {
            var h = LowTell();
            h.Frames(5);

            var answer = h.Last<StepAnswered>();
            Assert.That(answer.ErrorCode, Is.EqualTo(HypoErrorCode.LoTimeout));
            Assert.That(answer.ChoiceId, Is.Null);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.GuardianPrompt));
        }

        [Test]
        public void LowTell_Timeout_BelowGlucoseLow_Eliminates()
        {
            var h = LowTell(false);
            h.Frames(5);

            var e = h.Last<RunEliminated>();
            Assert.That(e.Cause, Is.EqualTo(EliminationCause.GlucoseLow));
            Assert.That(e.Step, Is.EqualTo(HypoStep.LowNotify));
            Assert.That(e.Reason, Is.EqualTo(HypoErrorCode.LoTimeout));
            Assert.That(h.All<DoseApplied>(), Is.Empty);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Eliminated));
        }

        [Test]
        public void LowTell_GuardianSnack_OnCorrectAndAfterPrompt_NotOnElimination()
        {
            var correct = LowTell();
            correct.Fsm.PressHelp(HypoPhase.LowNotify);
            Assert.That(correct.All<DoseApplied>(), Has.Count.EqualTo(1));

            var afterPrompt = LowTell();
            afterPrompt.Fsm.ChooseKeepRunning();
            afterPrompt.Fsm.PressHelp(HypoPhase.GuardianPrompt);
            Assert.That(afterPrompt.All<DoseApplied>(), Has.Count.EqualTo(1));

            var declined = LowTell();
            declined.Fsm.ChooseKeepRunning();
            declined.Fsm.ChooseRunAgain(HypoPhase.GuardianPrompt);
            Assert.That(declined.All<DoseApplied>(), Is.Empty);

            var promptTimeout = LowTell();
            promptTimeout.Fsm.ChooseKeepRunning();
            promptTimeout.Frames(5);
            Assert.That(promptTimeout.Fsm.Phase, Is.EqualTo(HypoPhase.Eliminated));
            Assert.That(promptTimeout.All<DoseApplied>(), Is.Empty);

            var belowLow = LowTell(false);
            belowLow.Frames(5);
            Assert.That(belowLow.Fsm.Phase, Is.EqualTo(HypoPhase.Eliminated));
            Assert.That(belowLow.All<RunEliminated>(), Has.Count.EqualTo(1));
            Assert.That(belowLow.All<DoseApplied>(), Is.Empty);
        }

        [Test]
        public void HyperTell_Juice_AppliesJuiceThenError()
        {
            var h = HighTell();

            Assert.That(h.Fsm.ChooseJuice(), Is.True);

            var dose = h.Last<DoseApplied>();
            Assert.That(dose.SnackId, Is.EqualTo("juice"));
            Assert.That(dose.Servings, Is.EqualTo(1));
            Assert.That(dose.ChosenByChild, Is.True);
            var answer = h.Last<StepAnswered>();
            Assert.That(answer.Step, Is.EqualTo(HypoStep.HyperNotify));
            Assert.That(answer.ChoiceId, Is.EqualTo("juice"));
            Assert.That(answer.ErrorCode, Is.EqualTo(HypoErrorCode.HyJuice));
            Assert.That(h.Last<GuardianPromptStarted>().ErrorCode, Is.EqualTo(HypoErrorCode.HyJuice));
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.GuardianPrompt));

            h.Fsm.PressHelp(HypoPhase.GuardianPrompt);
            Assert.That(h.Last<HyperResolved>().AllCorrect, Is.False);
        }

        [Test]
        public void ChooseJuice_Type1_ReturnsFalse()
        {
            var h = new HypoHarness();
            h.Model.EatSnack("juice", 5);
            h.Frames(3);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.HyperNotify));
            int before = h.Events.Count;

            Assert.That(h.Fsm.ChooseJuice(), Is.False);

            Assert.That(h.Events.Count, Is.EqualTo(before));
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.HyperNotify));
        }
    }
}
