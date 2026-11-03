using System;
using SugarDash.App;
using SugarDash.Domain.Director;
using UnityEngine;

namespace SugarDash.Presentation
{
    /// <summary>캐릭터 연출 상태. 신호(고혈당·저혈당 증상) > 쉬기 > 보통 순으로 정한다.</summary>
    public enum CharacterState
    {
        Normal,
        Resting,
        TiredHigh,
        TiredLow,
    }

    /// <summary>
    /// 캐릭터 상태를 연출로 바꾼다. 상태는 RunController의 쉬기 여부와 활성 신호로만 정한다.
    /// 혈당을 읽거나 판정하지 않는다. 지친 연출은 연출만이며 달리기 속도·충돌·거리를 바꾸지 않는다.
    /// </summary>
    // [빈 틀] 담당: 캐릭터
    public sealed class CharacterStateView : MonoBehaviour
    {
        [Tooltip("켜면 주행과 상관없이 아래 샌드박스 상태를 보여 준다(연습용 씬).")]
        [SerializeField] private bool sandboxMode;
        [SerializeField] private CharacterState sandboxState;

        private RunController _run;
        private CharacterState? _applied;

        /// <summary>신호 > 쉬기 > 보통 순서로 상태를 정한다.</summary>
        public static CharacterState StateOf(bool isResting, NoticeKind? signal)
        {
            if (signal == NoticeKind.High) return CharacterState.TiredHigh;
            if (signal == NoticeKind.Low) return CharacterState.TiredLow;
            return isResting ? CharacterState.Resting : CharacterState.Normal;
        }

        /// <summary>GameBootstrapper가 연결한다. 반환값을 Dispose하면 연결을 해제한다.</summary>
        public IDisposable Bind(RunController run)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _applied = null;
            return new Unbinder(() => { _run = null; });
        }

        private void Update()
        {
            CharacterState state;
            if (sandboxMode) state = sandboxState;
            else if (_run != null) state = StateOf(_run.IsResting, _run.ActiveSignal);
            else return;

            if (_applied == state) return;
            _applied = state;
            Apply(state);
        }

        private void Apply(CharacterState state)
        {
            // TODO(캐릭터): 상태별 애니메이터 파라미터·표정·자세를 연결한다.
            // 체형(몸 크기·비율)은 바꾸지 않는다. 표정, 걸음 속도 연출, 자세만 바꾼다.
        }
    }
}
