using System;
using SugarDash.App;
using SugarDash.Presentation.Runner;
using UnityEngine;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 레인 이동, 점프, 슬라이드, 장애물·간식 충돌 판정. 물리 엔진 대신 계산으로 판정한다.
    /// 세계 시간(worldDt)으로만 움직이고, 충돌 결과는 RunController에 명령으로 전달한다(혈당을 직접 바꾸지 않는다).
    /// 장애물에 부딪히면 잠깐 느려질 뿐 혈당과 탈락에는 영향이 없다.
    /// </summary>
    public sealed class PlayerController : MonoBehaviour
    {
        public enum Outcome
        {
            /// <summary>다른 레인이거나 피했다.</summary>
            Passed,
            /// <summary>간식을 먹었다.</summary>
            Collected,
            /// <summary>장애물에 부딪혔다.</summary>
            Hit,
        }

        [SerializeField] private float laneWidth = 2.5f;
        [Tooltip("옆 레인으로 옮기는 데 걸리는 시간(초)")]
        [SerializeField] private float laneChangeSec = 0.15f;
        [SerializeField] private float jumpHeight = 2f;
        [SerializeField] private float jumpSec = 0.7f;
        [SerializeField] private float slideSec = 0.7f;
        [Tooltip("달리기 속도(초당 거리)")]
        [SerializeField] private float runSpeed = 10f;
        [Tooltip("장애물에 부딪힌 뒤 느려지는 시간(초)")]
        [SerializeField] private float stumbleSec = 1f;
        [Tooltip("부딪힌 뒤 속도 배율")]
        [SerializeField] private float stumbleSpeedFactor = 0.5f;
        [Tooltip("캐릭터 뒤를 따라가는 카메라. 비워 두면 카메라를 움직이지 않는다.")]
        [SerializeField] private Transform cameraRig;
        [SerializeField] private Vector3 cameraOffset = new Vector3(0, 4.5f, -7f);

        private RunController _run;
        private Transform _view;
        private int _targetLane;
        private float _x;
        private float _jumpLeft;
        private float _slideLeft;
        private float _stumbleLeft;

        public float LaneWidth => laneWidth;
        public float LaneChangeSec => laneChangeSec;
        public float JumpSec => jumpSec;
        public float SlideSec => slideSec;
        public float RunSpeed => runSpeed;
        public float StumbleSec => stumbleSec;
        public float StumbleSpeedFactor => stumbleSpeedFactor;

        /// <summary>목표 레인. -1(왼쪽), 0, 1(오른쪽).</summary>
        public int TargetLane => _targetLane;
        /// <summary>지금 위치에서 가장 가까운 레인. 충돌 판정에 쓴다.</summary>
        public int CurrentLane => Mathf.Clamp(Mathf.RoundToInt(_x / laneWidth), -1, 1);
        public float X => _x;
        public bool IsAirborne => _jumpLeft > 0;
        public bool IsSliding => _slideLeft > 0;
        public bool IsStumbling => _stumbleLeft > 0;
        /// <summary>이번 프레임의 달리기 속도. 트랙이 이 속도로 다가온다.</summary>
        public float CurrentSpeed => IsStumbling ? runSpeed * stumbleSpeedFactor : runSpeed;

        /// <param name="createView">false면 화면 없이 상태만 계산한다(테스트용).</param>
        public void Init(RunController run, bool createView)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            if (createView && _view == null) _view = PrimitiveViews.CreatePlayer(transform);
            ResetRun();
        }

        /// <summary>새 주행 시작 시 가운데 레인, 서 있는 상태로 되돌린다.</summary>
        public void ResetRun()
        {
            _targetLane = 0;
            _x = 0;
            _jumpLeft = 0;
            _slideLeft = 0;
            _stumbleLeft = 0;
            UpdateView();
        }

        public void MoveLeft() => _targetLane = Math.Max(-1, _targetLane - 1);

        public void MoveRight() => _targetLane = Math.Min(1, _targetLane + 1);

        /// <summary>슬라이드 중이면 슬라이드를 끝내고 뛴다. 이미 공중이면 무시한다.</summary>
        public void Jump()
        {
            if (IsAirborne) return;
            _slideLeft = 0;
            _jumpLeft = jumpSec;
        }

        /// <summary>공중이면 바로 내려와 슬라이드한다.</summary>
        public void Slide()
        {
            _jumpLeft = 0;
            _slideLeft = slideSec;
        }

        /// <summary>GameBootstrapper가 RunController.Tick 직후 worldDt로 부른다. 0이면 아무것도 변하지 않는다.</summary>
        public void Advance(double worldDt)
        {
            float dt = (float)worldDt;
            if (dt <= 0) return;

            float targetX = _targetLane * laneWidth;
            float step = laneWidth / laneChangeSec * dt;
            _x = Mathf.MoveTowards(_x, targetX, step);

            _jumpLeft = Mathf.Max(0, _jumpLeft - dt);
            _slideLeft = Mathf.Max(0, _slideLeft - dt);
            _stumbleLeft = Mathf.Max(0, _stumbleLeft - dt);
            UpdateView();
        }

        /// <summary>캐릭터 위치(z = 0)를 지나간 물체를 판정한다.</summary>
        public Outcome Resolve(TrackItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (item.Kind == TrackItemKind.Marker || item.Lane != CurrentLane) return Outcome.Passed;

            switch (item.Kind)
            {
                case TrackItemKind.Snack:
                    _run.PickUpSnack(item.SnackId);
                    return Outcome.Collected;
                case TrackItemKind.LowObstacle:
                    return IsAirborne ? Outcome.Passed : Stumble();
                case TrackItemKind.HighObstacle:
                    return IsSliding ? Outcome.Passed : Stumble();
                default:
                    return Stumble();
            }
        }

        private Outcome Stumble()
        {
            _stumbleLeft = stumbleSec;
            return Outcome.Hit;
        }

        private void UpdateView()
        {
            if (_view != null)
            {
                // 점프는 포물선, 슬라이드는 납작하게 표현한다.
                float y = 0;
                if (IsAirborne)
                {
                    float p = 1 - _jumpLeft / jumpSec;
                    y = 4 * jumpHeight * p * (1 - p);
                }
                bool sliding = IsSliding;
                _view.localScale = new Vector3(0.8f, sliding ? 0.45f : 1f, 0.8f);
                _view.localPosition = new Vector3(_x, y + (sliding ? 0.45f : 1f), 0);
            }

            if (cameraRig != null)
            {
                cameraRig.position = new Vector3(_x * 0.5f, 0, 0) + cameraOffset;
                cameraRig.LookAt(new Vector3(_x * 0.5f, 1f, 8f));
            }
        }
    }
}
