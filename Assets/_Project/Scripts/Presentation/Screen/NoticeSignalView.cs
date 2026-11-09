using System;
using SugarDash.App;
using SugarDash.Domain.Director;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 2형 notice 신호 표시. 활성 신호(RunController.ActiveSignal)만 보고 말풍선·아이콘을 보여 준다.
    /// 혈당을 읽거나 판정하지 않는다.
    /// </summary>
    // [빈 틀] 담당: 화면
    public sealed class NoticeSignalView : MonoBehaviour
    {
        [SerializeField] private Text signalText;
        [Tooltip("높음 신호가 흐려졌다 밝아지는 한 번의 시간(실제 초).")]
        [SerializeField] private float blinkPeriodSec = 1f;
        [Tooltip("깜빡일 때 가장 흐린 투명도.")]
        [SerializeField] private float blinkMinAlpha = 0.3f;

        private RunController _run;

        /// <summary>신호별 문구(docs/notice-evidence.md). 신호가 없으면 빈 문자열.</summary>
        public static string LabelOf(NoticeKind? signal)
        {
            if (signal == NoticeKind.High) return "목말라요";
            if (signal == NoticeKind.Low) return "몸이 떨려요";
            return "";
        }

        /// <summary>깜빡임 투명도. 주기의 처음과 끝이 1, 가운데가 minAlpha. 주기가 0 이하이면 1.</summary>
        public static float BlinkAlpha(float time, float periodSec, float minAlpha)
        {
            if (!(periodSec > 0)) return 1f;
            float phase = Mathf.Repeat(time, periodSec) / periodSec;
            float wave = 0.5f + 0.5f * Mathf.Cos(phase * 2f * Mathf.PI);
            return Mathf.Lerp(minAlpha, 1f, wave);
        }

        /// <summary>GameBootstrapper가 연결한다. 반환값을 Dispose하면 연결을 해제한다.</summary>
        public IDisposable Bind(RunController run)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            return new Unbinder(() =>
            {
                if (signalText != null) signalText.text = "";
                _run = null;
            });
        }

        private void Update()
        {
            if (_run == null) return;
            var signal = _run.ActiveSignal;
            if (signalText != null)
            {
                signalText.text = LabelOf(signal);
                // 높음 신호만 깜빡인다(화장실 아이콘 깜빡임). 낮음은 가만히 둔다.
                var color = signalText.color;
                color.a = signal == NoticeKind.High ? BlinkAlpha(Time.unscaledTime, blinkPeriodSec, blinkMinAlpha) : 1f;
                signalText.color = color;
            }
            // TODO(화면): High면 갈증 말풍선과 화장실 아이콘 그림을 넣고 같은 BlinkAlpha로 깜빡인다.
            // TODO(화면): Low면 떨리는 손 그림을 넣는다(docs/notice-evidence.md).
        }
    }
}
