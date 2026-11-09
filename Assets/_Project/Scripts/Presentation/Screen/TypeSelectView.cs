using System;
using SugarDash.App;
using SugarDash.Domain.Config;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDash.Presentation
{
    /// <summary>유형 선택 화면. Screen이 TypeSelect일 때만 보이고, 1형·2형 버튼으로 유형을 고른다.</summary>
    // [빈 틀] 담당: 화면
    public sealed class TypeSelectView : MonoBehaviour
    {
        [Tooltip("화면 전체 묶음. 보이고 숨길 대상.")]
        [SerializeField] private GameObject root;
        [SerializeField] private Text titleText;
        [SerializeField] private Text guideText;
        [SerializeField] private Button type1Button;
        [SerializeField] private Button type2Button;

        private GameFlowController _flow;

        public static bool IsVisible(FlowScreen screen) => screen == FlowScreen.TypeSelect;

        /// <summary>GameBootstrapper가 연결한다. 반환값을 Dispose하면 버튼 연결을 해제한다.</summary>
        public IDisposable Bind(GameFlowController flow)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            if (titleText != null) titleText.text = "어떤 당뇨가 있나요?";
            if (guideText != null) guideText.text = "보호자와 함께 골라 주세요";
            if (type1Button != null) type1Button.onClick.AddListener(OnType1);
            else Debug.LogWarning("TypeSelectView: type1Button이 비어 있다. 씬에서 연결해야 한다.");
            if (type2Button != null) type2Button.onClick.AddListener(OnType2);
            else Debug.LogWarning("TypeSelectView: type2Button이 비어 있다. 씬에서 연결해야 한다.");
            return new Unbinder(() =>
            {
                if (type1Button != null) type1Button.onClick.RemoveListener(OnType1);
                if (type2Button != null) type2Button.onClick.RemoveListener(OnType2);
                _flow = null;
            });
        }

        private void OnType1() => _flow?.SelectType(TrackType.Type1);
        private void OnType2() => _flow?.SelectType(TrackType.Type2);

        private void Update()
        {
            if (_flow == null) return;
            var visible = IsVisible(_flow.Screen);
            if (root != null && root.activeSelf != visible) root.SetActive(visible);
            // TODO(화면): 유형별 그림과 안내 음성·문구를 꾸민다.
        }
    }
}
