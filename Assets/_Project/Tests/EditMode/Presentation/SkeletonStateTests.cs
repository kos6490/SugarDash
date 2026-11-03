using NUnit.Framework;
using SugarDash.App;
using SugarDash.Domain.Config;
using SugarDash.Domain.Director;

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
    }
}
