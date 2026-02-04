// UI-10 关卡确认弹窗
// 依据：需求知识库 v2.2 0.6.2、开发计划 v2.4
using UnityEngine;
using UnityEngine.UI;

public class LevelConfirmationPopup : MonoBehaviour
{
    private Text titleText;
    private Text descText;
    private int pendingLevelId;

    /// <summary>用户点击「取消」时调用，用于恢复主菜单主按钮（避免重叠）。</summary>
    public System.Action OnCancel;

    public void Show(int levelId)
    {
        pendingLevelId = levelId;
        if (titleText != null) titleText.text = $"进入第{levelId}关？";
        if (descText != null)
        {
            LevelConfig cfg = null;
            bool hasCfg = ConfigManager.Instance != null && ConfigManager.Instance.LevelConfigs != null &&
                          ConfigManager.Instance.LevelConfigs.TryGetValue(levelId, out cfg) && cfg != null;
            if (!hasCfg)
            {
                descText.text = "该关卡尚未配置，仍可进入（将回退到第1关）。";
            }
            else
            {
                string rec = cfg.recommended_level > 0 ? $"推荐等级：{cfg.recommended_level}\n" : "";
                string rew = (cfg.reward_gold > 0 || cfg.reward_exp > 0) ? $"通关奖励：金币 +{cfg.reward_gold}，经验 +{cfg.reward_exp}" : "通关奖励：按默认规则结算";
                descText.text = rec + rew;
            }
        }
        gameObject.SetActive(true);
    }

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

        // Panel
        var panelGo = new GameObject("_Panel");
        panelGo.transform.SetParent(transform, false);
        var prt = panelGo.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = Vector2.zero;
        prt.sizeDelta = new Vector2(720f, 520f);
        var pimg = panelGo.AddComponent<Image>();
        pimg.color = Color.white;
        pimg.raycastTarget = false;
        UITheme.ApplyImageSprite(pimg, "UI/Common/UI_Panel_Background", preserveAspect: true);

        titleText = CreateText(prt, "_Text_Title", "进入关卡？", new Vector2(0.5f, 0.78f), new Vector2(600f, 80f), 40, TextAnchor.MiddleCenter);
        descText = CreateText(prt, "_Text_Desc", "", new Vector2(0.5f, 0.58f), new Vector2(620f, 120f), 24, TextAnchor.MiddleCenter);

        // 信息区与按钮、两按钮之间留足间距，避免布局重叠/过紧
        var ok = CreateButton(prt, "_Button_Confirm", "开始", new Vector2(0.5f, 0.33f), new Vector2(400f, 110f));
        ok.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
            if (GameManager.Instance != null)
            {
                int levelId = pendingLevelId;
                if (ConfigManager.Instance != null && ConfigManager.Instance.LevelConfigs != null &&
                    !ConfigManager.Instance.LevelConfigs.ContainsKey(levelId))
                    levelId = 1;
                GameManager.Instance.StartGame(levelId);
            }
        });

        var cancel = CreateButton(prt, "_Button_Cancel", "取消", new Vector2(0.5f, 0.10f), new Vector2(400f, 110f));
        cancel.onClick.AddListener(() =>
        {
            OnCancel?.Invoke();
            gameObject.SetActive(false);
        });

        gameObject.SetActive(false);
    }

    private static Text CreateText(RectTransform parent, string name, string content, Vector2 anchorPos, Vector2 size, int fontSize, TextAnchor align)
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
        text.alignment = align;
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

