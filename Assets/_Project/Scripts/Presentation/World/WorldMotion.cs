using UnityEngine;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 월드 연출이 함께 쓰는 움직임 계산(위아래 흔들림, 회전, 깜빡임). 시간을 받아 값만 돌려주는 순수 함수다.
    /// TrackItemIdle(음식·쉬는 곳)과 TutorialLaneHighlightView(레인 띠)가 같이 쓴다.
    /// </summary>
    // [영역 파일] 담당: 월드
    public static class WorldMotion
    {
        /// <summary>위아래 흔들림 높이. period초마다 한 번 오르내리고, 폭은 ±amplitude다. period ≤ 0이면 0.</summary>
        public static float Bob(float time, float amplitude, float period)
        {
            if (period <= 0f) return 0f;
            return Mathf.Sin(time / period * Mathf.PI * 2f) * amplitude;
        }

        /// <summary>회전 각도(도). 초당 degreesPerSec만큼 돌고 0~360 사이로 돌려준다.</summary>
        public static float SpinAngle(float time, float degreesPerSec) => Mathf.Repeat(time * degreesPerSec, 360f);

        /// <summary>깜빡임 투명도. period초마다 min → max → min으로 부드럽게 오간다. period ≤ 0이면 max.</summary>
        public static float Pulse(float time, float min, float max, float period)
        {
            if (period <= 0f) return max;
            float wave = (1f - Mathf.Cos(time / period * Mathf.PI * 2f)) * 0.5f;
            return Mathf.Lerp(min, max, wave);
        }

        /// <summary>색의 투명도만 바꾼 사본.</summary>
        public static Color WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);
    }
}
