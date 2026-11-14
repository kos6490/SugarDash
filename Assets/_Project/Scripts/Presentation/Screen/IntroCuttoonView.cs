using System;
using SugarDash.App;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 인트로 컷툰. 처음 실행해 유형을 고른 직후 타이틀 화면 위에 컷툰을 한 칸씩 보여 준다.
    /// 화면을 누르면 다음 칸, 마지막 칸에서 누르거나 건너뛰기를 누르면 닫히고 타이틀이 보인다.
    /// 그림이 한 장이면 건너뛰기와 쪽 번호 없이 화면을 한 번 누르면 닫힌다.
    /// 연출만이다. 게임 흐름(GameFlowController)을 읽기만 하고 명령을 보내지 않는다. 혈당·기록과 관계없다.
    /// 화면 오브젝트(캔버스, 그림, 버튼)는 처음 열 때 코드로 만든다. 프리팹이 생기면 바꾼다.
    /// </summary>
    // [영역 파일] 담당: 화면(인트로 컷툰. 팀원2가 맡아 작성)
    public sealed class IntroCuttoonView : MonoBehaviour
    {
        [Tooltip("컷툰 그림. 보여 줄 순서대로 넣는다. 비어 있으면 컷툰을 열지 않는다.")]
        [SerializeField] private Sprite[] panels;
        [Tooltip("버튼·안내 글자에 쓸 글꼴(Jua). 비워 두면 Unity 기본 글꼴을 쓴다.")]
        [SerializeField] private Font font;
        [Tooltip("켜면 앱을 켤 때마다 타이틀 앞에서 보여 준다(확인용). 끄면 처음 유형을 고른 직후에만 보여 준다.")]
        [SerializeField] private bool showEveryLaunch;

        [Header("모양")]
        [SerializeField] private Color backgroundColor = new Color(1f, 0.98f, 0.94f);
        [SerializeField] private Color buttonColor = new Color(0.29f, 0.2f, 0.17f, 0.85f);
        [SerializeField] private Color buttonTextColor = Color.white;
        [SerializeField] private Color hintColor = new Color(0.29f, 0.2f, 0.17f);
        [SerializeField] private int sortingOrder = 500;
        [SerializeField] private string skipLabel = "건너뛰기";
        [SerializeField] private string nextHint = "화면을 누르면 다음으로 넘어가요";
        [SerializeField] private string lastHint = "화면을 누르면 시작해요";

        private GameFlowController _flow;
        private GameObject _root;
        private Image _panelImage;
        private Text _hintText;
        private Text _pageText;
        private GameObject _skipButton;
        private int _index;
        private bool _sawTypeSelect;
        private bool _shown;

        /// <summary>컷툰이 떠 있다.</summary>
        public bool IsOpen => _root != null && _root.activeSelf;

        /// <summary>
        /// 컷툰을 열 때인지. 타이틀 화면이고 아직 보여 주지 않았을 때, 이번 실행에서 유형 선택 화면을 거쳤거나(처음 실행)
        /// 매번 보기가 켜져 있으면 연다.
        /// </summary>
        public static bool ShouldOpen(FlowScreen screen, bool sawTypeSelect, bool alreadyShown, bool everyLaunch) =>
            screen == FlowScreen.Title && !alreadyShown && (sawTypeSelect || everyLaunch);

        /// <summary>index번째 칸에서 다음으로 넘기면 닫히는지(마지막 칸인지).</summary>
        public static bool IsLast(int index, int count) => index >= count - 1;

        /// <summary>GameBootstrapper가 연결한다. 반환값을 Dispose하면 연결을 해제하고 컷툰을 닫는다.</summary>
        public IDisposable Bind(GameFlowController flow)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            return new Unbinder(() =>
            {
                _flow = null;
                Close();
            });
        }

        private void Update()
        {
            if (_flow == null) return;
            var screen = _flow.Screen;
            if (screen == FlowScreen.TypeSelect) _sawTypeSelect = true;
            if (!ShouldOpen(screen, _sawTypeSelect, _shown, showEveryLaunch)) return;
            _shown = true;
            Open();
        }

        private void Open()
        {
            if (panels == null || panels.Length == 0) return;
            if (_root == null) Build();
            _index = 0;
            _root.SetActive(true);
            Show();
        }

        private void Next()
        {
            if (!IsOpen) return;
            if (IsLast(_index, panels.Length)) { Close(); return; }
            _index++;
            Show();
        }

        private void Close()
        {
            if (_root != null) _root.SetActive(false);
        }

        private void Show()
        {
            _panelImage.sprite = panels[_index];
            _hintText.text = IsLast(_index, panels.Length) ? lastHint : nextHint;
            // 그림이 한 장이면 쪽 번호와 건너뛰기를 숨긴다(한 번 누르면 닫히므로 필요 없다).
            _pageText.gameObject.SetActive(panels.Length > 1);
            _skipButton.SetActive(panels.Length > 1);
            _pageText.text = (_index + 1) + " / " + panels.Length;
        }

        // ───── 화면 만들기 ─────

        private void Build()
        {
            _root = new GameObject("Intro Cuttoon Canvas");
            _root.transform.SetParent(transform, false);
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            _root.AddComponent<GraphicRaycaster>();

            // 배경: 화면 전체를 덮고, 누르면 다음 칸으로 넘긴다. 아래 타이틀 버튼이 눌리지 않게 터치를 막는다.
            var background = AddImage("Background", _root.transform, backgroundColor, Vector2.zero, Vector2.one);
            background.gameObject.AddComponent<Button>().onClick.AddListener(Next);

            _panelImage = AddImage("Panel", _root.transform, Color.white, new Vector2(0.02f, 0.11f), new Vector2(0.98f, 0.905f));
            _panelImage.preserveAspect = true;
            _panelImage.raycastTarget = false;

            _hintText = AddText("Hint", _root.transform, 40, hintColor, new Vector2(0.05f, 0.03f), new Vector2(0.95f, 0.1f));
            _pageText = AddText("Page", _root.transform, 36, hintColor, new Vector2(0.04f, 0.915f), new Vector2(0.3f, 0.975f));
            _pageText.alignment = TextAnchor.MiddleLeft;

            var skip = AddImage("Skip", _root.transform, buttonColor, new Vector2(0.66f, 0.915f), new Vector2(0.96f, 0.975f));
            skip.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            _skipButton = skip.gameObject;
            AddText("Label", skip.transform, 38, buttonTextColor, Vector2.zero, Vector2.one).text = skipLabel;
        }

        private static Image AddImage(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var image = AddRect(name, parent, anchorMin, anchorMax).gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private Text AddText(string name, Transform parent, int size, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var text = AddRect(name, parent, anchorMin, anchorMax).gameObject.AddComponent<Text>();
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform AddRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }
    }
}
