using System;
using SugarDash.App;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 화면이 바뀔 때(GameFlowController.Screen) 모든 화면 위의 덮개를 불투명하게 띄웠다가 걷어 새 화면이 서서히 드러나게 한다.
    /// 탈락 장면으로 넘어갈 때는 걸지 않는다(게임 위에 뜨는 장면이다). 연출만이고 터치를 막지 않으며 판정·흐름을 바꾸지 않는다.
    /// 일시정지 중에도 움직이도록 실제 시간(unscaledDeltaTime)으로 돈다. Canvas의 맨 마지막 자식으로 둔다.
    /// </summary>
    public sealed class ScreenFadeView : MonoBehaviour
    {
        [Tooltip("화면 전체를 덮는 판. Raycast Target은 끈다.")]
        [SerializeField] private Image overlay;
        [Tooltip("덮개가 걷히는 시간(실제 초).")]
        [SerializeField] private float durationSec = 0.25f;

        private GameFlowController _flow;
        private FlowScreen? _last;
        private float _elapsed;

        /// <summary>이 화면 이동에 덮개를 걸지. 처음 화면(from 없음)에도 건다.</summary>
        public static bool ShouldFade(FlowScreen? from, FlowScreen to) =>
            from != to && to != FlowScreen.Elimination;

        /// <summary>덮개 투명도. 처음 1에서 duration 동안 0으로(끝에서 천천히). duration이 0 이하이면 0.</summary>
        public static float Alpha(float elapsed, float duration)
        {
            if (!(duration > 0)) return 0f;
            float t = Mathf.Clamp01(elapsed / duration);
            return (1f - t) * (1f - t);
        }

        /// <summary>GameBootstrapper가 연결한다. 반환값을 Dispose하면 연결을 해제하고 덮개를 걷는다.</summary>
        public IDisposable Bind(GameFlowController flow)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            _last = null;
            _elapsed = durationSec;
            SetAlpha(0f);
            if (overlay != null) overlay.raycastTarget = false;
            return new Unbinder(() =>
            {
                SetAlpha(0f);
                _flow = null;
            });
        }

        private void Update()
        {
            if (_flow == null) return;
            var screen = _flow.Screen;
            if (screen != _last)
            {
                if (ShouldFade(_last, screen)) _elapsed = 0f;
                _last = screen;
            }
            if (_elapsed >= durationSec)
            {
                SetAlpha(0f);
                return;
            }
            _elapsed += Time.unscaledDeltaTime;
            SetAlpha(Alpha(_elapsed, durationSec));
        }

        private void SetAlpha(float alpha)
        {
            if (overlay == null) return;
            var color = overlay.color;
            if (Mathf.Approximately(color.a, alpha)) return;
            color.a = alpha;
            overlay.color = color;
            overlay.enabled = alpha > 0f;
        }
    }
}
