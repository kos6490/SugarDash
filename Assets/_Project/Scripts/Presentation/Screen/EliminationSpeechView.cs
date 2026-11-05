using System;
using SugarDash.App;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 탈락 장면에서 어른 옆에 뜨는 말풍선 한 줄. GameFlowController가 탈락 장면을 보여 주는 동안만 보이고,
    /// 문구는 탈락 이유(EliminatedBy)로 EliminationText가 정한다. 판정하지 않고 장면 길이를 바꾸지 않는다.
    /// </summary>
    // [빈 틀] 담당: 화면
    public sealed class EliminationSpeechView : MonoBehaviour
    {
        [Tooltip("말풍선 전체(이 오브젝트의 자식). 탈락 장면이 아니면 끈다.")]
        [SerializeField] private GameObject root;
        [SerializeField] private Text speechText;

        private GameFlowController _flow;

        /// <summary>GameBootstrapper가 연결한다. 반환값을 Dispose하면 연결을 해제한다.</summary>
        public IDisposable Bind(GameFlowController flow)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            Show(false);
            return new Unbinder(() =>
            {
                Show(false);
                _flow = null;
            });
        }

        private void Update()
        {
            if (_flow == null) return;
            bool visible = _flow.IsShowingElimination;
            Show(visible);
            if (visible && speechText != null) speechText.text = EliminationText.For(_flow.EliminatedBy);
            // TODO(화면): 말풍선 그림, 어른 머리 위를 가리키는 꼬리, 나타나는 연출을 넣는다.
        }

        private void Show(bool visible)
        {
            if (root != null && root.activeSelf != visible) root.SetActive(visible);
        }
    }
}
