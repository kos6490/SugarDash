using System;
using SugarDash.App;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 주행 결과 화면. GameFlowController가 결과를 보여 주는 동안 한 번 그리고 나타나며, 끝나면 숨는다.
    /// 문구는 ResultScreenText가 만든다.
    /// </summary>
    public sealed class ResultScreenView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text titleText;
        [SerializeField] private Text summaryText;
        [SerializeField] private Button retryButton;

        private GameFlowController _flow;
        private bool _shown;

        /// <summary>GameBootstrapper가 연결한다. 반환값을 Dispose하면 버튼 연결을 해제한다.</summary>
        public IDisposable Bind(GameFlowController flow)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));

            root.SetActive(false);
            retryButton.onClick.AddListener(OnRetry);

            return new Unbinder(() =>
            {
                retryButton.onClick.RemoveListener(OnRetry);
                _flow = null;
            });
        }

        private void Update()
        {
            if (_flow == null) return;
            if (_flow.IsShowingResult == _shown) return;

            _shown = _flow.IsShowingResult;
            if (_shown) Draw();
            else root.SetActive(false);
        }

        private void Draw()
        {
            var result = _flow.LastResult;
            titleText.text = ResultScreenText.Title(result);
            summaryText.text = ResultScreenText.Summary(result);
            root.SetActive(true);
        }

        private void OnRetry() => _flow?.Retry();
    }
}
