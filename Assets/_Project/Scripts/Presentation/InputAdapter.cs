using System;
using SugarDash.App;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 터치·마우스 쓸기와 키보드 입력을 캐릭터 명령으로 바꾼다(Input System 전용).
    /// 짧은 탭은 쓸기로 보지 않아 화면 버튼(측정기 등)을 눌러도 캐릭터가 움직이지 않는다.
    /// 세계가 멈춘 동안(대처 패널, 일시정지, 주행 종료)의 입력은 무시한다.
    /// </summary>
    public sealed class InputAdapter : MonoBehaviour
    {
        public enum Swipe
        {
            None,
            Left,
            Right,
            Up,
            Down,
        }

        [Tooltip("쓸기로 인정하는 최소 거리(화면 높이 대비 비율)")]
        [SerializeField] private float minSwipeScreenRatio = 0.05f;

        private RunController _run;
        private PlayerController _player;
        private Vector2? _pressStart;

        public void Init(RunController run, PlayerController player)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _player = player ?? throw new ArgumentNullException(nameof(player));
        }

        /// <summary>누른 곳에서 뗀 곳까지의 이동으로 쓸기 방향을 정한다. 더 많이 움직인 축을 따른다.</summary>
        public static Swipe Classify(Vector2 delta, float minDistance)
        {
            if (delta.magnitude < minDistance) return Swipe.None;
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)) return delta.x < 0 ? Swipe.Left : Swipe.Right;
            return delta.y > 0 ? Swipe.Up : Swipe.Down;
        }

        private void Update()
        {
            if (_run == null) return;

            var swipe = ReadPointer();
            if (swipe == Swipe.None) swipe = ReadKeyboard();
            if (swipe == Swipe.None) return;
            if (_run.State != RunState.Running || _run.IsWorldFrozen) return;

            switch (swipe)
            {
                case Swipe.Left: _player.MoveLeft(); break;
                case Swipe.Right: _player.MoveRight(); break;
                case Swipe.Up: _player.Jump(); break;
                case Swipe.Down: _player.Slide(); break;
            }
        }

        private Swipe ReadPointer()
        {
            var pointer = Pointer.current;
            if (pointer == null) return Swipe.None;

            if (pointer.press.wasPressedThisFrame) _pressStart = pointer.position.ReadValue();
            if (!pointer.press.wasReleasedThisFrame || _pressStart == null) return Swipe.None;

            var delta = pointer.position.ReadValue() - _pressStart.Value;
            _pressStart = null;
            return Classify(delta, Screen.height * minSwipeScreenRatio);
        }

        private static Swipe ReadKeyboard()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return Swipe.None;

            if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) return Swipe.Left;
            if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) return Swipe.Right;
            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) return Swipe.Up;
            if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) return Swipe.Down;
            return Swipe.None;
        }
    }
}
