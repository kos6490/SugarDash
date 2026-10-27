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
    /// 2~4단계 선택지와 오류 뒤 "어른에게 알리기" 대기 화면. 단계가 바뀔 때마다 버튼 템플릿을 복제해 다시 만든다.
    /// 버튼은 만들 때의 단계를 기억해 명령과 함께 넘긴다(연타로 다음 단계에 잘못 들어가지 않게).
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

        /// <summary>GameBootstrapper가 연결한다. 반환값을 Dispose하면 구독을 해제한다.</summary>
        public IDisposable Bind(RunController run, DomainEventBus bus, IReadOnlyList<SnackConfig> snacks)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _snacks = snacks ?? throw new ArgumentNullException(nameof(snacks));

            buttonTemplate.gameObject.SetActive(false);
            recheckText.text = "";
            root.SetActive(false);

            var subscriptions = new List<IDisposable>
            {
                bus.Subscribe<HypoEventTriggered>(_ => recheckText.text = ""),
                bus.Subscribe<RecheckFastForward>(e => recheckText.text = $"다시 잰 혈당: {e.GlucoseAfter:0} mg/dL"),
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
            if (phase == _shown) return;

            _shown = phase;
            Rebuild(phase);
        }

        private void Rebuild(HypoPhase phase)
        {
            foreach (var button in _buttons) Destroy(button.gameObject);
            _buttons.Clear();

            switch (phase)
            {
                case HypoPhase.Respond:
                    Show("무엇을 먹을까요?");
                    foreach (var snack in _snacks)
                    {
                        string id = snack.Id;
                        Add(id, () => _run.ChooseSnack(id));
                    }
                    Add("계속 달리기", () => _run.ChooseKeepRunning());
                    Add("먹을 게 없어요", () => _run.ChooseNoSugar());
                    AddHelp(phase);
                    break;
                case HypoPhase.Dose:
                    Show($"{_run.DoseSnackId}을(를) 얼마나 먹을까요?");
                    Add("조금", () => _run.ChooseDose(DoseLevel.Under));
                    Add("알맞게", () => _run.ChooseDose(DoseLevel.Proper));
                    Add("많이", () => _run.ChooseDose(DoseLevel.Over));
                    AddHelp(phase);
                    break;
                case HypoPhase.Remeasure:
                    Show("이제 무엇을 할까요?");
                    Add("다시 재기", () => _run.ChooseRecheck());
                    Add("다시 달리기", () => _run.ChooseRunAgain(phase));
                    AddHelp(phase);
                    break;
                case HypoPhase.Notify:
                    Show("이제 무엇을 할까요?");
                    Add("다시 달리기", () => _run.ChooseRunAgain(phase));
                    AddHelp(phase);
                    break;
                case HypoPhase.GuardianPrompt:
                    Show("도움이 필요해요!");
                    AddHelp(phase);
                    Add("다시 달리기", () => _run.ChooseRunAgain(phase));
                    break;
                case HypoPhase.ResumeCountdown:
                    Show("곧 다시 달려요");
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

        private void AddHelp(HypoPhase phase) => Add("어른에게 알리기", () => _run.PressHelp(phase));

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
