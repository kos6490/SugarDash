using NUnit.Framework;
using SugarDash.App;
using SugarDash.Domain.Config;

namespace SugarDash.Presentation.Tests
{
    public class TutorialTextTests
    {
        // TutorialController의 안내 순서(비공개)와 같게 둔다.
        private static readonly TutorialStep[] Type1Order =
        {
            TutorialStep.MoveLeft, TutorialStep.MoveRight, TutorialStep.Jump, TutorialStep.Slide, TutorialStep.Snack,
            TutorialStep.HypoRecognize, TutorialStep.HypoRespond, TutorialStep.HypoDose, TutorialStep.HypoRemeasure,
            TutorialStep.HypoNotify, TutorialStep.HyperNotify, TutorialStep.Finish,
        };

        private static readonly TutorialStep[] Type2Order =
        {
            TutorialStep.MoveLeft, TutorialStep.MoveRight, TutorialStep.Jump, TutorialStep.Slide, TutorialStep.FoodRow,
            TutorialStep.Rest, TutorialStep.HighNotice, TutorialStep.HighTell, TutorialStep.LowNotice,
            TutorialStep.LowTell, TutorialStep.Finish,
        };

        private static readonly string[] Forbidden = { "나쁜", "죽", "생존", "치료" };

        [Test]
        public void EveryStepHasText_BothTypes()
        {
            foreach (var step in Type1Order) Assert.That(TutorialText.For(step, TrackType.Type1), Is.Not.Empty, step.ToString());
            foreach (var step in Type2Order) Assert.That(TutorialText.For(step, TrackType.Type2), Is.Not.Empty, step.ToString());
        }

        [Test]
        public void FixedTexts_AndShared()
        {
            Assert.That(TutorialText.TryAgain, Is.EqualTo("다시 해봐요"));
            Assert.That(TutorialText.Acknowledge, Is.EqualTo("알겠어요"));
            Assert.That(TutorialText.Skip, Is.EqualTo("건너뛰기"));
            Assert.That(TutorialText.SettingsButton, Is.EqualTo("튜토리얼 다시 하기"));
            Assert.That(TutorialText.For(TutorialStep.Jump, TrackType.Type2), Is.EqualTo(TutorialText.For(TutorialStep.Jump, TrackType.Type1)));
            Assert.That(TutorialText.For(TutorialStep.HighTell, TrackType.Type2), Is.EqualTo("어른에게 알리기를 골라요"));
            Assert.That(TutorialText.For(TutorialStep.LowTell, TrackType.Type2), Is.EqualTo("어른에게 알리기를 골라요"));
            Assert.That(TutorialText.For(TutorialStep.Finish, TrackType.Type1), Is.EqualTo("이제 혼자 달려 봐요!"));
        }

        [Test]
        public void NoForbiddenWords()
        {
            var all = new System.Collections.Generic.List<string>
            {
                TutorialText.TryAgain, TutorialText.Acknowledge, TutorialText.Skip, TutorialText.SettingsButton,
                ResultScreenText.TutorialTitle,
            };
            foreach (var step in Type1Order) all.Add(TutorialText.For(step, TrackType.Type1));
            foreach (var step in Type2Order) all.Add(TutorialText.For(step, TrackType.Type2));
            foreach (var text in all)
                foreach (var word in Forbidden) Assert.That(text, Does.Not.Contain(word), text);
        }
    }
}
