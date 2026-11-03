using System;
using SugarDash.Domain.Events;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 수면 전환. 하루가 넘어가 다음 날 첫 장면에 들어가면 화면을 바로 검게 했다가 밝힌다.
    /// 바로 검게 하는 까닭: 같은 순간 SceneLook이 다음 날 맵으로 바꾸므로, 서서히 어두워지면 바뀐 맵이 먼저 보인다.
    /// 연출만이다. RunController를 읽지 않고 세계·혈당을 멈추지 않는다.
    /// </summary>
    // [빈 틀] 담당: 화면
    public sealed class SleepTransitionView : MonoBehaviour
    {
        [Tooltip("검은 전체 화면 이미지(raycastTarget 끔). 비워 두면 아무것도 바꾸지 않는다.")]
        [SerializeField] private Image fade;
        [SerializeField] private float holdSec = 0.2f;
        [SerializeField] private float fadeInSec = 0.6f;

        private int _lastDayIndex;
        private bool _fading;
        private float _elapsed;

        /// <summary>하루 번호가 올랐을 때만 암전한다. 새 주행(하루 번호가 0으로 돌아감)은 암전하지 않는다.</summary>
        public static bool ShouldFade(int lastDayIndex, int dayIndex) => dayIndex > lastDayIndex;

        /// <summary>
        /// 암전 시작부터 elapsed초 뒤의 검은 화면 투명도. hold 동안 1, fadeIn 동안 1→0, 그 뒤 0.
        /// </summary>
        public static float AlphaAt(float elapsed, float hold, float fadeIn)
        {
            if (elapsed < hold) return 1f;
            if (elapsed < hold + fadeIn) return 1f - (elapsed - hold) / fadeIn;
            return 0f;
        }

        /// <summary>GameBootstrapper가 연결한다. 반환값을 Dispose하면 연결을 해제한다.</summary>
        public IDisposable Bind(DomainEventBus bus)
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            var subscription = bus.Subscribe<SceneEntered>(OnSceneEntered);
            return new Unbinder(() =>
            {
                subscription.Dispose();
                Stop();
                _lastDayIndex = 0;
            });
        }

        private void OnSceneEntered(SceneEntered e)
        {
            if (ShouldFade(_lastDayIndex, e.DayIndex))
            {
                _fading = true;
                _elapsed = 0f;
                SetAlpha(1f);
            }
            else if (e.DayIndex < _lastDayIndex)
            {
                Stop(); // 새 주행
            }
            _lastDayIndex = e.DayIndex;
        }

        private void Update()
        {
            if (!_fading) return;
            // 일시정지(timeScale 0) 중에는 암전도 멈춘다.
            _elapsed += Time.deltaTime;
            SetAlpha(AlphaAt(_elapsed, holdSec, fadeInSec));
            if (_elapsed >= holdSec + fadeInSec) Stop();
            // TODO(화면): 잠자는 그림이나 달·해 전환을 넣는다. 길이는 Inspector 값으로 맞춘다.
        }

        private void Stop()
        {
            _fading = false;
            SetAlpha(0f);
        }

        private void SetAlpha(float alpha)
        {
            if (fade != null) fade.color = new Color(0f, 0f, 0f, alpha);
        }
    }
}
