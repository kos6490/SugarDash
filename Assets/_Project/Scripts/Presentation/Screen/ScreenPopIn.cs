using UnityEngine;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 켜질 때 흐릿하고 조금 작은 상태에서 또렷해지며 나타나게 한다. 말풍선·메뉴·설명 창 오브젝트에 붙인다.
    /// 일시정지 중에도 움직이도록 실제 시간(unscaledDeltaTime)으로 돈다. 연출만이고 판정·보이고 숨기기를 바꾸지 않는다.
    /// </summary>
    // [영역 파일] 담당: 화면
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ScreenPopIn : MonoBehaviour
    {
        [Tooltip("나타나는 데 걸리는 시간(실제 초).")]
        [SerializeField] private float durationSec = 0.2f;
        [Tooltip("처음 크기 배율. 1이면 커지지 않고 또렷해지기만 한다.")]
        [SerializeField] private float startScale = 0.9f;

        private CanvasGroup _group;
        private float _elapsed;

        /// <summary>지난 시간에 따른 투명도(0~1)와 크기 배율. 시간이 0 이하이면 바로 끝 상태다.</summary>
        public static (float Alpha, float Scale) Progress(float elapsed, float duration, float startScale)
        {
            float t = duration > 0 ? Mathf.Clamp01(elapsed / duration) : 1f;
            // 끝에서 천천히 멈추게(ease-out).
            float eased = 1f - (1f - t) * (1f - t);
            return (eased, Mathf.Lerp(startScale, 1f, eased));
        }

        private void OnEnable()
        {
            if (_group == null) _group = GetComponent<CanvasGroup>();
            _elapsed = 0;
            Apply();
        }

        private void OnDisable()
        {
            _elapsed = durationSec;
            Apply();
        }

        private void Update()
        {
            if (_elapsed >= durationSec) return;
            _elapsed += Time.unscaledDeltaTime;
            Apply();
        }

        private void Apply()
        {
            var (alpha, scale) = Progress(_elapsed, durationSec, startScale);
            if (_group != null) _group.alpha = alpha;
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
