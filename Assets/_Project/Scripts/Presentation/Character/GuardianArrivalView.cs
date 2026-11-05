using System;
using SugarDash.App;
using SugarDash.Presentation.Runner;
using UnityEngine;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 탈락 장면의 어른 캐릭터. 탈락 장면이 시작되면 시작 위치에 나타나 주인공 옆까지 달려와 서고, 장면이 끝나면 사라진다.
    /// 위치는 주인공 모습의 레인(x)·앞뒤(z)와 바닥 높이(y = 0) 기준이다. 프리팹은 발밑이 기준점이어야 한다.
    /// 프리팹이 없으면 기본 도형(캡슐)을 쓴다. 연출만이며 장면 길이·판정을 바꾸지 않는다.
    /// </summary>
    // [빈 틀] 담당: 캐릭터
    public sealed class GuardianArrivalView : MonoBehaviour
    {
        [Tooltip("어른 캐릭터 프리팹. 비어 있으면 기본 도형을 쓴다.")]
        [SerializeField] private GameObject guardianPrefab;
        [Tooltip("나타나는 위치(주인공 기준). 옆 레인 앞쪽.")]
        [SerializeField] private Vector3 startOffset = new Vector3(3f, 0f, 8f);
        [Tooltip("멈춰 서는 위치(주인공 기준). 바로 옆.")]
        [SerializeField] private Vector3 standOffset = new Vector3(1.2f, 0f, 0f);
        [Tooltip("시작 위치에서 옆까지 오는 시간(초). 탈락 장면 길이(Bootstrapper)보다 짧게 둔다.")]
        [SerializeField] private float arriveSec = 1f;

        private GameFlowController _flow;
        private Transform _player;
        private Transform _guardian;
        private float _elapsed;

        /// <summary>달려온 정도(0~1). 0초면 바로 옆에 선다.</summary>
        public static float Progress(float elapsed, float arriveSec) =>
            arriveSec <= 0f ? 1f : Mathf.Clamp01(elapsed / arriveSec);

        /// <summary>GameBootstrapper가 연결한다. 반환값을 Dispose하면 연결을 해제한다.</summary>
        /// <param name="playerView">레인을 옮겨 다니는 주인공 모습(PlayerController.View).</param>
        public IDisposable Bind(GameFlowController flow, Transform playerView)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            _player = playerView;
            if (_player == null) Debug.LogWarning("GuardianArrivalView: 주인공 모습이 없다. 어른이 나오지 않는다.");
            return new Unbinder(() =>
            {
                Hide();
                _flow = null;
                _player = null;
            });
        }

        private void Update()
        {
            if (_flow == null || _player == null) return;
            if (!_flow.IsShowingElimination)
            {
                Hide();
                return;
            }

            if (_guardian == null)
            {
                _guardian = guardianPrefab != null
                    ? Instantiate(guardianPrefab, transform).transform
                    : PrimitiveViews.CreateGuardian(transform);
                _elapsed = 0f;
            }
            else
            {
                _elapsed += Time.deltaTime;
            }

            var basePos = new Vector3(_player.position.x, 0f, _player.position.z);
            float t = Progress(_elapsed, arriveSec);
            _guardian.position = Vector3.Lerp(basePos + startOffset, basePos + standOffset, t);
            _guardian.LookAt(new Vector3(basePos.x, _guardian.position.y, basePos.z));
            // TODO(캐릭터): t < 1이면 달리기, 다 오면 서서 말하기 애니메이션을 재생한다.
        }

        private void Hide()
        {
            if (_guardian == null) return;
            Destroy(_guardian.gameObject);
            _guardian = null;
        }
    }
}
