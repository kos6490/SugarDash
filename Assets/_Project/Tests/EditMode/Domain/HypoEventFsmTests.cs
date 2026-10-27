using System;
using System.Linq;
using SugarDash.Domain.Events;
using SugarDash.Domain.Hypo;
using NUnit.Framework;

namespace SugarDash.Domain.Tests
{
    public class HypoEventFsmTests
    {
        private const double Eps = 1e-9;

        private static HypoHarness Triggered(Action<ConfigBuilder> configure = null)
        {
            var h = new HypoHarness(configure);
            h.RunUntilTriggered();
            return h;
        }

        private static HypoHarness AtRespond(Action<ConfigBuilder> configure = null)
        {
            var h = Triggered(configure);
            h.Fsm.PressMeter();
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Respond));
            return h;
        }

        private static HypoHarness AtDose(Action<ConfigBuilder> configure = null)
        {
            var h = AtRespond(configure);
            h.Fsm.ChooseSnack("juice");
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Dose));
            return h;
        }

        private static StepAnswered Answer(HypoHarness h, HypoStep step) =>
            h.All<StepAnswered>().Single(a => a.Step == step);

        [Test]
        public void Trigger_EntersRecognize_WorldKeepsRunning()
        {
            var h = Triggered();

            Assert.That(h.Fsm.IsWorldFrozen, Is.False);
            Assert.That(h.Fsm.EventId, Is.EqualTo(1));
            Assert.That(h.Model.Actual, Is.EqualTo(68).Within(Eps));
        }

        [Test]
        public void Recognize_ActualKeepsFallingWithRunDecay()
        {
            var h = Triggered();

            h.Frame(1);

            Assert.That(h.Model.Actual, Is.EqualTo(66).Within(Eps));
        }

        [Test]
        public void MeterBeforeWarning_IsCorrectAndMeasuredFirst()
        {
            var h = Triggered(b => b.WarningThreshold = 70); // 발동 시 표시 = actual(23) = 74

            h.Fsm.PressMeter();

            var answer = Answer(h, HypoStep.Recognize);
            Assert.That(answer.IsCorrect, Is.True);
            Assert.That(answer.MeasuredBeforeWarning, Is.True);
            Assert.That(answer.ChoiceId, Is.EqualTo("meter"));
            Assert.That(h.Last<MeterPressed>().Measured, Is.EqualTo(68).Within(Eps));
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Respond));
            Assert.That(h.Fsm.IsWorldFrozen, Is.True);
        }

        [Test]
        public void MeterAfterWarning_IsCorrectButNotMeasuredFirst()
        {
            var h = Triggered(b => b.WarningThreshold = 70);
            h.Frames(3); // 표시 = actual(26) = 68

            h.Fsm.PressMeter();

            var answer = Answer(h, HypoStep.Recognize);
            Assert.That(answer.IsCorrect, Is.True);
            Assert.That(answer.MeasuredBeforeWarning, Is.False);
            Assert.That(answer.DecisionSec, Is.EqualTo(3).Within(Eps));
        }

        [Test]
        public void Recognize_Timeout_IsSMissed_ThenGuardianPrompt_WorldFrozen()
        {
            var h = Triggered();

            h.Frames(4);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Recognize));
            h.Frames(1);

            var answer = Answer(h, HypoStep.Recognize);
            Assert.That(answer.IsCorrect, Is.False);
            Assert.That(answer.ErrorCode, Is.EqualTo(HypoErrorCode.SMissed));
            Assert.That(answer.ChoiceId, Is.Null);
            Assert.That(h.Last<GuardianPromptStarted>().ErrorCode, Is.EqualTo(HypoErrorCode.SMissed));
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.GuardianPrompt));
            Assert.That(h.Fsm.IsWorldFrozen, Is.True);
        }

        [Test]
        public void SMissed_Notify_ProceedsToRespond()
        {
            var h = Triggered();
            h.Frames(5);

            Assert.That(h.Fsm.PressHelp(), Is.True);

            Assert.That(h.Last<GuardianNotified>().Step, Is.EqualTo(HypoStep.Recognize));
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Respond));
        }

        [TestCase("juice", true, HypoErrorCode.None)]
        [TestCase("cracker", false, HypoErrorCode.RsSlow)]
        public void Respond_Snack_SetsDoseSnackToChosenSnack(string id, bool correct, HypoErrorCode code)
        {
            var h = AtRespond();

            h.Fsm.ChooseSnack(id);
            if (!correct)
            {
                Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.GuardianPrompt));
                h.Fsm.PressHelp();
            }

            var answer = Answer(h, HypoStep.Respond);
            Assert.That(answer.IsCorrect, Is.EqualTo(correct));
            Assert.That(answer.ErrorCode, Is.EqualTo(code));
            Assert.That(answer.ChoiceId, Is.EqualTo(id));
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Dose));
            Assert.That(h.Fsm.DoseSnackId, Is.EqualTo(id));
        }

        [Test]
        public void Respond_KeepRunning_UsesGuardianSnack()
        {
            var h = AtRespond();

            h.Fsm.ChooseKeepRunning();
            h.Fsm.PressHelp();

            var answer = Answer(h, HypoStep.Respond);
            Assert.That(answer.ErrorCode, Is.EqualTo(HypoErrorCode.RsKeepRunning));
            Assert.That(answer.ChoiceId, Is.EqualTo("keep_running"));
            Assert.That(h.Fsm.DoseSnackId, Is.EqualTo("juice"));
            Assert.That(h.Fsm.IsWorldFrozen, Is.True, "계속 달리기는 그림 선택지다");
        }

        [Test]
        public void Respond_NoSugar_UsesGuardianSnack()
        {
            var h = AtRespond();

            h.Fsm.ChooseNoSugar();
            h.Fsm.PressHelp();

            Assert.That(Answer(h, HypoStep.Respond).ErrorCode, Is.EqualTo(HypoErrorCode.RsNone));
            Assert.That(h.Fsm.DoseSnackId, Is.EqualTo("juice"));
        }

        [Test]
        public void Respond_Timeout_IsRsTimeout_UsesGuardianSnack()
        {
            var h = AtRespond();

            h.Frames(5);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.GuardianPrompt));
            h.Fsm.PressHelp();

            Assert.That(Answer(h, HypoStep.Respond).ErrorCode, Is.EqualTo(HypoErrorCode.RsTimeout));
            Assert.That(h.Fsm.DoseSnackId, Is.EqualTo("juice"));
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Dose));
        }

        [Test]
        public void Respond_HelpInstead_SkipsPrompt_UsesGuardianSnack()
        {
            var h = AtRespond();

            Assert.That(h.Fsm.PressHelp(), Is.True);

            var answer = Answer(h, HypoStep.Respond);
            Assert.That(answer.IsCorrect, Is.False);
            Assert.That(answer.ErrorCode, Is.EqualTo(HypoErrorCode.HelpInstead));
            Assert.That(answer.HelpInstead, Is.True);
            Assert.That(h.All<GuardianPromptStarted>(), Is.Empty);
            Assert.That(h.Last<GuardianNotified>().Step, Is.EqualTo(HypoStep.Respond));
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Dose));
            Assert.That(h.Fsm.DoseSnackId, Is.EqualTo("juice"));
        }

        [Test]
        public void WorldFrozen_SimAndAbsorptionContinue()
        {
            var h = AtRespond();
            double before = h.Model.Actual;
            double sim = h.Model.SimTime;

            h.Frames(2);
            Assert.That(h.Model.Actual, Is.EqualTo(before).Within(Eps), "세계 정지 중 달리기 감소 없음(기본 하강 0)");
            Assert.That(h.Model.SimTime, Is.EqualTo(sim + 2).Within(Eps));

            h.Fsm.ChooseSnack("juice");
            h.Fsm.ChooseDose(DoseLevel.Proper);
            h.Frames(3); // juice onset 1, absorb 2, proper 2 → +60

            Assert.That(h.Model.Actual, Is.EqualTo(before + 60).Within(Eps));
        }

        [Test]
        public void ProperDose_AppliesImmediately_ChosenByChild()
        {
            var h = AtDose();

            h.Fsm.ChooseDose(DoseLevel.Proper);

            var dose = h.Last<DoseApplied>();
            Assert.That(dose.SnackId, Is.EqualTo("juice"));
            Assert.That(dose.Servings, Is.EqualTo(2));
            Assert.That(dose.ChosenByChild, Is.True);
            Assert.That(Answer(h, HypoStep.Dose).IsCorrect, Is.True);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Remeasure));
        }

        [TestCase(DoseLevel.Under, HypoErrorCode.DsUnder, 1)]
        [TestCase(DoseLevel.Over, HypoErrorCode.DsOver, 3)]
        public void WrongDose_AfterNotify_AppliesChosenServings(DoseLevel level, HypoErrorCode code, double servings)
        {
            var h = AtDose();

            h.Fsm.ChooseDose(level);
            Assert.That(h.All<DoseApplied>(), Is.Empty, "알리기 전에는 투입하지 않는다");
            h.Fsm.PressHelp();

            var dose = h.Last<DoseApplied>();
            Assert.That(dose.Servings, Is.EqualTo(servings));
            Assert.That(dose.ChosenByChild, Is.True);
            Assert.That(Answer(h, HypoStep.Dose).ErrorCode, Is.EqualTo(code));
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Remeasure));
        }

        [Test]
        public void SlowSnack_DoseUsesSlowSnack()
        {
            var h = AtRespond();
            h.Fsm.ChooseSnack("cracker");
            h.Fsm.PressHelp();

            h.Fsm.ChooseDose(DoseLevel.Proper);

            Assert.That(h.Last<DoseApplied>().SnackId, Is.EqualTo("cracker"));
        }

        [Test]
        public void Dose_Timeout_GuardianAppliesProperAfterNotify()
        {
            var h = AtDose();

            h.Frames(5);
            Assert.That(Answer(h, HypoStep.Dose).ErrorCode, Is.EqualTo(HypoErrorCode.DsTimeout));
            Assert.That(h.All<DoseApplied>(), Is.Empty);
            h.Fsm.PressHelp();

            var dose = h.Last<DoseApplied>();
            Assert.That(dose.Servings, Is.EqualTo(2));
            Assert.That(dose.ChosenByChild, Is.False);
        }

        [Test]
        public void Dose_HelpInstead_GuardianAppliesProperImmediately()
        {
            var h = AtDose();

            h.Fsm.PressHelp();

            Assert.That(Answer(h, HypoStep.Dose).ErrorCode, Is.EqualTo(HypoErrorCode.HelpInstead));
            var dose = h.Last<DoseApplied>();
            Assert.That(dose.Servings, Is.EqualTo(2));
            Assert.That(dose.ChosenByChild, Is.False);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Remeasure));
        }

        [Test]
        public void GuardianPrompt_RunAgain_Eliminates()
        {
            var h = Triggered();
            h.Frames(5);

            Assert.That(h.Fsm.ChooseRunAgain(), Is.True);

            var eliminated = h.Last<RunEliminated>();
            Assert.That(eliminated.Step, Is.EqualTo(HypoStep.Recognize));
            Assert.That(eliminated.Reason, Is.EqualTo(HypoErrorCode.SMissed));
            Assert.That(eliminated.EventId, Is.EqualTo(1));
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Eliminated));

            h.Frames(10);
            Assert.That(h.All<StepAnswered>(), Has.Count.EqualTo(1));
            Assert.That(h.All<RunEliminated>(), Has.Count.EqualTo(1));
        }

        [Test]
        public void GuardianPrompt_Timeout_Eliminates()
        {
            var h = AtDose();
            h.Fsm.ChooseDose(DoseLevel.Over);

            h.Frames(5);

            Assert.That(h.Last<RunEliminated>().Reason, Is.EqualTo(HypoErrorCode.DsOver));
            Assert.That(h.All<DoseApplied>(), Is.Empty, "탈락하면 투입하지 않는다");
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Eliminated));
        }

        [Test]
        public void ChoiceInWrongPhase_IsIgnored()
        {
            var h = Triggered();

            Assert.That(h.Fsm.ChooseDose(DoseLevel.Proper), Is.False);
            Assert.That(h.Fsm.ChooseSnack("juice"), Is.False);
            Assert.That(h.Fsm.PressHelp(), Is.False, "1단계에는 알리기 버튼이 없다");
            Assert.That(h.All<StepAnswered>(), Is.Empty);

            h.Fsm.PressMeter();
            Assert.That(h.Fsm.ChooseSnack("juice"), Is.True);
            Assert.That(h.Fsm.ChooseSnack("juice"), Is.False);
            Assert.That(h.All<StepAnswered>().Count(a => a.Step == HypoStep.Respond), Is.EqualTo(1));
        }

        [Test]
        public void UnknownSnack_Throws()
        {
            var h = AtRespond();

            Assert.Throws<ArgumentException>(() => h.Fsm.ChooseSnack("candy"));
        }

        [Test]
        public void HugeFrame_AdvancesOnlyOneDeadline()
        {
            var h = Triggered();

            h.Frame(100);

            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.GuardianPrompt));
            Assert.That(h.All<RunEliminated>(), Is.Empty);
        }

        [Test]
        public void PauseDuringGuardianPrompt_DoesNotEliminate()
        {
            var h = Triggered();
            h.Frames(5);

            h.Pause(1000);

            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.GuardianPrompt));
        }

        [Test]
        public void Begin_WhenNotIdle_Throws()
        {
            var h = Triggered();

            Assert.Throws<InvalidOperationException>(() =>
                h.Fsm.Begin(new HypoEventTriggered(9, false, 0, 0, 60)));
        }
    }
}
