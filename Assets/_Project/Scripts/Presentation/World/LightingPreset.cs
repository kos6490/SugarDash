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

        /// <summary>낮의 옅은 안개 농도. 안개를 끄지 않고 옅게만 두어 때가 바뀔 때 셰이더가 새로 준비되지 않게 한다.</summary>
        public const float LightFogDensity = 0.004f;

        /// <summary>
        /// 때 4종의 처음 값(TimeOfDayLighting Inspector 목록의 처음 값). 연출 값이며 Inspector에서 고친다.
        /// 밤에는 해를 지평선 아래로 내려 하늘(기본 Procedural 하늘)이 어두워지게 한다.
        /// </summary>
        public static System.Collections.Generic.List<LightingPreset> CreateDefaults() =>
            new System.Collections.Generic.List<LightingPreset>
            {
                Make("morning", new Color(1f, 0.92f, 0.80f), 0.95f, new Vector3(22f, -30f, 0f),
                    new Color(0.60f, 0.62f, 0.68f), new Color(0.70f, 0.82f, 0.95f)),
                Make("afternoon", new Color(1f, 0.98f, 0.94f), 1.15f, new Vector3(55f, -30f, 0f),
                    new Color(0.68f, 0.68f, 0.70f), new Color(0.55f, 0.75f, 0.95f)),
                Make("evening", new Color(1f, 0.62f, 0.40f), 0.75f, new Vector3(6f, -60f, 0f),
                    new Color(0.52f, 0.44f, 0.46f), new Color(0.95f, 0.62f, 0.45f), 0.008f),
                Make("night", new Color(0.55f, 0.62f, 0.90f), 0f, new Vector3(-20f, -30f, 0f),
                    new Color(0.20f, 0.23f, 0.36f), new Color(0.06f, 0.08f, 0.18f), 0.02f),
            };

        // 안개는 늘 켜고 그 색(하늘색)과 농도만 바꾼다. 해가 연결되지 않아도 저녁·밤 느낌이 나게 한다.
        private static LightingPreset Make(string key, Color sun, float intensity, Vector3 euler, Color ambient, Color background,
            float fogDensity = LightFogDensity) =>
            new LightingPreset
            {
                key = key,
                sunColor = sun,
                sunIntensity = intensity,
                sunEuler = euler,
                ambientColor = ambient,
                cameraBackground = background,
                fog = true,
                fogColor = background,
                fogDensity = fogDensity,
            };

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
