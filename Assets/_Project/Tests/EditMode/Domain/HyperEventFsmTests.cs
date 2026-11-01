using System;
using System.Linq;
using SugarDash.Domain.Events;
using SugarDash.Domain.Hypo;
using NUnit.Framework;

namespace SugarDash.Domain.Tests
{
    /// <summary>HypoEventFsm의 고혈당 "어른에게 알리기" 단계(docs/hyper-event.md).</summary>
    public class HyperEventFsmTests
    {
        private const double Eps = 1e-9;

        /// <summary>주스 5인분(+150)을 먹고 3초 뒤 264(달리기 감소 2/s)에서 고혈당이 발동한다.</summary>
        private static HypoHarness Triggered(Action<ConfigBuilder> configure = null)
        {
            var h = new HypoHarness(configure);
            h.Model.EatSnack("juice", 5);
            h.Frames(3);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.HyperNotify));
            return h;
        }

        private static StepAnswered Answer(HypoHarness h) =>
            h.All<StepAnswered>().Single(a => a.Step == HypoStep.HyperNotify);

        [Test]
        public void Trigger_FreezesWorld_ButGlucoseTimeFlows()
        {
            var h = Triggered();
            double simT = h.Model.SimTime;

            h.Frame(1);

            Assert.That(h.Fsm.IsWorldFrozen, Is.True);
            Assert.That(h.Fsm.EventId, Is.EqualTo(1));
            Assert.That(h.Model.SimTime, Is.EqualTo(simT + 1).Within(Eps));
            Assert.That(h.Model.Actual, Is.EqualTo(264).Within(Eps), "세계가 멈춰 달리기 감소가 없다");
        }

        [Test]
        public void PressHelp_IsCorrect_ThenResumesWithGrace()
        {
            var h = Triggered();
            h.Frame(1);

            Assert.That(h.Fsm.PressHelp(HypoPhase.HyperNotify), Is.True);

            var answer = Answer(h);
            Assert.That(answer.IsCorrect, Is.True);
            Assert.That(answer.ChoiceId, Is.EqualTo("help"));
            Assert.That(answer.DecisionSec, Is.EqualTo(1).Within(Eps));
            Assert.That(h.Last<HyperResolved>().AllCorrect, Is.True);
            Assert.That(h.All<HypoResolved>(), Is.Empty);
            Assert.That(h.All<GuardianPromptStarted>(), Is.Empty);
            Assert.That(h.Director.IsHyperEventActive, Is.False);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.ResumeCountdown));

            h.Frames(1);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Grace));
            h.Frames(3);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Idle));
            Assert.That(h.All<GraceEnded>(), Has.Count.EqualTo(1));
        }

        [Test]
        public void KeepRunning_IsError_ThenNotifyInPromptResolves()
        {
            var h = Triggered();

            Assert.That(h.Fsm.ChooseKeepRunning(), Is.True);

            var answer = Answer(h);
            Assert.That(answer.IsCorrect, Is.False);
            Assert.That(answer.ChoiceId, Is.EqualTo("keep_running"));
            Assert.That(answer.ErrorCode, Is.EqualTo(HypoErrorCode.HyKeepRunning));
            var prompt = h.Last<GuardianPromptStarted>();
            Assert.That(prompt.Step, Is.EqualTo(HypoStep.HyperNotify));
            Assert.That(prompt.ErrorCode, Is.EqualTo(HypoErrorCode.HyKeepRunning));
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.GuardianPrompt));
            Assert.That(h.Fsm.ChooseKeepRunning(), Is.False, "연타한 계속 달리기는 알리기 대기에서 무시한다");

            h.Fsm.PressHelp(HypoPhase.GuardianPrompt);

            Assert.That(h.Last<GuardianNotified>().Step, Is.EqualTo(HypoStep.HyperNotify));
            Assert.That(h.Last<HyperResolved>().AllCorrect, Is.False);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.ResumeCountdown));
        }

        [Test]
        public void Timeout_IsError()
        {
            var h = Triggered();

            h.Frames(5);

            var answer = Answer(h);
            Assert.That(answer.ChoiceId, Is.Null);
            Assert.That(answer.ErrorCode, Is.EqualTo(HypoErrorCode.HyTimeout));
            Assert.That(answer.DecisionSec, Is.EqualTo(5).Within(Eps));
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.GuardianPrompt));
        }

        [Test]
        public void DecliningInPrompt_Eliminates()
        {
            var h = Triggered();
            h.Fsm.ChooseKeepRunning();

            h.Fsm.ChooseRunAgain(HypoPhase.GuardianPrompt);

            var eliminated = h.Last<RunEliminated>();
            Assert.That(eliminated.Step, Is.EqualTo(HypoStep.HyperNotify));
            Assert.That(eliminated.Reason, Is.EqualTo(HypoErrorCode.HyKeepRunning));
            Assert.That(eliminated.Cause, Is.EqualTo(EliminationCause.Declined));
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Eliminated));
            Assert.That(h.All<HyperResolved>(), Is.Empty);
        }

        [Test]
        public void PromptTimeout_Eliminates()
        {
            var h = Triggered();
            h.Frames(5);

            h.Frames(5);

            Assert.That(h.Last<RunEliminated>().Cause, Is.EqualTo(EliminationCause.PromptTimeout));
            Assert.That(h.Last<RunEliminated>().Reason, Is.EqualTo(HypoErrorCode.HyTimeout));
        }

        [Test]
        public void OtherCommands_AreIgnored()
        {
            var h = Triggered();

            h.Fsm.PressMeter();
            Assert.That(h.Fsm.ChooseSnack("juice"), Is.False);
            Assert.That(h.Fsm.ChooseNoSugar(), Is.False);
            Assert.That(h.Fsm.ChooseRecheck(), Is.False);
            Assert.That(h.Fsm.ChooseRunAgain(HypoPhase.HyperNotify), Is.False);
            Assert.That(h.Fsm.PressHelp(HypoPhase.GuardianPrompt), Is.False, "화면이 보여 주던 단계가 다르면 무시한다");

            Assert.That(h.All<MeterPressed>(), Is.Empty);
            Assert.That(h.All<StepAnswered>(), Is.Empty);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.HyperNotify));
        }

        /// <summary>과잉 처치 → 저혈당 대처 마침 → 카운트다운·유예 뒤 고혈당 발동(docs/glucose-model.md 3절).</summary>
        [Test]
        public void OverDose_LeadsToHyperAfterGrace()
        {
            var h = new HypoHarness(b =>
            {
                b.HyperThreshold = 150;
                b.DoseOver = 4;
            });
            h.RunUntilTriggered(); // 68
            h.Fsm.PressMeter();
            h.Fsm.ChooseSnack("juice");
            h.Fsm.ChooseDose(DoseLevel.Over);
            h.Fsm.PressHelp(HypoPhase.GuardianPrompt); // 4인분(+120)
            h.Fsm.ChooseRecheck(); // 빨리 감기 뒤 188
            h.Fsm.PressHelp(HypoPhase.Notify);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.ResumeCountdown));

            for (int i = 0; i < 10 && h.Fsm.Phase != HypoPhase.HyperNotify; i++) h.Frame(1);

            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.HyperNotify));
            var hyper = h.Last<HyperEventTriggered>();
            Assert.That(hyper.EventId, Is.EqualTo(2));
            Assert.That(h.Events.IndexOf(hyper), Is.GreaterThan(h.Events.IndexOf(h.Last<GraceEnded>())),
                "유예가 끝난 뒤에 발동한다");
            Assert.That(hyper.Actual, Is.EqualTo(180).Within(Eps));
        }
    }
}
