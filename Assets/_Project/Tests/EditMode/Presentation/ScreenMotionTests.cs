using NUnit.Framework;

namespace SugarDash.Presentation.Tests
{
    public class ScreenMotionTests
    {
        [Test]
        public void PopIn_StartsFaintAndSmall_EndsFull()
        {
            var start = ScreenPopIn.Progress(0, 0.2f, 0.9f);
            Assert.That(start.Alpha, Is.EqualTo(0).Within(1e-6));
            Assert.That(start.Scale, Is.EqualTo(0.9f).Within(1e-6));

            var mid = ScreenPopIn.Progress(0.1f, 0.2f, 0.9f);
            Assert.That(mid.Alpha, Is.GreaterThan(0.5f).And.LessThan(1f), "ease-out이라 절반 시간에 절반보다 많이 나타난다");

            var end = ScreenPopIn.Progress(0.5f, 0.2f, 0.9f);
            Assert.That(end.Alpha, Is.EqualTo(1).Within(1e-6));
            Assert.That(end.Scale, Is.EqualTo(1).Within(1e-6));
        }

        [Test]
        public void PopIn_ZeroDuration_IsImmediatelyFull()
        {
            var p = ScreenPopIn.Progress(0, 0, 0.5f);
            Assert.That(p.Alpha, Is.EqualTo(1).Within(1e-6));
            Assert.That(p.Scale, Is.EqualTo(1).Within(1e-6));
        }

        [Test]
        public void Blink_FullAtPeriodEdges_FaintestInMiddle()
        {
            Assert.That(NoticeSignalView.BlinkAlpha(0, 1, 0.3f), Is.EqualTo(1).Within(1e-5));
            Assert.That(NoticeSignalView.BlinkAlpha(0.5f, 1, 0.3f), Is.EqualTo(0.3f).Within(1e-5));
            Assert.That(NoticeSignalView.BlinkAlpha(1.25f, 1, 0.3f), Is.EqualTo(NoticeSignalView.BlinkAlpha(0.25f, 1, 0.3f)).Within(1e-5));
            Assert.That(NoticeSignalView.BlinkAlpha(0.5f, 0, 0.3f), Is.EqualTo(1));
        }

        [Test]
        public void Fade_OnEveryScreenChange_ExceptIntoElimination()
        {
            Assert.That(ScreenFadeView.ShouldFade(null, SugarDash.App.FlowScreen.Title), Is.True, "처음 화면");
            Assert.That(ScreenFadeView.ShouldFade(SugarDash.App.FlowScreen.Title, SugarDash.App.FlowScreen.Settings), Is.True);
            Assert.That(ScreenFadeView.ShouldFade(SugarDash.App.FlowScreen.Title, SugarDash.App.FlowScreen.Running), Is.True);
            Assert.That(ScreenFadeView.ShouldFade(SugarDash.App.FlowScreen.Elimination, SugarDash.App.FlowScreen.Result), Is.True);
            Assert.That(ScreenFadeView.ShouldFade(SugarDash.App.FlowScreen.Running, SugarDash.App.FlowScreen.Elimination), Is.False);
            Assert.That(ScreenFadeView.ShouldFade(SugarDash.App.FlowScreen.Title, SugarDash.App.FlowScreen.Title), Is.False);
        }

        [Test]
        public void FadeAlpha_FromOpaqueToClear()
        {
            Assert.That(ScreenFadeView.Alpha(0, 0.25f), Is.EqualTo(1).Within(1e-6));
            Assert.That(ScreenFadeView.Alpha(0.125f, 0.25f), Is.LessThan(0.5f).And.GreaterThan(0f));
            Assert.That(ScreenFadeView.Alpha(0.3f, 0.25f), Is.EqualTo(0).Within(1e-6));
            Assert.That(ScreenFadeView.Alpha(0, 0), Is.EqualTo(0));
        }
    }
}
