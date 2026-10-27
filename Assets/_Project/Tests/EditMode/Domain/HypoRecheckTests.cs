using System;
using System.Linq;
using SugarDash.Domain.Events;
using SugarDash.Domain.Hypo;
using NUnit.Framework;

namespace SugarDash.Domain.Tests
{
    public class HypoRecheckTests
    {
        private const double Eps = 1e-9;

        /// <summary>과소 처치 시나리오 설정. 처치 후 해제 수준 80에 못 닿고, 유예 중 달리기로 70 아래로 내려간다.</summary>
        private static void UnderDoseConfig(ConfigBuilder b)
        {
            b.DoseUnder = 0.2;
            b.BaseDecayPerSec = 0.1;
        }

        private static HypoHarness ReachRemeasure(Action<ConfigBuilder> configure = null,
            DoseLevel dose = DoseLevel.Proper, string snack = "juice")
        {
            var h = new HypoHarness(configure);
            h.RunUntilTriggered();
            h.Fsm.PressMeter();
            h.Fsm.ChooseSnack(snack);
            if (h.Fsm.Phase == HypoPhase.GuardianPrompt) h.Fsm.PressHelp();
            h.Fsm.ChooseDose(dose);
            if (h.Fsm.Phase == HypoPhase.GuardianPrompt) h.Fsm.PressHelp();
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Remeasure));
            return h;
        }

        private static HypoHarness Resolved(Action<ConfigBuilder> configure = null, DoseLevel dose = DoseLevel.Proper)
        {
            var h = ReachRemeasure(configure, dose);
            h.Fsm.ChooseRecheck();
            h.Fsm.PressHelp();
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.ResumeCountdown));
            return h;
        }

        private static StepAnswered Answer(HypoHarness h, HypoStep step) =>
            h.All<StepAnswered>().Single(a => a.Step == step);

        [Test]
        public void ProperTreatment_FastForward_ActualAndDisplayedAboveNormalRange()
        {
            var h = ReachRemeasure();
            double sim = h.Model.SimTime;
            double decision = h.Model.DecisionTime;

            Assert.That(h.Fsm.ChooseRecheck(), Is.True);

            var ff = h.Last<RecheckFastForward>();
            Assert.That(ff.SimSecSkipped, Is.EqualTo(6).Within(Eps));
            Assert.That(ff.GlucoseBefore, Is.EqualTo(68).Within(Eps));
            Assert.That(ff.GlucoseAfter, Is.EqualTo(h.Model.Actual).Within(Eps));
            Assert.That(h.Model.Actual, Is.GreaterThanOrEqualTo(80));
            Assert.That(h.Model.Displayed, Is.GreaterThanOrEqualTo(80));
            Assert.That(h.Model.SimTime, Is.EqualTo(sim + 6).Within(Eps));
            Assert.That(h.Model.DecisionTime, Is.EqualTo(decision).Within(Eps), "실제로 기다리지 않는다");
            Assert.That(Answer(h, HypoStep.Remeasure).IsCorrect, Is.True);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Notify));
        }

        [Test]
        public void FastForward_ResultIndependentOfTickSize()
        {
            var coarse = ReachRemeasure(b => b.FastForwardTickSimSec = 0.5);
            var fine = ReachRemeasure(b => b.FastForwardTickSimSec = 0.1);

            coarse.Fsm.ChooseRecheck();
            fine.Fsm.ChooseRecheck();

            Assert.That(fine.Model.Actual, Is.EqualTo(coarse.Model.Actual).Within(Eps));
            Assert.That(fine.Model.Displayed, Is.EqualTo(coarse.Model.Displayed).Within(Eps));
        }

        [Test]
        public void UnderDose_RecheckIsLow_ThenRecurrenceAfterCooldownAndGrace()
        {
            var h = Resolved(UnderDoseConfig, DoseLevel.Under);

            Assert.That(h.Last<RecheckFastForward>().GlucoseAfter, Is.LessThan(80));
            Assert.That(Answer(h, HypoStep.Remeasure).IsCorrect, Is.True, "재측정 값과 무관하게 정답");
            Assert.That(h.Last<HypoResolved>().AllCorrect, Is.False);

            h.Frames(15);

            var triggers = h.All<HypoEventTriggered>();
            Assert.That(triggers, Has.Count.EqualTo(2));
            Assert.That(triggers[1].IsRecurrence, Is.True);
            Assert.That(triggers[1].EventId, Is.EqualTo(2));
        }

        [Test]
        public void SlowSnack_RecheckIsLower()
        {
            var fast = ReachRemeasure();
            var slow = ReachRemeasure(snack: "cracker");

            fast.Fsm.ChooseRecheck();
            slow.Fsm.ChooseRecheck();

            Assert.That(slow.Last<RecheckFastForward>().GlucoseAfter,
                Is.LessThan(fast.Last<RecheckFastForward>().GlucoseAfter));
        }

        [Test]
        public void Remeasure_RunAgain_IsRcNoRecheck_ThenAdultRemeasures()
        {
            var h = ReachRemeasure();

            Assert.That(h.Fsm.ChooseRunAgain(), Is.True);
            Assert.That(Answer(h, HypoStep.Remeasure).ErrorCode, Is.EqualTo(HypoErrorCode.RcNoRecheck));
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.GuardianPrompt));
            Assert.That(h.All<RecheckFastForward>(), Is.Empty);

            h.Fsm.PressHelp();

            Assert.That(h.All<RecheckFastForward>(), Has.Count.EqualTo(1));
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Notify));
        }

        [Test]
        public void Remeasure_Timeout_IsRcNoRecheck()
        {
            var h = ReachRemeasure();

            h.Frames(5);

            Assert.That(Answer(h, HypoStep.Remeasure).ErrorCode, Is.EqualTo(HypoErrorCode.RcNoRecheck));
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.GuardianPrompt));
        }

        [Test]
        public void Remeasure_HelpButton_IsRcNoRecheckWithoutPrompt()
        {
            var h = ReachRemeasure();
            int prompts = h.All<GuardianPromptStarted>().Count;

            Assert.That(h.Fsm.PressHelp(), Is.True);

            var answer = Answer(h, HypoStep.Remeasure);
            Assert.That(answer.ErrorCode, Is.EqualTo(HypoErrorCode.RcNoRecheck));
            Assert.That(answer.HelpInstead, Is.True);
            Assert.That(h.All<GuardianPromptStarted>(), Has.Count.EqualTo(prompts));
            Assert.That(h.All<RecheckFastForward>(), Has.Count.EqualTo(1));
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Notify));
        }

        [Test]
        public void Notify_HelpButton_IsCorrect_AndResolves()
        {
            var h = ReachRemeasure();
            h.Fsm.ChooseRecheck();

            h.Fsm.PressHelp();

            Assert.That(Answer(h, HypoStep.Notify).IsCorrect, Is.True);
            Assert.That(h.Last<HypoResolved>().EventId, Is.EqualTo(1));
            Assert.That(h.Director.IsHypoEventActive, Is.False);
        }

        [Test]
        public void Notify_RunAgain_IsRcNoNotify()
        {
            var h = ReachRemeasure();
            h.Fsm.ChooseRecheck();

            h.Fsm.ChooseRunAgain();
            Assert.That(Answer(h, HypoStep.Notify).ErrorCode, Is.EqualTo(HypoErrorCode.RcNoNotify));
            Assert.That(h.All<HypoResolved>(), Is.Empty);
            h.Fsm.PressHelp();

            Assert.That(h.All<HypoResolved>(), Has.Count.EqualTo(1));
        }

        [Test]
        public void Notify_Timeout_IsRcNoNotify()
        {
            var h = ReachRemeasure();
            h.Fsm.ChooseRecheck();

            h.Frames(5);

            Assert.That(Answer(h, HypoStep.Notify).ErrorCode, Is.EqualTo(HypoErrorCode.RcNoNotify));
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.GuardianPrompt));
        }

        [Test]
        public void AllCorrect_TrueOnlyWithoutAnyError()
        {
            Assert.That(Resolved().Last<HypoResolved>().AllCorrect, Is.True);

            var h = new HypoHarness();
            h.RunUntilTriggered();
            h.Fsm.PressMeter();
            h.Fsm.ChooseSnack("juice");
            h.Fsm.PressHelp(); // Dose HELP_INSTEAD
            h.Fsm.ChooseRecheck();
            h.Fsm.PressHelp();

            Assert.That(h.Last<HypoResolved>().AllCorrect, Is.False);
        }

        [Test]
        public void Resume_CountdownThenGrace_ThenIdle()
        {
            var h = Resolved();
            Assert.That(h.Fsm.IsWorldFrozen, Is.True);

            h.Frame(1);
            Assert.That(h.All<GraceStarted>(), Has.Count.EqualTo(1));
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Grace));
            Assert.That(h.Fsm.IsWorldFrozen, Is.False);
            Assert.That(h.Fsm.IsInGrace, Is.True);

            h.Frames(2);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Grace));
            h.Frame(1);

            Assert.That(h.Last<GraceEnded>().EventId, Is.EqualTo(1));
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Idle));
            Assert.That(h.Fsm.EventId, Is.EqualTo(0));
        }

        [Test]
        public void DuringGrace_NoTrigger_AndMeterIsInGrace()
        {
            // cooldown을 재개 1 + 유예 3보다 짧게 둬서 cooldown이 아니라 유예가 발동을 막는지 본다.
            var h = Resolved(b => { UnderDoseConfig(b); b.CooldownSec = 1; }, DoseLevel.Under);
            h.Frames(3); // 카운트다운 1 + 유예 2
            Assert.That(h.Fsm.IsInGrace, Is.True);

            h.Fsm.PressMeter();
            var meter = h.Last<MeterPressed>();
            Assert.That(meter.InGrace, Is.True);
            Assert.That(meter.EventId, Is.EqualTo(0));

            h.Frame(1); // 유예 끝
            Assert.That(h.Model.Actual, Is.LessThan(70));
            Assert.That(h.All<HypoEventTriggered>(), Has.Count.EqualTo(1));

            h.Frame(1);
            Assert.That(h.All<HypoEventTriggered>(), Has.Count.EqualTo(2));
        }

        [Test]
        public void MeterOutsideEvent_AboveThreshold_IsUnnecessary()
        {
            var h = new HypoHarness();

            h.Fsm.PressMeter();

            var meter = h.Last<MeterPressed>();
            Assert.That(meter.Unnecessary, Is.True);
            Assert.That(meter.InGrace, Is.False);
            Assert.That(meter.EventId, Is.EqualTo(0));
            Assert.That(meter.Measured, Is.EqualTo(120));
        }

        [Test]
        public void MeterOutsideEvent_BelowThreshold_IsNotUnnecessary()
        {
            var h = Resolved(UnderDoseConfig, DoseLevel.Under);
            h.Frames(4); // 유예 끝, cooldown 중
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Idle));
            Assert.That(h.Model.Actual, Is.LessThan(70));

            h.Fsm.PressMeter();

            Assert.That(h.Last<MeterPressed>().Unnecessary, Is.False);
        }

        [Test]
        public void Overdose_ClampsAtMax_AndResolves()
        {
            var h = Resolved(b => b.DoseOver = 12, DoseLevel.Over); // 68 + 360 > 400

            Assert.That(h.Model.Actual, Is.EqualTo(400));
            Assert.That(h.All<HypoResolved>(), Has.Count.EqualTo(1));
        }

        [Test]
        public void Pause_ChangesNothing()
        {
            void AssertPaused(HypoHarness h)
            {
                var phase = h.Fsm.Phase;
                double actual = h.Model.Actual, sim = h.Model.SimTime, decision = h.Model.DecisionTime;

                h.Pause(100);

                Assert.That(h.Fsm.Phase, Is.EqualTo(phase));
                Assert.That(h.Model.Actual, Is.EqualTo(actual));
                Assert.That(h.Model.SimTime, Is.EqualTo(sim));
                Assert.That(h.Model.DecisionTime, Is.EqualTo(decision));
            }

            var recognize = new HypoHarness();
            recognize.RunUntilTriggered();
            AssertPaused(recognize);

            var respond = new HypoHarness();
            respond.RunUntilTriggered();
            respond.Fsm.PressMeter();
            AssertPaused(respond);

            AssertPaused(ReachRemeasure());

            var grace = Resolved();
            grace.Frame(1);
            Assert.That(grace.Fsm.IsInGrace, Is.True);
            AssertPaused(grace);
        }
    }
}
