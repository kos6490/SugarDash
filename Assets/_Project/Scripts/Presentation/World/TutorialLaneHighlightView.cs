using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 튜토리얼 트랙 안내에서 아이가 지나가야 할 레인을 반투명 띠로 강조한다.
    /// 캐릭터 앞(z=0)에서 대상(toZ)까지 레인마다 띠 하나. 빈 틀은 노란 반투명 납작한 Cube를 쓴다.
    /// 연출만이다. 충돌체가 없고 장애물·충돌 판정에 영향을 주지 않는다.
    /// </summary>
    // [빈 틀] 담당: 월드
    public sealed class TutorialLaneHighlightView : MonoBehaviour
    {
        private const float StripY = 0.02f;
        private const float StripThickness = 0.02f;
        private const float StripWidthRatio = 0.9f;

        [Header("샌드박스(혼자 시험할 때만. GameBootstrapper가 Show/Hide를 부르면 덮어쓴다)")]
        [Tooltip("켜 두면 시작할 때 아래 값으로 띠를 보여 준다.")]
        [SerializeField] private bool sandboxShow;
        [SerializeField] private int[] sandboxLanes = { 0 };
        [SerializeField] private float sandboxLaneWidth = 1.5f;
        [SerializeField] private float sandboxToZ = 12f;

        [Header("모양")]
        [Tooltip("띠 모양 프리팹(길이 1, 폭 1 기준으로 늘어난다). 비워 두면 노란 반투명 납작한 Cube를 쓴다.")]
        [SerializeField] private GameObject stripPrefab;
        [SerializeField] private Color stripColor = new Color(1f, 0.92f, 0.1f, 0.35f);

        [Header("반짝임")]
        [Tooltip("반짝임 한 번(흐림 → 진함 → 흐림)에 걸리는 시간(초). 0이면 반짝이지 않고 Strip Color 그대로다.")]
        [Min(0f)] [SerializeField] private float pulsePeriod = 0.8f;
        [Tooltip("가장 흐릴 때의 투명도. 가장 진할 때는 Strip Color의 투명도다.")]
        [Range(0f, 1f)] [SerializeField] private float pulseMinAlpha = 0.12f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private MaterialPropertyBlock _block;
        private readonly List<Renderer[]> _stripRenderers = new List<Renderer[]>();
        private float _shownSince;

        private readonly List<Transform> _strips = new List<Transform>();
        // 기본 띠가 함께 쓰는 머티리얼 사본 하나. 저절로 지워지지 않아 OnDestroy에서 지운다.
        private Material _stripMaterial;
        private bool _visible;

        /// <summary>레인 가운데에서 z=0~toZ 구간의 가운데. x = lane × laneWidth.</summary>
        public static Vector3 StripCenter(int lane, float laneWidth, float toZ) =>
            new Vector3(lane * laneWidth, StripY, StripLength(toZ) * 0.5f);

        /// <summary>띠 길이. 대상이 캐릭터 뒤(toZ ≤ 0)면 0.</summary>
        public static float StripLength(float toZ) => Mathf.Max(0f, toZ);

        public bool IsVisible => _visible;

        /// <summary>매 프레임 불러도 된다. 띠는 다시 쓰고 새로 만들지 않는다(레인 수가 늘 때만 만든다).</summary>
        public void Show(IReadOnlyList<int> lanes, float laneWidth, float toZ)
        {
            if (lanes == null || lanes.Count == 0) { Hide(); return; }
            if (!_visible) _shownSince = Time.time;
            _visible = true;
            // Inspector에서 바꾼 색이 플레이 중에도 보이게 매번 넣는다.
            if (_stripMaterial != null) _stripMaterial.color = stripColor;
            while (_strips.Count < lanes.Count) _strips.Add(CreateStrip());
            for (int i = 0; i < _strips.Count; i++)
            {
                var strip = _strips[i];
                bool used = i < lanes.Count;
                if (strip.gameObject.activeSelf != used) strip.gameObject.SetActive(used);
                if (!used) continue;
                strip.localPosition = StripCenter(lanes[i], laneWidth, toZ);
                strip.localScale = new Vector3(laneWidth * StripWidthRatio, StripThickness, StripLength(toZ));
            }
            // 띠 반짝임: 보이기 시작한 때부터 투명도가 흐림 ↔ 진함으로 오간다(진할 때가 Strip Color).
            ApplyPulse(lanes.Count);
        }

        private void ApplyPulse(int usedCount)
        {
            float alpha = WorldMotion.Pulse(Time.time - _shownSince, pulseMinAlpha, stripColor.a, pulsePeriod);
            var color = WorldMotion.WithAlpha(stripColor, alpha);
            if (_stripMaterial != null) _stripMaterial.color = color;
            if (stripPrefab == null) return;

            // 프리팹 띠는 머티리얼 사본을 만들지 않고 색만 덮어쓴다(URP Lit은 _BaseColor, 그 밖은 _Color).
            if (_block == null) _block = new MaterialPropertyBlock();
            // Show는 매 프레임 불리므로 띠마다 렌더러 목록을 한 번만 찾아 둔다.
            while (_stripRenderers.Count < _strips.Count)
                _stripRenderers.Add(_strips[_stripRenderers.Count].GetComponentsInChildren<Renderer>(true));
            for (int i = 0; i < usedCount && i < _strips.Count; i++)
            {
                foreach (var rend in _stripRenderers[i])
                {
                    rend.GetPropertyBlock(_block);
                    _block.SetColor(BaseColorId, color);
                    _block.SetColor(ColorId, color);
                    rend.SetPropertyBlock(_block);
                }
            }
        }

        public void Hide()
        {
            if (!_visible) return;
            _visible = false;
            foreach (var strip in _strips) strip.gameObject.SetActive(false);
        }

        private void Start()
        {
            if (sandboxShow) Show(sandboxLanes, sandboxLaneWidth, sandboxToZ);
        }

        private Transform CreateStrip()
        {
            GameObject go;
            if (stripPrefab != null)
            {
                go = Instantiate(stripPrefab, transform, false);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                // 시각 전용이다. 충돌은 계산으로 판정하므로 콜라이더를 두지 않는다.
                Destroy(go.GetComponent<Collider>());
                var rend = go.GetComponent<Renderer>();
                rend.shadowCastingMode = ShadowCastingMode.Off;
                rend.receiveShadows = false;
                if (_stripMaterial == null)
                {
                    var shader = Shader.Find("Sprites/Default");
                    _stripMaterial = shader != null ? new Material(shader) : new Material(rend.sharedMaterial);
                }
                _stripMaterial.color = stripColor;
                rend.sharedMaterial = _stripMaterial;
                go.transform.SetParent(transform, false);
            }
            go.name = "TutorialLaneStrip";
            return go.transform;
        }

        private void OnDestroy()
        {
            if (_stripMaterial != null) Destroy(_stripMaterial);
            _stripMaterial = null;
        }
    }
}
