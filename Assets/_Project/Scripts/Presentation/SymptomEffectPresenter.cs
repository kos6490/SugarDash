using System;
using SugarDash.App;
using UnityEngine;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 실제 혈당이 낮을수록 카메라 Transform을 더 흔든다(저혈당 쪽만, UI는 흔들지 않는다).
    /// 지난 프레임 오프셋을 먼저 빼고 새 오프셋을 더하므로 다른 코드가 카메라 위치를 정하든 멈추든 누적되지 않는다.
    /// </summary>
    public sealed class SymptomEffectPresenter : MonoBehaviour
    {
        public Transform cameraTarget;
        public float maxAmplitude = 0.3f;
        public float frequency = 12f;

        private RunController _run;
        private double _startGlucose;
        private double _lowGlucose;

        /// <summary>이번 프레임에 카메라에 더한 오프셋.</summary>
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

        public void LateUpdate()
        {
            double intensity = _run != null && _run.State == RunState.Running
                ? SymptomShake.Intensity(_run.Actual, _startGlucose, _lowGlucose)
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

            cameraTarget.position += offset - LastOffset;
            LastOffset = offset;
        }
    }
}
