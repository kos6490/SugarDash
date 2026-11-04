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
        [Tooltip("점수 표시. 비워 두면 표시하지 않는다.")]
        [SerializeField] private Text scoreText;
        [Tooltip("재개 카운트다운 숫자(3·2·1). 비워 두면 표시하지 않는다.")]
        [SerializeField] private Text countdownText;
        [Tooltip("측정 결과 글자를 보여 주는 시간(초). 지나면 지운다.")]
        [SerializeField] private float meterTextShowSec = 3f;

        private RunController _run;
        private GameFlowController _flow;
        private int _shownRunCount;
        // 측정 결과 글자를 지울 시각(Time.time). 보여 주는 글자가 없으면 음수.
        private float _meterTextHideAt = -1;

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
                bus.Subscribe<MeterPressed>(e =>
                {
                    meterText.text = HudText.Measured(e.Measured);
                    _meterTextHideAt = Time.time + meterTextShowSec;
                }),
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
                if (countdownText != null) countdownText.text = "";
                if (scoreText != null && _flow.LastScore != null)
                    scoreText.text = HudText.Score((int)Math.Floor(_flow.LastScore.Value));
                return;
            }

            if (_run.RunCount != _shownRunCount)
            {
                _shownRunCount = _run.RunCount;
                ClearMeterText();
            }

            if (_meterTextHideAt >= 0 && Time.time >= _meterTextHideAt) ClearMeterText();

            // 2형에는 측정기·표시 혈당 지연·게이지 경고가 없다. 혈당바는 실제 혈당을 보여 주고 측정 관련 표시는 숨긴다.
            bool type2 = _run.TrackType == TrackType.Type2;
            meterButton.gameObject.SetActive(!type2);
            meterText.enabled = !type2;
            double displayed = type2 ? _run.Actual : _run.Displayed;
            glucoseBar.value = (float)displayed;
            glucoseText.text = $"{displayed:0} mg/dL";
            warningText.enabled = !type2 && _run.IsGaugeWarningOn;

            // 2~4단계와 알리기 대기 중에는 패널이 화면을 맡는다.
            var phase = _run.Phase;
            meterButton.interactable = _run.State == RunState.Running &&
                                       (phase == HypoPhase.Idle || phase == HypoPhase.Recognize ||
                                        phase == HypoPhase.Grace) &&
                                       IsMeterAllowed(_flow.Tutorial.AllowedControls);

            if (scoreText != null) scoreText.text = HudText.Score((int)Math.Floor(_run.Score));

            if (countdownText != null)
            {
                int? number = AnswerFeedbackText.CountdownNumber(_run.ResumeCountdownRemaining);
                countdownText.text = number.HasValue ? number.Value.ToString() : "";
            }

            statusText.text = _run.State == RunState.Paused ? HudText.Paused : "";
        }

        /// <summary>튜토리얼 조작 잠금에서 측정기가 허용되는지. 일반 판은 All이라 항상 허용된다.</summary>
        public static bool IsMeterAllowed(TutorialControls allowed) => (allowed & TutorialControls.Meter) != 0;

        private void OnMeter()
        {
            if (_run == null || _flow == null || !IsMeterAllowed(_flow.Tutorial.AllowedControls)) return;
            _run.PressMeter();
        }

        private void ClearMeterText()
        {
            meterText.text = "";
            _meterTextHideAt = -1;
        }
    }
}
