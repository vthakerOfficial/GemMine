using UnityEngine;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    private Image m_fillImage;
    private Text m_messageText;
    private Text m_clickText;

    public static LoadingScreen Create()
    {
        var go = new GameObject("LoadingScreen");
        DontDestroyOnLoad(go);
        return go.AddComponent<LoadingScreen>();
    }

    private void Awake()
    {
        BuildUI();
        gameObject.SetActive(false);
    }

    private void BuildUI()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        gameObject.AddComponent<GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Dark overlay covering the whole screen
        MakePanel("Background", transform, Vector2.zero, Vector2.one, new Color(0.05f, 0.05f, 0.05f, 0.92f));

        // "Loading... 42%" title
        m_messageText = MakeText("LoadingText", transform, font, 48, Color.white,
            new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.70f), TextAnchor.MiddleCenter);
        m_messageText.text = "Loading...";

        // Grey progress track
        var barBg = MakePanel("BarBG", transform,
            new Vector2(0.15f, 0.455f), new Vector2(0.85f, 0.505f),
            new Color(0.2f, 0.2f, 0.2f, 1f));

        // Blue fill — uses Image.Type.Filled so fillAmount drives width
        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(barBg.transform, false);
        m_fillImage = fillGo.AddComponent<Image>();
        m_fillImage.color = new Color(0.18f, 0.65f, 1f, 1f);
        m_fillImage.type = Image.Type.Filled;
        m_fillImage.fillMethod = Image.FillMethod.Horizontal;
        m_fillImage.fillOrigin = 0;
        m_fillImage.fillAmount = 0f;
        var fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;

        // "Click anywhere to start" prompt — only visible when ready
        m_clickText = MakeText("ClickText", transform, font, 28, new Color(1f, 1f, 1f, 0.75f),
            new Vector2(0.1f, 0.37f), new Vector2(0.9f, 0.44f), TextAnchor.MiddleCenter);
        m_clickText.text = "";
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void SetProgress(float t)
    {
        t = Mathf.Clamp01(t);
        if (m_fillImage != null)
            m_fillImage.fillAmount = t;
        if (m_messageText != null)
            m_messageText.text = t >= 1f ? "Ready!" : string.Format("Loading...  {0}%", Mathf.RoundToInt(t * 100));
        if (m_clickText != null)
            m_clickText.text = t >= 1f ? "Click anywhere to start" : "";
    }

    // ── UI helpers ──────────────────────────────────────────────────────────

    private static GameObject MakePanel(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.sizeDelta = Vector2.zero;
        r.anchoredPosition = Vector2.zero;
        return go;
    }

    private static Text MakeText(string name, Transform parent, Font font,
        int size, Color color, Vector2 anchorMin, Vector2 anchorMax, TextAnchor alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.color = color;
        t.alignment = alignment;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.sizeDelta = Vector2.zero;
        r.anchoredPosition = Vector2.zero;
        return t;
    }
}
