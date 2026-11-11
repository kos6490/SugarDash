using System;
using SugarDash.App;
using UnityEngine;

namespace SugarDash.Presentation.Character
{
    /// <summary>
    /// 플레이어 상태를 캐릭터 애니메이터 파라미터로 전달한다. 외형 프리팹 루트에 붙인다.
    /// PlayerController가 외형을 만든 직후 Init을 부른다. 상태를 읽기만 하고 바꾸지 않는다.
    /// </summary>
    // [빈 틀] 담당: 캐릭터
    public sealed class CharacterAnimatorView : MonoBehaviour
    {
        [Tooltip("비워 두면 자식에서 찾는다.")]
        [SerializeField] private Animator animator;

        private static readonly int IsRunningId = Animator.StringToHash("IsRunning");
        private static readonly int IsAirborneId = Animator.StringToHash("IsAirborne");
        private static readonly int IsSlidingId = Animator.StringToHash("IsSliding");

        private RunController _run;
        private PlayerController _player;

        public void Init(RunController run, PlayerController player)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _player = player ?? throw new ArgumentNullException(nameof(player));
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        private void LateUpdate()
        {
            if (_run == null || animator == null) return;

            // TODO(캐릭터): 옆 이동·지친 상태 파라미터가 생기면 여기서 전달한다.
            bool running = _run.State == RunState.Running;
            bool frozen = _run.IsWorldFrozen;
            bool airborne = running && _player.IsAirborne;
            bool sliding = running && _player.IsSliding;

            // 대처 패널 등으로 세계가 멈추면 땅에서는 Idle, 공중이거나 슬라이드 중이면 그 자세로 멈춘다.
            animator.SetBool(IsRunningId, running && !frozen);
            animator.SetBool(IsAirborneId, airborne);
            animator.SetBool(IsSlidingId, sliding);
            animator.speed = frozen && (airborne || sliding) ? 0f : 1f;
        }
    }
}
