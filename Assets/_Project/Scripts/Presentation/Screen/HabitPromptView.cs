using System;
using SugarDash.App;
using SugarDash.Domain.Config;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 1형 혈당 재기 습관 순간의 말풍선(측정기 옆). 떠 있는 습관 순간(RunController.ActiveHabitMoment)만 보고 보이고 숨긴다.
    /// 혈당을 읽거나 판정하지 않고 세계를 멈추지 않는다. 측정은 지금 측정기 버튼으로 한다.
    /// </summary>
    // [빈 틀] 담당: 화면
    public sealed class HabitPromptView : MonoBehaviour
    {
        [Tooltip("말풍선 전체(이 오브젝트의 자식). 습관 순간이 없으면 끈다.")]
        [SerializeField] private GameObject root;
        [SerializeField] private Text promptText;

        private RunController _run;

        /// <summary>GameBootstrapper가 연결한다. 반환값을 Dispose하면 연결을 해제한다.</summary>
        public IDisposable Bind(RunController run)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            Show(null);
            return new Unbinder(() =>
            {
                Show(null);
                _run = null;
            });
        }

        private void Update()
        {
            if (_run == null) return;
            Show(_run.ActiveHabitMoment);
            // TODO(화면): 측정기 쪽을 가리키는 말풍선 그림, 나타나고 사라지는 연출을 넣는다.
        }

        private void Show(HabitMoment? moment)
        {
            if (root != null) root.SetActive(moment.HasValue);
            if (promptText != null) promptText.text = HabitPromptText.For(moment);
        }
    }
}
