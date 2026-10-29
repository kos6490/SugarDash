using System;
using System.Collections.Generic;
using System.Linq;
using SugarDash.App;
using SugarDash.Domain.Config;
using SugarDash.Domain.Events;
using SugarDash.Domain.Hypo;
using SugarDash.Domain.Metrics;
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
        [SerializeField] private Button retryButton;

        // 러너(트랙 간식 충돌)가 생기기 전까지 쓰는 개발용 버튼. 비워 두면 쓰지 않는다. 러너 작업 때 지운다.
        [Header("개발용 (러너 전까지)")]
        [SerializeField] private Button devSnackButton;
        [SerializeField] private string devSnackId = "juice";

        private RunController _run;
        private GameFlowController _flow;

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
            retryButton.onClick.AddListener(OnRetry);
            if (devSnackButton != null) devSnackButton.onClick.AddListener(OnDevSnack);

            var subscriptions = new List<IDisposable>
            {
                bus.Subscribe<MeterPressed>(e => meterText.text = $"측정: {e.Measured:0} mg/dL"),
            };

            return new Unbinder(() =>
            {
                foreach (var subscription in subscriptions) subscription.Dispose();
                meterButton.onClick.RemoveListener(OnMeter);
                retryButton.onClick.RemoveListener(OnRetry);
                if (devSnackButton != null) devSnackButton.onClick.RemoveListener(OnDevSnack);
                _run = null;
                _flow = null;
            });
        }

        private void Update()
        {
            if (_run == null) return;

            retryButton.gameObject.SetActive(_flow.CanRetry);
            if (_flow.IsShowingResult)
            {
                // 주행이 닫혀 혈당 값이 없으므로 마지막 화면을 그대로 두고 결과만 보여 준다.
                var result = _flow.LastResult;
                statusText.text = Title(result);
                meterText.text = EventSummary(result);
                meterButton.interactable = false;
                return;
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
            if (devSnackButton != null)
                devSnackButton.interactable = _run.State == RunState.Running && !_run.IsWorldFrozen;
        }

        // 결과 화면(ResultScreenView)이 생기기 전까지 HUD 글자 칸에 결과를 짧게 보여 준다.

        private static string Title(RunSummary result)
        {
            if (result == null) return "";
            return result.Outcome == RunOutcome.Eliminated ? "어른이 도와주러 왔어요" : "끝까지 달렸어요!";
        }

        private static string EventSummary(RunSummary result)
        {
            if (result == null || result.Events.Count == 0) return "";
            int good = result.Events.Count(e => e.AllCorrect);
            return $"저혈당 {result.Events.Count}번 중 {good}번 모두 잘 해냈어요";
        }

        private void OnMeter() => _run?.PressMeter();

        private void OnDevSnack() => _run?.PickUpSnack(devSnackId);

        private void OnRetry()
        {
            if (_flow == null) return;

            meterText.text = "";
            _flow.Retry();
        }
    }
}
