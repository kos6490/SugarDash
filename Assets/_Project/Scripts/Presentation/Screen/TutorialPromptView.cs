using System;
using SugarDash.App;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 튜토리얼 설명 창과 "다시 해봐요" 문구. TutorialController의 단계(Stage)만 보고 보이고 숨긴다.
    /// 판정과 시계는 건드리지 않고, 화면 터치와 "건너뛰기"는 GameFlowController에 명령으로 넘긴다.
    /// </summary>
    // [빈 틀] 담당: 화면
    public sealed class TutorialPromptView : MonoBehaviour
    {
        [Tooltip("설명 창 묶음. 화면 전체를 덮어 아래 패널·측정기 터치를 막는다. Root에 화면 전체 버튼(tapArea)을 붙인다.")]
        [SerializeField] private GameObject root;
        [SerializeField] private Text messageText;
        [Tooltip("\"다시 해봐요\" 문구. 설명 창과 따로 보이고 숨는다.")]
        [SerializeField] private Text retryText;
        [Tooltip("그림 자리. 안내마다 그림을 넣을 때 쓴다.")]
        [SerializeField] private Image image;
        [Tooltip("Root에 붙는 화면 전체 버튼. 화면 아무 곳이나 눌러 넘어간다.")]
        [SerializeField] private Button tapArea;
        [Tooltip("\"화면을 눌러요\" 작은 안내 글자. 박스 아래쪽에 둔다.")]
        [SerializeField] private Text hintText;
        [Tooltip("설명 창이 보인 뒤 이 시간(초)이 지나기 전의 터치는 무시한다.")]
        [SerializeField] private float tapDelaySec = 0.5f;
        [SerializeField] private Button skipButton;

        private GameFlowController _flow;
        // 설명 창이 보이기 시작한 시각(unscaled). 보이지 않으면 음수.
        private float _shownAt = -1f;

        public static bool IsPromptVisible(TutorialStage stage) => stage == TutorialStage.Prompt;
        public static bool IsRetryVisible(TutorialStage stage) => stage == TutorialStage.Retry;
        public static bool AcceptsTap(float shownFor, float delay) => shownFor >= delay;

        /// <summary>
        /// 설명 창이 보이기 시작한 시각을 갱신한다. 보이면 처음 보인 시각을 지키고(없으면 now), 숨으면 −1.
        /// 설명 창이 숨었다가(직접 해 봄·"다시 해봐요") 다시 뜨면 지연이 처음부터 다시 시작된다.
        /// </summary>
        public static float NextShownAt(bool promptVisible, float shownAt, float now)
        {
            if (!promptVisible) return -1f;
            return shownAt < 0f ? now : shownAt;
        }

        /// <summary>GameBootstrapper가 연결한다. 반환값을 Dispose하면 버튼 연결을 해제한다.</summary>
        public IDisposable Bind(GameFlowController flow)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            if (root == null) Debug.LogWarning("TutorialPromptView: root가 비어 있다. 씬에서 연결해야 한다.");
            if (messageText == null) Debug.LogWarning("TutorialPromptView: messageText가 비어 있다. 씬에서 연결해야 한다.");
            if (retryText == null) Debug.LogWarning("TutorialPromptView: retryText가 비어 있다. 씬에서 연결해야 한다.");
            if (tapArea != null) tapArea.onClick.AddListener(OnTap);
            else Debug.LogWarning("TutorialPromptView: tapArea가 비어 있다. 씬에서 연결해야 한다.");
            if (hintText != null) hintText.text = TutorialText.TapHint;
            if (skipButton != null)
            {
                skipButton.onClick.AddListener(OnSkip);
                SetLabel(skipButton, TutorialText.Skip);
            }
            else Debug.LogWarning("TutorialPromptView: skipButton이 비어 있다. 씬에서 연결해야 한다.");
            if (retryText != null) retryText.text = TutorialText.TryAgain;
            return new Unbinder(() =>
            {
                if (tapArea != null) tapArea.onClick.RemoveListener(OnTap);
                if (skipButton != null) skipButton.onClick.RemoveListener(OnSkip);
                if (root != null) root.SetActive(false);
                if (retryText != null) retryText.gameObject.SetActive(false);
                _flow = null;
                _shownAt = -1f;
            });
        }

        private static void SetLabel(Button button, string label)
        {
            var text = button.GetComponentInChildren<Text>();
            if (text != null) text.text = label;
        }

        private void OnTap()
        {
            if (!TapReady()) return;
            _flow.Tutorial.Acknowledge();
        }

        // 설명 창을 넘기려던 터치가 막 뜬 건너뛰기에 닿지 않게 같은 지연을 둔다.
        private void OnSkip()
        {
            if (!TapReady()) return;
            _flow.SkipTutorial();
        }

        private bool TapReady() =>
            _flow != null && _shownAt >= 0f && AcceptsTap(Time.unscaledTime - _shownAt, tapDelaySec);

        private void Update()
        {
            if (_flow == null) return;
            var tutorial = _flow.Tutorial;
            var stage = tutorial.Stage;

            // root는 화면 전체를 덮어 아래 패널·측정기 터치를 막는다.
            bool prompt = IsPromptVisible(stage);
            _shownAt = NextShownAt(prompt, _shownAt, Time.unscaledTime);
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
