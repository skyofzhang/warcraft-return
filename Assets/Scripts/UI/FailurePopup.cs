// UI-16 失败界面（弹窗）
// 说明：当前工程失败会进入 Settlement 状态；本弹窗用于满足“失败界面”交互与按钮语义。
using UnityEngine;
using UnityEngine.UI;

public class FailurePopup : MonoBehaviour
{
    private Text titleText;

    private void Start()
    {
        var root = GetComponent<RectTransform>();
        if (root == null) root = gameObject.AddComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        var bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.65f);
        bg.raycastTarget = true;

        var panelGo = new GameObject("_Panel");
        panelGo.transform.SetParent(transform, false);
        var prt = panelGo.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = Vector2.zero;
        prt.sizeDelta = new Vector2(620f, 600f);
        var pimg = panelGo.AddComponent<Image>();
        pimg.color = Color.white;
        pimg.raycastTarget = false;
        UITheme.ApplyImageSprite(pimg, "UI/Common/UI_Panel_Background", preserveAspect: true);

        titleText = CreateText(prt, "_Text_Title", "失败", new Vector2(0.5f, 0.80f), new Vector2(400f, 80f), 46);

        var retry = CreateButton(prt, "_Button_Retry", "重试", new Vector2(0.5f, 0.38f), new Vector2(400f, 120f));
        retry.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
            Time.timeScale = 1f;
            if (GameManager.Instance != null) GameManager.Instance.RetryLevel();
        });

        var home = CreateButton(prt, "_Button_Home", "返回主菜单", new Vector2(0.5f, 0.20f), new Vector2(400f, 120f));
        home.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
            Time.timeScale = 1f;
            if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu();
        });

        gameObject.SetActive(false);
    }

    public void Show()
    {
        if (titleText != null) titleText.text = "失败";
        gameObject.SetActive(true);
    }

    private static Text CreateText(RectTransform parent, string name, string content, Vector2 anchorPos, Vector2 size, int fontSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorPos;
        rt.anchorMax = anchorPos;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        var text = go.AddComponent<Text>();
        text.text = content;
        text.font = UITheme.DefaultFont;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        return text;
    }

    private static Button CreateButton(RectTransform parent, string name, string label, Vector2 anchorPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorPos;
        rt.anchorMax = anchorPos;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = Color.white;
        var btn = go.AddComponent<Button>();
        go.AddComponent<UIButtonFeedback>();
        UITheme.ApplySpriteSwapButton(btn, "UI_Button_Big_Normal", "UI_Button_Big_Pressed", "UI_Button_Big_Disabled");

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        var text = textGo.AddComponent<Text>();
        text.text = label;
        text.font = UITheme.DefaultFont;
        text.fontSize = 28;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        return btn;
    }
}

