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
    /// 표시 혈당바, 게이지 경고, 측정기 버튼. 혈당바는 표시 혈당(지연된 값)만 보여 주고,
    /// 측정기를 누르면 그때의 실제 혈당을 보여 준다.
    /// </summary>
    public sealed class HudPresenter : MonoBehaviour
    {
        [SerializeField] private Slider glucoseBar;
        [SerializeField] private Text glucoseText;
        [SerializeField] private Text warningText;
        [SerializeField] private Button meterButton;
        [SerializeField] private Text meterText;
        [SerializeField] private Text statusText;

        private RunController _run;
        private GameFlowController _flow;
        private int _shownRunCount;

        /// <summary>GameBootstrapper가 연결한다. 반환값을 Dispose하면 구독과 버튼 연결을 해제한다.</summary>
        public IDisposable Bind(RunController run, GameFlowController flow, DomainEventBus bus,
            GlucoseLevelConfig levels)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));

            glucoseBar.minValue = (float)levels.Min;
            glucoseBar.maxValue = (float)levels.Max;
            glucoseBar.interactable = false;
            meterText.text = "";
            meterButton.onClick.AddListener(OnMeter);

            var subscriptions = new List<IDisposable>
            {
                bus.Subscribe<MeterPressed>(e => meterText.text = $"측정: {e.Measured:0} mg/dL"),
            };

            return new Unbinder(() =>
            {
                foreach (var subscription in subscriptions) subscription.Dispose();
                meterButton.onClick.RemoveListener(OnMeter);
                _run = null;
                _flow = null;
            });
        }

        private void Update()
        {
            if (_run == null) return;

            if (_flow.IsShowingResult)
            {
                // 주행이 닫혀 혈당 값이 없으므로 마지막 화면을 그대로 둔다. 결과는 ResultScreenView가 보여 준다.
                meterButton.interactable = false;
                return;
            }

            if (_run.RunCount != _shownRunCount)
            {
                _shownRunCount = _run.RunCount;
                meterText.text = "";
            }

            double displayed = _run.Displayed;
            glucoseBar.value = (float)displayed;
            glucoseText.text = $"{displayed:0} mg/dL";
            warningText.enabled = _run.IsGaugeWarningOn;

            // 2~4단계와 알리기 대기 중에는 패널이 화면을 맡는다.
            var phase = _run.Phase;
            meterButton.interactable = _run.State == RunState.Running &&
                                       (phase == HypoPhase.Idle || phase == HypoPhase.Recognize ||
                                        phase == HypoPhase.Grace);

            statusText.text = _run.State == RunState.Paused ? "잠시 멈춤" : "";
        }

        private void OnMeter() => _run?.PressMeter();
    }
}
