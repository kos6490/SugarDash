using System;
using System.Linq;
using SugarDash.Domain.Config;
using SugarDash.Domain.Events;
using SugarDash.Domain.Hypo;
using NUnit.Framework;

namespace SugarDash.Domain.Tests
{
    /// <summary>
    /// 선택 뒤 결과 문구 창(hypo.answerFeedbackSec). 대처 중 단계에 들어갈 때마다 문구 창이 먼저 열리고,
    /// 그동안 버튼은 무시되며, 다음 단계의 판단 시간과 마감은 문구 창이 끝난 뒤부터 잰다.
    /// 기본 설정: 문구 창 1초, 제한 시간 5초, 재개 카운트다운 3초.
    /// </summary>
    public class AnswerFeedbackTests
    {
        private const double Eps = 1e-9;
        private const double Feedback = 1;

        private static HypoHarness Triggered(Action<ConfigBuilder> configure = null)
        {
            var h = new HypoHarness(b =>
            {
                b.AnswerFeedbackSec = Feedback;
                b.ResumeCountdownSec = 3;
                configure?.Invoke(b);
            });
            h.RunUntilTriggered();
            return h;
        }

        private static HypoHarness AtRespond()
        {
            var h = Triggered();
            h.Fsm.PressMeter();
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Respond));
            return h;
        }

        /// <summary>문구 창이 끝날 때까지 진행한다.</summary>
        private static void SkipFeedback(HypoHarness h)
        {
            Assert.That(h.Fsm.IsShowingAnswerFeedback, Is.True);
            h.Frame(Feedback);
            Assert.That(h.Fsm.IsShowingAnswerFeedback, Is.False);
        }

        private static HypoHarness AtResumeCountdown()
        {
            var h = AtRespond();
            SkipFeedback(h);
            Assert.That(h.Fsm.ChooseSnack("juice"), Is.True);
            SkipFeedback(h);
            Assert.That(h.Fsm.ChooseDose(DoseLevel.Proper), Is.True);
            SkipFeedback(h);
            Assert.That(h.Fsm.ChooseRecheck(), Is.True);
            SkipFeedback(h);
            Assert.That(h.Fsm.PressHelp(HypoPhase.Notify), Is.True);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.ResumeCountdown));
            return h;
        }

        [Test]
        public void Recognize_HasNoFeedbackWindow()
        {
            var h = Triggered();

            Assert.That(h.Fsm.IsShowingAnswerFeedback, Is.False, "발동 직후 1단계는 문구 창 없이 바로 잰다");
        }

        [Test]
        public void ChoiceDuringFeedback_IsIgnored()
        {
            var h = AtRespond();
            Assert.That(h.Fsm.IsShowingAnswerFeedback, Is.True);

            Assert.That(h.Fsm.ChooseSnack("juice"), Is.False);
            Assert.That(h.Fsm.ChooseKeepRunning(), Is.False);
            Assert.That(h.Fsm.ChooseNoSugar(), Is.False);
            Assert.That(h.Fsm.PressHelp(HypoPhase.Respond), Is.False);
            h.Frame(Feedback / 2);
            Assert.That(h.Fsm.ChooseSnack("juice"), Is.False, "문구 창이 끝나기 전에는 계속 무시한다");

            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Respond));
            Assert.That(h.All<StepAnswered>(), Has.Count.EqualTo(1), "1단계 답만 있다");
            Assert.That(h.All<GuardianNotified>(), Is.Empty);

            h.Frame(Feedback / 2);
            Assert.That(h.Fsm.IsShowingAnswerFeedback, Is.False);
            Assert.That(h.Fsm.ChooseSnack("juice"), Is.True);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Dose));
        }

        [Test]
        public void ChoiceDuringFeedback_IsIgnoredInEveryCopingPhase()
        {
            var h = AtRespond();
            SkipFeedback(h);
            h.Fsm.ChooseSnack("juice");

            Assert.That(h.Fsm.ChooseDose(DoseLevel.Proper), Is.False, "3단계 문구 창");
            Assert.That(h.Fsm.PressHelp(HypoPhase.Dose), Is.False);
            SkipFeedback(h);
            h.Fsm.ChooseDose(DoseLevel.Proper);

            Assert.That(h.Fsm.ChooseRecheck(), Is.False, "4단계 ① 문구 창");
            Assert.That(h.Fsm.ChooseRunAgain(HypoPhase.Remeasure), Is.False);
            Assert.That(h.Fsm.PressHelp(HypoPhase.Remeasure), Is.False);
            SkipFeedback(h);
            h.Fsm.ChooseRunAgain(HypoPhase.Remeasure);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.GuardianPrompt));

            Assert.That(h.Fsm.PressHelp(HypoPhase.GuardianPrompt), Is.False, "알리기 대기 문구 창");
            Assert.That(h.Fsm.ChooseRunAgain(HypoPhase.GuardianPrompt), Is.False, "문구 창 중에는 탈락하지 않는다");
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.GuardianPrompt));
            SkipFeedback(h);
            h.Fsm.PressHelp(HypoPhase.GuardianPrompt);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Notify));

            Assert.That(h.Fsm.PressHelp(HypoPhase.Notify), Is.False, "4단계 ② 문구 창");
            Assert.That(h.Fsm.ChooseRunAgain(HypoPhase.Notify), Is.False);
            SkipFeedback(h);
            Assert.That(h.Fsm.PressHelp(HypoPhase.Notify), Is.True);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.ResumeCountdown));
        }

        [Test]
        public void HyperNotify_HasNoFeedbackWindow_ButResumeCountdownDoes()
        {
            // 주스 5인분(+150)을 먹고 3초 뒤 고혈당이 발동한다(HyperEventFsmTests와 같은 설정).
            var h = new HypoHarness(b => b.AnswerFeedbackSec = Feedback);
            h.Model.EatSnack("juice", 5);
            h.Frames(3);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.HyperNotify));

            Assert.That(h.Fsm.IsShowingAnswerFeedback, Is.False);
            Assert.That(h.Fsm.PressHelp(HypoPhase.HyperNotify), Is.True);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.ResumeCountdown));
            Assert.That(h.Fsm.IsShowingAnswerFeedback, Is.True);
        }

        [Test]
        public void DecisionTime_StartsAfterFeedback()
        {
            var h = AtRespond();
            SkipFeedback(h);
            h.Frames(2);

            h.Fsm.ChooseSnack("juice");

            var answer = h.All<StepAnswered>().Single(a => a.Step == HypoStep.Respond);
            Assert.That(answer.DecisionSec, Is.EqualTo(2).Within(Eps));
        }

        [Test]
        public void Deadline_IncludesFeedback()
        {
            var h = AtRespond();

            // 문구 창 1초 + 제한 시간 5초 = 6초. 5.9초까지는 아직 2단계다.
            h.Frames(5);
            h.Frame(0.9);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Respond));

            h.Frame(0.1);

            var answer = h.All<StepAnswered>().Single(a => a.Step == HypoStep.Respond);
            Assert.That(answer.ErrorCode, Is.EqualTo(HypoErrorCode.RsTimeout));
            Assert.That(answer.DecisionSec, Is.EqualTo(5).Within(Eps), "판단 시간은 문구 창을 빼고 잰다");
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.GuardianPrompt));
        }

        [Test]
        public void GuardianPromptDeadline_IncludesFeedback()
        {
            var h = AtRespond();
            SkipFeedback(h);
            h.Fsm.ChooseKeepRunning();
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.GuardianPrompt));

            h.Frames(5);
            h.Frame(0.9);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.GuardianPrompt), "문구 창 1초 + 알리기 대기 5초");

            h.Frame(0.1);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Eliminated));
            Assert.That(h.Last<RunEliminated>().Cause, Is.EqualTo(EliminationCause.PromptTimeout));
        }

        [Test]
        public void ZeroFeedback_BehavesAsBefore()
        {
            var h = new HypoHarness(b => b.AnswerFeedbackSec = 0);
            h.RunUntilTriggered();
            h.Fsm.PressMeter();

            Assert.That(h.Fsm.IsShowingAnswerFeedback, Is.False);
            h.Frames(4);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Respond));
            h.Frame(1);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.GuardianPrompt), "마감은 전환 시각 + 제한 시간");
            Assert.That(h.Fsm.IsShowingAnswerFeedback, Is.False);
            Assert.That(h.Fsm.PressHelp(HypoPhase.GuardianPrompt), Is.True, "문구 창이 없으면 바로 누를 수 있다");
        }

        [Test]
        public void ResumeCountdownRemaining_StartsAfterFeedbackAndCountsDown()
        {
            var h = AtResumeCountdown();

            Assert.That(h.Fsm.IsShowingAnswerFeedback, Is.True);
            Assert.That(h.Fsm.ResumeCountdownRemaining, Is.EqualTo(0), "문구 창 중에는 카운트다운이 없다");

            h.Frame(Feedback);
            Assert.That(h.Fsm.IsShowingAnswerFeedback, Is.False);
            Assert.That(h.Fsm.ResumeCountdownRemaining, Is.EqualTo(3).Within(Eps));

            h.Frame(1);
            Assert.That(h.Fsm.ResumeCountdownRemaining, Is.EqualTo(2).Within(Eps));
            h.Frame(1.5);
            Assert.That(h.Fsm.ResumeCountdownRemaining, Is.EqualTo(0.5).Within(Eps));
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.ResumeCountdown));

            h.Frame(0.5);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Grace));
            Assert.That(h.Fsm.ResumeCountdownRemaining, Is.EqualTo(0));
            Assert.That(h.Fsm.IsShowingAnswerFeedback, Is.False, "유예 중에는 문구 창이 없다");
        }

        [Test]
        public void ResumeCountdownRemaining_IsZeroOutsideCountdown()
        {
            var h = AtRespond();
            SkipFeedback(h);

            Assert.That(h.Fsm.ResumeCountdownRemaining, Is.EqualTo(0));
        }
    }
}
