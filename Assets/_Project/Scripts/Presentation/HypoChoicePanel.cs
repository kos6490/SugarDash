using System;
using System.Collections.Generic;
using SugarDash.App;
using SugarDash.Domain.Config;
using SugarDash.Domain.Events;
using SugarDash.Domain.Hypo;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 2~4단계 선택지, 고혈당 알리기, 오류 뒤 "어른에게 알리기" 대기 화면. 단계가 바뀔 때마다 버튼 템플릿을 복제해 다시 만든다.
    /// 버튼은 만들 때의 단계를 기억해 명령과 함께 넘긴다(연타로 다음 단계에 잘못 들어가지 않게).
    /// 선택 뒤 결과 문구 창 동안에는 무엇을 골랐는지 문구만 보여 주고 버튼은 없다. 창이 끝나면 지금 단계 화면을 그린다.
    /// 재개 카운트다운은 패널을 숨기고 HudPresenter가 숫자로 보여 준다.
    /// </summary>
    public sealed class HypoChoicePanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text titleText;
        [SerializeField] private Text recheckText;
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private Button buttonTemplate;

        private readonly List<Button> _buttons = new List<Button>();
        private RunController _run;
        private IReadOnlyList<SnackConfig> _snacks;
        private HypoPhase? _shown;
        private bool _shownFeedback;
        // 마지막 선택의 결과 문구. 문구 창 동안 제목에 보여 준다.
        private string _feedback = "";
        private System.Random _random;

        /// <summary>GameBootstrapper가 연결한다. 반환값을 Dispose하면 구독을 해제한다.</summary>
        public IDisposable Bind(RunController run, DomainEventBus bus, IReadOnlyList<SnackConfig> snacks,
            int seed)
        {
            _random = new System.Random(seed);
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _snacks = snacks ?? throw new ArgumentNullException(nameof(snacks));

            buttonTemplate.gameObject.SetActive(false);
            recheckText.text = "";
            root.SetActive(false);

            var subscriptions = new List<IDisposable>
            {
                bus.Subscribe<HypoEventTriggered>(_ => recheckText.text = ""),
                bus.Subscribe<StepAnswered>(e => _feedback = AnswerFeedbackText.For(e)),
                bus.Subscribe<PracticeRetried>(_ => _feedback = TutorialText.TryAgain),
                bus.Subscribe<GuardianNotified>(_ => _feedback = AnswerFeedbackText.ForGuardianNotified()),
                bus.Subscribe<RecheckFastForward>(e => recheckText.text = ChoicePanelText.RecheckResult(e.GlucoseAfter)),
            };

            return new Unbinder(() =>
            {
                foreach (var subscription in subscriptions) subscription.Dispose();
                _run = null;
            });
        }

        private void Update()
        {
            if (_run == null) return;

            var phase = _run.Phase;
            bool feedback = _run.IsShowingAnswerFeedback;
            if (phase == _shown && feedback == _shownFeedback) return;

            _shown = phase;
            _shownFeedback = feedback;
            Rebuild(phase, feedback);
        }

        private void Rebuild(HypoPhase phase, bool feedback)
        {
            foreach (var button in _buttons) Destroy(button.gameObject);
            _buttons.Clear();

            if (feedback)
            {
                Show(_feedback);
                return;
            }

            switch (phase)
            {
                case HypoPhase.Respond:
                    bool emptyBag = _run.IsEmptyBag;
                    Show(emptyBag ? ChoicePanelText.EmptyBagTitle : ChoicePanelText.RespondTitle);
                    var respond = new List<(string, Action)>();
                    foreach (var snack in ChoiceOrder.RespondSnacks(_snacks, _random, emptyBag))
                    {
                        string id = snack.Id;
                        respond.Add((FoodText.Name(id), () => _run.ChooseSnack(id)));
                    }
                    respond.Add((ChoicePanelText.KeepRunning, () => _run.ChooseKeepRunning()));
                    // 빈 간식 가방에서는 "먹을 게 없어요"를 빼 "어른에게 알리기"와 뜻이 겹치지 않게 한다.
                    if (!emptyBag) respond.Add((ChoicePanelText.NoSugar, () => _run.ChooseNoSugar()));
                    AddShuffled(respond);
                    AddHelp(phase);
                    break;
                case HypoPhase.Dose:
                    Show(ChoicePanelText.DoseTitle(_run.DoseSnackId));
                    AddShuffled(new List<(string, Action)>
                    {
                        (ChoicePanelText.DoseUnder, () => _run.ChooseDose(DoseLevel.Under)),
                        (ChoicePanelText.DoseProper, () => _run.ChooseDose(DoseLevel.Proper)),
                        (ChoicePanelText.DoseOver, () => _run.ChooseDose(DoseLevel.Over)),
                    });
                    AddHelp(phase);
                    break;
                case HypoPhase.Remeasure:
                    Show(ChoicePanelText.NextActionTitle);
                    AddShuffled(new List<(string, Action)>
                    {
                        (ChoicePanelText.Recheck, () => _run.ChooseRecheck()),
                        (ChoicePanelText.RunAgain, () => _run.ChooseRunAgain(phase)),
                    });
                    AddHelp(phase);
                    break;
                case HypoPhase.Notify:
                    Show(ChoicePanelText.NextActionTitle);
                    Add(ChoicePanelText.RunAgain, () => _run.ChooseRunAgain(phase));
                    AddHelp(phase);
                    break;
                case HypoPhase.HyperNotify:
                    if (_run.TrackType == TrackType.Type2)
                    {
                        Show(ChoicePanelText.Type2TellTitle);
                        AddShuffled(new List<(string, Action)>
                        {
                            (ChoicePanelText.KeepRunning, () => _run.ChooseKeepRunning()),
                            (ChoicePanelText.Juice, () => _run.ChooseJuice()),
                        });
                    }
                    else
                    {
                        Show(ChoicePanelText.HighTitle);
                        Add(ChoicePanelText.KeepRunning, () => _run.ChooseKeepRunning());
                    }
                    AddHelp(phase);
                    break;
                case HypoPhase.LowNotify:
                    Show(ChoicePanelText.Type2TellTitle);
                    Add(ChoicePanelText.KeepRunning, () => _run.ChooseKeepRunning());
                    AddHelp(phase);
                    break;
                case HypoPhase.GuardianPrompt:
                    Show(ChoicePanelText.GuardianPromptTitle);
                    Add(ChoicePanelText.RunAgain, () => _run.ChooseRunAgain(phase));
                    AddHelp(phase);
                    break;
                default:
                    root.SetActive(false);
                    break;
            }
        }

        private void Show(string title)
        {
            titleText.text = title;
            root.SetActive(true);
        }

        private void AddShuffled(List<(string Label, Action Action)> choices)
        {
            foreach (var (label, action) in ChoiceOrder.Shuffle(choices, _random)) Add(label, action);
        }

        private void AddHelp(HypoPhase phase) => Add(ChoicePanelText.TellAdult, () => _run.PressHelp(phase));

        private void Add(string label, Action onClick)
        {
            var button = Instantiate(buttonTemplate, buttonContainer);
            button.gameObject.SetActive(true);
            button.GetComponentInChildren<Text>().text = label;
            button.onClick.AddListener(() =>
            {
                if (_run != null) onClick();
            });
            _buttons.Add(button);
        }
    }
}
