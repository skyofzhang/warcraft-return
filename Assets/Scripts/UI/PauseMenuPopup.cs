// UI-15 暂停菜单（弹窗）
// 依据：需求知识库 v2.2 0.6.2、策划知识库 v2.2 0.6.3
using UnityEngine;
using UnityEngine.UI;

public class PauseMenuPopup : MonoBehaviour
{
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
        prt.sizeDelta = new Vector2(620f, 700f);
        var pimg = panelGo.AddComponent<Image>();
        pimg.color = Color.white;
        pimg.raycastTarget = false;
        UITheme.ApplyImageSprite(pimg, "UI/Common/UI_Panel_Background", preserveAspect: true);

        CreateText(prt, "_Text_Title", "暂停", new Vector2(0.5f, 0.86f), new Vector2(400f, 80f), 44);

        var resume = CreateButton(prt, "_Button_Resume", "继续", new Vector2(0.5f, 0.70f), new Vector2(400f, 120f));
        resume.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
            if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
        });

        var settings = CreateButton(prt, "_Button_Settings", "设置", new Vector2(0.5f, 0.52f), new Vector2(400f, 120f));
        settings.onClick.AddListener(() =>
        {
            // 暂停时打开设置弹窗（不切状态，不恢复时间）
            if (UIManager.Instance != null) UIManager.Instance.ShowSettingsPopup();
        });

        var restart = CreateButton(prt, "_Button_Restart", "重新开始", new Vector2(0.5f, 0.34f), new Vector2(400f, 120f));
        restart.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
            Time.timeScale = 1f;
            if (GameManager.Instance != null) GameManager.Instance.RetryLevel();
        });

        var home = CreateButton(prt, "_Button_Home", "返回主菜单", new Vector2(0.5f, 0.16f), new Vector2(400f, 120f));
        home.onClick.AddListener(() =>
        {
            // 二次确认：避免误触导致丢进度（UI策划案 v3.2 Pause->HomeConfirm）
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowConfirm(
                    "返回主菜单",
                    "返回主菜单将放弃本局进度，是否继续？",
                    "返回主菜单",
                    "取消",
                    () =>
                    {
                        gameObject.SetActive(false);
                        Time.timeScale = 1f;
                        if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu();
                    });
            }
        });

        var close = CreateButton(prt, "_Button_Close", "关闭", new Vector2(0.5f, 0.08f), new Vector2(400f, 110f));
        close.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
            if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
        });

        gameObject.SetActive(false);
    }

    public void Show()
    {
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

