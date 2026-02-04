// Toast 提示（Tip层）：用于金币不足/购买成功/卖出成功等轻提示
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ToastManager : MonoBehaviour
{
    public static ToastManager Instance { get; private set; }

    private RectTransform root;
    private CanvasGroup cg;
    private Text text;
    private Coroutine routine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        root = GetComponent<RectTransform>();
        if (root == null) root = gameObject.AddComponent<RectTransform>();
        root.anchorMin = new Vector2(0f, 0f);
        root.anchorMax = new Vector2(1f, 0f);
        root.pivot = new Vector2(0.5f, 0f);
        root.anchoredPosition = Vector2.zero;
        root.sizeDelta = new Vector2(0f, 300f);

        cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        var bgGo = new GameObject("_ToastBG");
        bgGo.transform.SetParent(transform, false);
        var bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0.5f, 0.35f);
        bgRt.anchorMax = new Vector2(0.5f, 0.35f);
        bgRt.pivot = new Vector2(0.5f, 0.5f);
        bgRt.anchoredPosition = Vector2.zero;
        bgRt.sizeDelta = new Vector2(760f, 90f);
        var bg = bgGo.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.7f);
        bg.raycastTarget = false;

        var textGo = new GameObject("_Text");
        textGo.transform.SetParent(bgGo.transform, false);
        var trt = textGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(24f, 10f);
        trt.offsetMax = new Vector2(-24f, -10f);
        text = textGo.AddComponent<Text>();
        text.text = "";
        text.font = UITheme.DefaultFont;
        text.fontSize = 28;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
    }

    public void Show(string message, float seconds = 1.2f)
    {
        if (string.IsNullOrEmpty(message)) return;
        if (text != null) text.text = message;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(CoShow(seconds));
    }

    private IEnumerator CoShow(float seconds)
    {
        seconds = Mathf.Clamp(seconds, 0.3f, 5f);

        float t = 0f;
        while (t < 0.12f)
        {
            t += Time.unscaledDeltaTime;
            if (cg != null) cg.alpha = Mathf.Clamp01(t / 0.12f);
            yield return null;
        }
        if (cg != null) cg.alpha = 1f;

        float hold = seconds;
        while (hold > 0f)
        {
            hold -= Time.unscaledDeltaTime;
            yield return null;
        }

        t = 0f;
        while (t < 0.18f)
        {
            t += Time.unscaledDeltaTime;
            if (cg != null) cg.alpha = 1f - Mathf.Clamp01(t / 0.18f);
            yield return null;
        }
        if (cg != null) cg.alpha = 0f;
        routine = null;
    }
}

