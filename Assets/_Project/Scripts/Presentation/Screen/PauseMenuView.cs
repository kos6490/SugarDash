using System;
using SugarDash.App;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 주행 화면 구석의 일시정지 버튼과 일시정지 메뉴(계속하기, 타이틀로).
    /// 버튼은 GameFlowController.CanPause일 때만, 메뉴는 IsPauseMenuOpen일 때만 보인다. 명령은 GameFlowController로 넘긴다.
    /// </summary>
    // [빈 틀] 담당: 화면
    public sealed class PauseMenuView : MonoBehaviour
    {
        [Tooltip("구석의 일시정지 버튼.")]
        [SerializeField] private Button pauseButton;
        [Tooltip("메뉴 전체(이 오브젝트의 자식). 화면 전체를 덮는 배경이 뒤 버튼 터치를 막아야 한다.")]
        [SerializeField] private GameObject menuRoot;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button titleButton;
        [Tooltip("메뉴 위쪽의 '잠시 멈춤' 글자. 비워 두면 글자 없이 버튼만 보인다.")]
        [SerializeField] private Text pausedText;

        private GameFlowController _flow;

        /// <summary>GameBootstrapper가 연결한다. 반환값을 Dispose하면 버튼 연결을 해제한다.</summary>
        public IDisposable Bind(GameFlowController flow)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            if (pausedText != null) pausedText.text = HudText.Paused;
            if (pauseButton != null) pauseButton.onClick.AddListener(OnPause);
            else Debug.LogWarning("PauseMenuView: pauseButton이 비어 있다. 씬에서 연결해야 한다.");
            if (continueButton != null) continueButton.onClick.AddListener(OnContinue);
            if (titleButton != null) titleButton.onClick.AddListener(OnTitle);
            Show(false, false);
            return new Unbinder(() =>
            {
                if (pauseButton != null) pauseButton.onClick.RemoveListener(OnPause);
                if (continueButton != null) continueButton.onClick.RemoveListener(OnContinue);
                if (titleButton != null) titleButton.onClick.RemoveListener(OnTitle);
                Show(false, false);
                _flow = null;
            });
        }

        private void OnPause() => _flow?.OpenPauseMenu();
        private void OnContinue() => _flow?.ContinueFromPause();
        private void OnTitle() => _flow?.QuitToTitle();

        private void Update()
        {
            if (_flow == null) return;
            Show(_flow.CanPause, _flow.IsPauseMenuOpen);
            // TODO(화면): 버튼 아이콘(일시정지 모양), 메뉴 배치, 버튼 소리를 꾸민다. 메뉴가 나타나는 연출은 MenuRoot의 ScreenPopIn이 한다.
        }

        private void Show(bool button, bool menu)
        {
            if (pauseButton != null && pauseButton.gameObject.activeSelf != button) pauseButton.gameObject.SetActive(button);
            if (menuRoot != null && menuRoot.activeSelf != menu) menuRoot.SetActive(menu);
        }
    }
}
