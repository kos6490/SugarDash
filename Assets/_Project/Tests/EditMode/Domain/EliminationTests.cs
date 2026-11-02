using System.Linq;
using SugarDash.Domain.Events;
using SugarDash.Domain.Hypo;
using NUnit.Framework;

namespace SugarDash.Domain.Tests
{
    /// <summary>세 가지 탈락(알리지 않음, 혈당 안전 범위 이탈, 하트 모두 잃음)을 RunEliminated 하나로 낸다.</summary>
    public class EliminationTests
    {
        private static HypoHarness Harness(System.Action<ConfigBuilder> configure = null) =>
            new HypoHarness(b =>
            {
                b.GlucoseLow = 65;
                configure?.Invoke(b);
            });

        /// <summary>
        /// 대처 중에도 혈당이 내려가는 설정. 달리기 2/s + 기본 1/s로 119에서 17초에 68로 발동하고,
        /// 세계가 멈춘 동안은 기본 감소 1/s만 남는다.
        /// </summary>
        private static HypoHarness CopingHarness(System.Action<ConfigBuilder> configure = null) =>
            Harness(b =>
            {
                b.Initial = 119;
                b.BaseDecayPerSec = 1;
                b.SuddenDropAmount = 1;
                b.GuardianPromptTimeLimitSec = 2;
                configure?.Invoke(b);
            });

        [Test]
        public void Running_BelowLow_Eliminates()
        {
            var h = Harness();
            h.RunUntilTriggered();
            int eventId = h.Fsm.EventId;
            Assert.That(h.Model.Actual, Is.EqualTo(68).Within(1e-9));

            h.Frame(1); // 66: 아직 범위 안
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Recognize));
            h.Frame(1); // 64

            Assert.That(h.Model.Actual, Is.LessThan(65));
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Eliminated));
            var eliminated = h.All<RunEliminated>().Single();
            Assert.That(eliminated.Cause, Is.EqualTo(EliminationCause.GlucoseLow));
            Assert.That(eliminated.Step, Is.Null);
            Assert.That(eliminated.Reason, Is.EqualTo(HypoErrorCode.None));
            Assert.That(eliminated.EventId, Is.EqualTo(eventId));
            Assert.That(eventId, Is.GreaterThan(0));
        }

        [Test]
        public void Coping_BelowLowWithinLimit_DoesNotEliminate()
        {
            var h = CopingHarness();
            h.RunUntilTriggered();
            h.Fsm.PressMeter();
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Respond));

            h.Frames(4);

            Assert.That(h.Model.Actual, Is.LessThan(65));
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Respond));
            Assert.That(h.All<RunEliminated>(), Is.Empty);
        }

        [Test]
        public void Coping_TimeoutWhileBelowLow_EliminatesAfterAnswer()
        {
            var h = CopingHarness();
            h.RunUntilTriggered();
            h.Fsm.PressMeter();

            h.Frames(5);

            Assert.That(h.Model.Actual, Is.LessThan(65));
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Eliminated));
            var answer = h.All<StepAnswered>().Single(a => a.Step == HypoStep.Respond);
            Assert.That(answer.ErrorCode, Is.EqualTo(HypoErrorCode.RsTimeout));
            Assert.That(answer.IsCorrect, Is.False);
            var eliminated = h.All<RunEliminated>().Single();
            Assert.That(h.Events.IndexOf(answer), Is.LessThan(h.Events.IndexOf(eliminated)));
            Assert.That(eliminated.Cause, Is.EqualTo(EliminationCause.GlucoseLow));
            Assert.That(eliminated.Step, Is.EqualTo(HypoStep.Respond));
            Assert.That(eliminated.Reason, Is.EqualTo(HypoErrorCode.RsTimeout));
            Assert.That(h.All<GuardianPromptStarted>(), Is.Empty);
        }

        [Test]
        public void Coping_TimeoutInRange_GoesToGuardianPrompt()
        {
            var h = CopingHarness(b => b.GlucoseLow = 20);
            h.RunUntilTriggered();
            h.Fsm.PressMeter();

            h.Frames(5);

            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.GuardianPrompt));
            Assert.That(h.All<RunEliminated>(), Is.Empty);
        }

        [Test]
        public void GuardianPromptTimeout_WhileBelowLow_IsPromptTimeout()
        {
            var h = CopingHarness();
            h.RunUntilTriggered();
            h.Fsm.PressMeter();
            h.Frames(3);
            Assert.That(h.Fsm.ChooseKeepRunning(), Is.True);
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.GuardianPrompt));

            h.Frames(2);

            Assert.That(h.Model.Actual, Is.LessThan(65));
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Eliminated));
            var eliminated = h.All<RunEliminated>().Single();
            Assert.That(eliminated.Cause, Is.EqualTo(EliminationCause.PromptTimeout));
            Assert.That(eliminated.Step, Is.EqualTo(HypoStep.Respond));
            Assert.That(eliminated.Reason, Is.EqualTo(HypoErrorCode.RsKeepRunning));
        }

        [Test]
        public void HyperNotifyTimeout_AboveHigh_EliminatesGlucoseHigh()
        {
            var h = Harness(b =>
            {
                b.RunDecayPerSec = 0;
                b.HyperThreshold = 135;
                b.GlucoseHigh = 140;
            });
            h.Model.EatSnack("juice", 1); // 30 상승, onset 1, absorb 2

            h.Frames(2);
            Assert.That(h.Model.Actual, Is.EqualTo(135).Within(1e-9));
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.HyperNotify));
            h.Frame(1);
            Assert.That(h.Model.Actual, Is.EqualTo(150).Within(1e-9));
            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.HyperNotify), "대처 중에는 시간 초과 순간에만 판정한다");

            h.Frames(4); // 발동 5초 뒤 시간 초과

            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Eliminated));
            var eliminated = h.All<RunEliminated>().Single();
            Assert.That(eliminated.Cause, Is.EqualTo(EliminationCause.GlucoseHigh));
            Assert.That(eliminated.Step, Is.EqualTo(HypoStep.HyperNotify));
            Assert.That(eliminated.Reason, Is.EqualTo(HypoErrorCode.HyTimeout));
        }

        [Test]
        public void EliminateObstacleHit_PublishesOnce()
        {
            var h = Harness();

            h.Fsm.EliminateObstacleHit();
            h.Fsm.EliminateObstacleHit();

            Assert.That(h.Fsm.Phase, Is.EqualTo(HypoPhase.Eliminated));
            var eliminated = h.All<RunEliminated>().Single();
            Assert.That(eliminated.Cause, Is.EqualTo(EliminationCause.ObstacleHit));
            Assert.That(eliminated.Step, Is.Null);
            Assert.That(eliminated.Reason, Is.EqualTo(HypoErrorCode.None));
            Assert.That(eliminated.EventId, Is.EqualTo(0));
        }

        [Test]
        public void Elimination_RecordsDistance()
        {
            var h = Harness();
            h.Distance = 123.5;

            h.Fsm.EliminateObstacleHit();

            Assert.That(h.Last<RunEliminated>().DistanceM, Is.EqualTo(123.5));
        }
    }
}
