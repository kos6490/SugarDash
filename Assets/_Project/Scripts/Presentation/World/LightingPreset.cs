using System;
using UnityEngine;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 때(아침, 오후, 저녁, 밤) 하나의 조명·하늘 값. TimeOfDayLighting이 Inspector 목록으로 들고 적용한다.
    /// 연출 값만 있다. 혈당·시간 수치가 아니다.
    /// </summary>
    // [영역 파일] 담당: 월드
    [Serializable]
    public struct LightingPreset
    {
        [Tooltip("설정의 때 이름: morning, afternoon, evening, night")]
        public string key;

        [Header("해(Directional Light)")]
        public Color sunColor;
        [Min(0f)] public float sunIntensity;
        [Tooltip("해의 각도(x: 높이, y: 방향)")]
        public Vector3 sunEuler;

        [Header("주변광·하늘")]
        [Tooltip("Lighting 창 Environment Lighting Source가 Color일 때 쓰는 주변광 색")]
        public Color ambientColor;
        [Tooltip("하늘 머티리얼. 비워 두면 바꾸지 않는다. 섞이지 않고 전환 중간에 바뀐다.")]
        public Material skybox;
        [Tooltip("카메라 배경색(Clear Flags가 Solid Color일 때)")]
        public Color cameraBackground;

        [Header("안개")]
        public bool fog;
        public Color fogColor;
        [Min(0f)] public float fogDensity;

        /// <summary>두 값 사이. 색·밝기·각도·안개 농도는 섞고, 하늘 머티리얼과 안개 켜기는 t가 0.5를 넘을 때 b로 바뀐다.</summary>
        public static LightingPreset Lerp(LightingPreset a, LightingPreset b, float t)
        {
            t = Mathf.Clamp01(t);
            bool second = t >= 0.5f;
            return new LightingPreset
            {
                key = second ? b.key : a.key,
                sunColor = Color.Lerp(a.sunColor, b.sunColor, t),
                sunIntensity = Mathf.Lerp(a.sunIntensity, b.sunIntensity, t),
                sunEuler = new Vector3(
                    Mathf.LerpAngle(a.sunEuler.x, b.sunEuler.x, t),
                    Mathf.LerpAngle(a.sunEuler.y, b.sunEuler.y, t),
                    Mathf.LerpAngle(a.sunEuler.z, b.sunEuler.z, t)),
                ambientColor = Color.Lerp(a.ambientColor, b.ambientColor, t),
                skybox = second ? b.skybox : a.skybox,
                cameraBackground = Color.Lerp(a.cameraBackground, b.cameraBackground, t),
                fog = second ? b.fog : a.fog,
                fogColor = Color.Lerp(a.fogColor, b.fogColor, t),
                fogDensity = Mathf.Lerp(a.fogDensity, b.fogDensity, t),
            };
        }
    }
}
