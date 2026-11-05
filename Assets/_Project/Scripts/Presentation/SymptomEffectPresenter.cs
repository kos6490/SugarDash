using System;
using SugarDash.App;
using SugarDash.Domain.Config;
using UnityEngine;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 실제 혈당이 낮을수록 카메라 Transform을 더 흔든다(저혈당 쪽만, UI는 흔들지 않는다). 탈락 장면 동안에는 살짝만 흔든다.
    /// 지난 프레임 오프셋을 먼저 빼고 새 오프셋을 더하므로 다른 코드가 카메라 위치를 정하든 멈추든 누적되지 않는다.
    /// </summary>
    public sealed class SymptomEffectPresenter : MonoBehaviour
    {
        public Transform cameraTarget;
        public float maxAmplitude = 0.3f;
        public float frequency = 12f;
        [Tooltip("탈락 장면 동안 흔들림 배율(0~1). 끄지 않고 살짝만 이어 간다.")]
        [Range(0f, 1f)] public float eliminatedScale = 0.3f;

        private RunController _run;
        private double _startGlucose;
        private double _lowGlucose;

        private Vector3 _lastWritten;

        /// <summary>마지막 ApplyShake가 카메라 기준 위치에 더한 오프셋.</summary>
        public Vector3 LastOffset { get; private set; }

        public IDisposable Bind(RunController run, double shakeStartGlucose, double glucoseLow)
        {
            _run = run;
            _startGlucose = shakeStartGlucose;
            _lowGlucose = glucoseLow;
            return new Unbinder(() =>
            {
                ApplyShake(0, 0f);
                _run = null;
            });
        }

        private void OnDisable() => ApplyShake(0, 0f);

        public void LateUpdate()
        {
            // 2형 주행에는 화면 흔들림이 없다.
            double intensity = _run != null && _run.TrackType != TrackType.Type2
                ? SymptomShake.Intensity(_run.Actual, _startGlucose, _lowGlucose) *
                  SymptomShake.StateScale(_run.State, eliminatedScale)
                : 0;
            ApplyShake(intensity, Time.time);
        }

        public void ApplyShake(double intensity, float time)
        {
            if (cameraTarget == null) return;

            var offset = Vector3.zero;
            if (intensity > 0)
            {
                float amp = maxAmplitude * (float)intensity;
                float t = time * frequency;
                offset = new Vector3((Mathf.PerlinNoise(t, 0.31f) * 2f - 1f) * amp,
                    (Mathf.PerlinNoise(0.77f, t) * 2f - 1f) * amp, 0f);
            }

            // 다른 코드(PlayerController)가 위치를 새로 썼으면 그 값이 기준, 아니면 지난 오프셋을 뺀 값이 기준이다.
            var current = cameraTarget.position;
            var basePos = current == _lastWritten ? current - LastOffset : current;
            cameraTarget.position = basePos + offset;
            _lastWritten = cameraTarget.position;
            LastOffset = offset;
        }
    }
}
