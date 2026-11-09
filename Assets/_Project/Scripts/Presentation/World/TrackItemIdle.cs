using UnityEngine;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 음식·쉬는 곳 프리팹에 붙이는 제자리 연출(빙글빙글 돌기, 위아래 흔들림). 프리팹 안의 모델 오브젝트에 붙인다.
    /// 붙인 오브젝트의 로컬 회전·위치만 바꾼다. 트랙 위치, 충돌 판정, 혈당과는 관계없다.
    /// </summary>
    // [영역 파일] 담당: 월드
    public sealed class TrackItemIdle : MonoBehaviour
    {
        [Tooltip("초당 회전 각도(도). 0이면 돌지 않는다.")]
        [SerializeField] private float spinDegreesPerSec = 90f;
        [Tooltip("위아래 흔들림 폭. 0이면 흔들리지 않는다.")]
        [SerializeField] private float bobAmplitude = 0.08f;
        [Tooltip("위아래 한 번 오르내리는 데 걸리는 시간(초)")]
        [SerializeField] private float bobPeriod = 1.2f;
        [Tooltip("켜 두면 물체마다 시작 위상을 달리해 줄지어 있어도 똑같이 움직이지 않는다.")]
        [SerializeField] private bool randomPhase = true;

        private Vector3 _basePosition;
        private Quaternion _baseRotation;
        private float _phase;

        private void Awake()
        {
            _basePosition = transform.localPosition;
            _baseRotation = transform.localRotation;
            _phase = randomPhase ? Random.Range(0f, 10f) : 0f;
        }

        private void Update()
        {
            float t = Time.time + _phase;
            transform.localRotation = _baseRotation * Quaternion.Euler(0f, WorldMotion.SpinAngle(t, spinDegreesPerSec), 0f);
            transform.localPosition = _basePosition + new Vector3(0f, WorldMotion.Bob(t, bobAmplitude, bobPeriod), 0f);
        }
    }
}
