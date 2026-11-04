using System;
using SugarDash.App;
using SugarDash.Domain.Config;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDash.Presentation
{
    /// <summary>설정 화면. Screen이 Settings일 때만 보이고, 지금 유형을 보여 주며 유형을 바꾸거나 돌아간다.</summary>
    // [빈 틀] 담당: 화면
    public sealed class SettingsView : MonoBehaviour
    {
        [Tooltip("화면 전체 묶음. 보이고 숨길 대상.")]
        [SerializeField] private GameObject root;
        [SerializeField] private Text titleText;
        [SerializeField] private Text currentTypeText;
        [SerializeField] private Button type1Button;
        [SerializeField] private Button type2Button;
        [SerializeField] private Button tutorialButton;
        [SerializeField] private Button backButton;

        private GameFlowController _flow;

        public static bool IsVisible(FlowScreen screen) => screen == FlowScreen.Settings;

        public static string CurrentLabel(TrackType? type)
        {
            if (type == TrackType.Type1) return "1형";
            if (type == TrackType.Type2) return "2형";
            return "";
        }

        /// <summary>GameBootstrapper가 연결한다. 반환값을 Dispose하면 버튼 연결을 해제한다.</summary>
        public IDisposable Bind(GameFlowController flow)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            if (titleText != null) titleText.text = "설정";
            if (type1Button != null) type1Button.onClick.AddListener(OnType1);
            else Debug.LogWarning("SettingsView: type1Button이 비어 있다. 씬에서 연결해야 한다.");
            if (type2Button != null) type2Button.onClick.AddListener(OnType2);
            else Debug.LogWarning("SettingsView: type2Button이 비어 있다. 씬에서 연결해야 한다.");
            if (tutorialButton != null)
            {
                tutorialButton.onClick.AddListener(OnTutorial);
                var label = tutorialButton.GetComponentInChildren<Text>();
                if (label != null) label.text = TutorialText.SettingsButton;
            }
            else Debug.LogWarning("SettingsView: tutorialButton이 비어 있다. 씬에서 연결해야 한다.");
            if (backButton != null) backButton.onClick.AddListener(OnBack);
            else Debug.LogWarning("SettingsView: backButton이 비어 있다. 씬에서 연결해야 한다.");
            return new Unbinder(() =>
            {
                if (type1Button != null) type1Button.onClick.RemoveListener(OnType1);
                if (type2Button != null) type2Button.onClick.RemoveListener(OnType2);
                if (tutorialButton != null) tutorialButton.onClick.RemoveListener(OnTutorial);
                if (backButton != null) backButton.onClick.RemoveListener(OnBack);
                _flow = null;
            });
        }

        private void OnType1() => _flow?.SelectType(TrackType.Type1);
        private void OnType2() => _flow?.SelectType(TrackType.Type2);
        private void OnTutorial() => _flow?.StartTutorial();
        private void OnBack() => _flow?.CloseSettings();

        private void Update()
        {
            if (_flow == null) return;
            var visible = IsVisible(_flow.Screen);
            if (root != null && root.activeSelf != visible) root.SetActive(visible);
            if (visible && currentTypeText != null) currentTypeText.text = CurrentLabel(_flow.CurrentType);
            // TODO(화면): 배치와 현재 유형 강조 표시를 꾸민다.
        }
    }
}
