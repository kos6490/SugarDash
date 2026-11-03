using System;
using SugarDash.App;
using SugarDash.Domain.Config;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDash.Presentation
{
    /// <summary>타이틀 화면. Screen이 Title일 때만 보이고, 고른 유형 문구와 시작·설정 버튼을 둔다.</summary>
    // [빈 틀] 담당: 화면
    public sealed class TitleScreenView : MonoBehaviour
    {
        [Tooltip("화면 전체 묶음. 보이고 숨길 대상.")]
        [SerializeField] private GameObject root;
        [SerializeField] private Text typeText;
        [SerializeField] private Button startButton;
        [SerializeField] private Button settingsButton;

        private GameFlowController _flow;

        public static bool IsVisible(FlowScreen screen) => screen == FlowScreen.Title;

        public static string TypeLabel(TrackType? type)
        {
            if (type == TrackType.Type1) return "1형으로 달려요";
            if (type == TrackType.Type2) return "2형으로 달려요";
            return "";
        }

        /// <summary>GameBootstrapper가 연결한다. 반환값을 Dispose하면 버튼 연결을 해제한다.</summary>
        public IDisposable Bind(GameFlowController flow)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            startButton.onClick.AddListener(OnStart);
            settingsButton.onClick.AddListener(OnSettings);
            return new Unbinder(() =>
            {
                startButton.onClick.RemoveListener(OnStart);
                settingsButton.onClick.RemoveListener(OnSettings);
                _flow = null;
            });
        }

        private void OnStart() => _flow?.StartRun();
        private void OnSettings() => _flow?.OpenSettings();

        private void Update()
        {
            if (_flow == null) return;
            var visible = IsVisible(_flow.Screen);
            if (root != null && root.activeSelf != visible) root.SetActive(visible);
            if (visible && typeText != null) typeText.text = TypeLabel(_flow.CurrentType);
            // TODO(화면): 배치(그림, 글자 크기)와 버튼 소리를 꾸민다.
        }
    }
}
