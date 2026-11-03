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

        private RunController _run;

        /// <summary>신호별 개발용 문구. 신호가 없으면 빈 문자열.</summary>
        public static string LabelOf(NoticeKind? signal)
        {
            if (signal == NoticeKind.High) return "목말라요";
            // 저혈당 쪽 표시는 의학 출처 확인 전이므로 개발용 표시만 둔다.
            if (signal == NoticeKind.Low) return "(개발용) 낮음 신호";
            return "";
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
            if (signalText != null) signalText.text = LabelOf(signal);
            // TODO(화면): High면 갈증 말풍선과 화장실 아이콘 깜빡임을 넣는다.
            // TODO(화면): Low는 의학 출처 확인 뒤에 연출을 정한다. 지금은 개발용 표시만 둔다.
        }
    }
}
