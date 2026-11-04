using System.Linq;
using NUnit.Framework;
using SugarDash.App;
using SugarDash.Domain.Config;
using SugarDash.Domain.Director;
using UnityEngine;

namespace SugarDash.Presentation.Tests
{
    public class SkeletonStateTests
    {
        [Test]
        public void CharacterState_Priority()
        {
            Assert.That(CharacterStateView.StateOf(false, null), Is.EqualTo(CharacterState.Normal));
            Assert.That(CharacterStateView.StateOf(true, null), Is.EqualTo(CharacterState.Resting));
            Assert.That(CharacterStateView.StateOf(true, NoticeKind.High), Is.EqualTo(CharacterState.TiredHigh));
            Assert.That(CharacterStateView.StateOf(true, NoticeKind.Low), Is.EqualTo(CharacterState.TiredLow));
            Assert.That(CharacterStateView.StateOf(false, NoticeKind.High), Is.EqualTo(CharacterState.TiredHigh));
            Assert.That(CharacterStateView.StateOf(false, NoticeKind.Low), Is.EqualTo(CharacterState.TiredLow));
        }

        [Test]
        public void TutorialPrompt_Visibility()
        {
            foreach (TutorialStage s in System.Enum.GetValues(typeof(TutorialStage)))
            {
                Assert.That(TutorialPromptView.IsPromptVisible(s), Is.EqualTo(s == TutorialStage.Prompt));
                Assert.That(TutorialPromptView.IsRetryVisible(s), Is.EqualTo(s == TutorialStage.Retry));
            }
        }

        [Test]
        public void TutorialPrompt_AcceptsTapAfterDelay()
        {
            Assert.That(TutorialPromptView.AcceptsTap(0f, 0.5f), Is.False);
            Assert.That(TutorialPromptView.AcceptsTap(0.49f, 0.5f), Is.False);
            Assert.That(TutorialPromptView.AcceptsTap(0.5f, 0.5f), Is.True);
            Assert.That(TutorialPromptView.AcceptsTap(2f, 0.5f), Is.True);
            Assert.That(TutorialPromptView.AcceptsTap(0f, 0f), Is.True);
        }

        [Test]
        public void ScreenVisibility_PerFlowScreen()
        {
            foreach (FlowScreen s in System.Enum.GetValues(typeof(FlowScreen)))
            {
                Assert.That(TitleScreenView.IsVisible(s), Is.EqualTo(s == FlowScreen.Title));
                Assert.That(TypeSelectView.IsVisible(s), Is.EqualTo(s == FlowScreen.TypeSelect));
                Assert.That(SettingsView.IsVisible(s), Is.EqualTo(s == FlowScreen.Settings));
            }
        }

        [Test]
        public void TitleTypeLabel()
        {
            Assert.That(TitleScreenView.TypeLabel(TrackType.Type1), Is.EqualTo("1형으로 달려요"));
            Assert.That(TitleScreenView.TypeLabel(TrackType.Type2), Is.EqualTo("2형으로 달려요"));
            Assert.That(TitleScreenView.TypeLabel(null), Is.EqualTo(""));
        }

        [Test]
        public void SettingsCurrentLabel()
        {
            Assert.That(SettingsView.CurrentLabel(TrackType.Type1), Is.EqualTo("1형"));
            Assert.That(SettingsView.CurrentLabel(TrackType.Type2), Is.EqualTo("2형"));
            Assert.That(SettingsView.CurrentLabel(null), Is.EqualTo(""));
        }

        [Test]
        public void NoticeSignal_Labels()
        {
            Assert.That(NoticeSignalView.LabelOf(null), Is.EqualTo(""));
            Assert.That(NoticeSignalView.LabelOf(NoticeKind.High), Is.Not.Empty);
            Assert.That(NoticeSignalView.LabelOf(NoticeKind.Low), Is.Not.Empty);
            Assert.That(NoticeSignalView.LabelOf(NoticeKind.High), Is.Not.EqualTo(NoticeSignalView.LabelOf(NoticeKind.Low)));
        }

        [Test]
        public void SceneLook_GroundColor_DistinctPerScene()
        {
            var colors = new[] { "schoolyard", "playground", "outing", "home" }.Select(SceneLook.GroundColorOf).ToList();

            Assert.That(colors.Distinct().Count(), Is.EqualTo(4));
            Assert.That(colors, Has.None.EqualTo(Color.gray));
            Assert.That(SceneLook.GroundColorOf("nope"), Is.EqualTo(Color.gray));
            Assert.That(SceneLook.GroundColorOf(null), Is.EqualTo(Color.gray));
        }

        [Test]
        public void SleepTransition_ShouldFade_OnlyWhenDayAdvances()
        {
            Assert.That(SleepTransitionView.ShouldFade(0, 0), Is.False);
            Assert.That(SleepTransitionView.ShouldFade(0, 1), Is.True);
            Assert.That(SleepTransitionView.ShouldFade(1, 1), Is.False);
            Assert.That(SleepTransitionView.ShouldFade(2, 0), Is.False, "새 주행");
        }

        [TestCase(0f, 1f)]
        [TestCase(0.1f, 1f)]
        [TestCase(0.2f, 1f)]
        [TestCase(0.5f, 0.5f)]
        [TestCase(0.8f, 0f)]
        [TestCase(5f, 0f)]
        public void SleepTransition_AlphaCurve_StartsBlack(float elapsed, float expected)
        {
            Assert.That(SleepTransitionView.AlphaAt(elapsed, 0.2f, 0.6f), Is.EqualTo(expected).Within(1e-5f));
        }

        [Test]
        public void SleepTransition_AlphaCurve_ZeroLengths()
        {
            Assert.That(SleepTransitionView.AlphaAt(0f, 0f, 0f), Is.EqualTo(0f));
            Assert.That(SleepTransitionView.AlphaAt(0f, 0f, 0.6f), Is.EqualTo(1f));
        }
    }
}
