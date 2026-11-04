using System;
using SugarDash.App;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 튜토리얼 설명 창과 "다시 해봐요" 문구. TutorialController의 단계(Stage)만 보고 보이고 숨긴다.
    /// 판정과 시계는 건드리지 않고, "알겠어요"와 "건너뛰기"는 GameFlowController에 명령으로 넘긴다.
    /// </summary>
    // [빈 틀] 담당: 화면
    public sealed class TutorialPromptView : MonoBehaviour
    {
        [Tooltip("설명 창 묶음. 화면 전체를 덮어 아래 패널·측정기 터치를 막는다.")]
        [SerializeField] private GameObject root;
        [SerializeField] private Text messageText;
        [Tooltip("\"다시 해봐요\" 문구. 설명 창과 따로 보이고 숨는다.")]
        [SerializeField] private Text retryText;
        [Tooltip("그림 자리. 안내마다 그림을 넣을 때 쓴다.")]
        [SerializeField] private Image image;
        [SerializeField] private Button acknowledgeButton;
        [SerializeField] private Button skipButton;

        private GameFlowController _flow;

        public static bool IsPromptVisible(TutorialStage stage) => stage == TutorialStage.Prompt;
        public static bool IsRetryVisible(TutorialStage stage) => stage == TutorialStage.Retry;

        /// <summary>GameBootstrapper가 연결한다. 반환값을 Dispose하면 버튼 연결을 해제한다.</summary>
        public IDisposable Bind(GameFlowController flow)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            if (root == null) Debug.LogWarning("TutorialPromptView: root가 비어 있다. 씬에서 연결해야 한다.");
            if (messageText == null) Debug.LogWarning("TutorialPromptView: messageText가 비어 있다. 씬에서 연결해야 한다.");
            if (retryText == null) Debug.LogWarning("TutorialPromptView: retryText가 비어 있다. 씬에서 연결해야 한다.");
            if (acknowledgeButton != null)
            {
                acknowledgeButton.onClick.AddListener(OnAcknowledge);
                SetLabel(acknowledgeButton, TutorialText.Acknowledge);
            }
            else Debug.LogWarning("TutorialPromptView: acknowledgeButton이 비어 있다. 씬에서 연결해야 한다.");
            if (skipButton != null)
            {
                skipButton.onClick.AddListener(OnSkip);
                SetLabel(skipButton, TutorialText.Skip);
            }
            else Debug.LogWarning("TutorialPromptView: skipButton이 비어 있다. 씬에서 연결해야 한다.");
            if (retryText != null) retryText.text = TutorialText.TryAgain;
            return new Unbinder(() =>
            {
                if (acknowledgeButton != null) acknowledgeButton.onClick.RemoveListener(OnAcknowledge);
                if (skipButton != null) skipButton.onClick.RemoveListener(OnSkip);
                if (root != null) root.SetActive(false);
                if (retryText != null) retryText.gameObject.SetActive(false);
                _flow = null;
            });
        }

        private static void SetLabel(Button button, string label)
        {
            var text = button.GetComponentInChildren<Text>();
            if (text != null) text.text = label;
        }

        private void OnAcknowledge() => _flow?.Tutorial.Acknowledge();
        private void OnSkip() => _flow?.SkipTutorial();

        private void Update()
        {
            if (_flow == null) return;
            var tutorial = _flow.Tutorial;
            var stage = tutorial.Stage;

            // root는 화면 전체를 덮어 아래 패널·측정기 터치를 막는다.
            bool prompt = IsPromptVisible(stage);
            if (root != null && root.activeSelf != prompt) root.SetActive(prompt);
            if (prompt && messageText != null && tutorial.Step != null)
                messageText.text = TutorialText.For(tutorial.Step.Value, tutorial.Type);
            if (skipButton != null) skipButton.gameObject.SetActive(prompt && tutorial.CanSkip);

            bool retry = IsRetryVisible(stage);
            if (retryText != null && retryText.gameObject.activeSelf != retry) retryText.gameObject.SetActive(retry);
            // TODO(화면): 그림, 배치, 손가락 안내를 꾸민다.
        }
    }
}
