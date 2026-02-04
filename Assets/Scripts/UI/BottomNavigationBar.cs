// COMP-01 底部导航栏：主页/背包/商城/设置（首版口径）。
// 依据：需求知识库 v2.2 0.6.3、策划知识库 v2.2 0.6.2、开发计划 v2.4
using UnityEngine;
using UnityEngine.UI;

public class BottomNavigationBar : MonoBehaviour
{
    private Button homeBtn;
    private Button bagBtn;
    private Button shopBtn;
    private Button settingsBtn;

    private void Start()
    {
        var root = GetComponent<RectTransform>();
        if (root == null) root = gameObject.AddComponent<RectTransform>();
        root.anchorMin = new Vector2(0f, 0f);
        root.anchorMax = new Vector2(1f, 0f);
        root.pivot = new Vector2(0.5f, 0f);
        root.anchoredPosition = Vector2.zero;
        root.sizeDelta = new Vector2(0f, 160f);

        var bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.45f);
        bg.raycastTarget = true; // 吃掉底部点击

        // 4 等分按钮
        homeBtn = CreateNavButton(root, "主页", "UI/MainMenu/UI_MainMenu_Logo", 0);
        bagBtn = CreateNavButton(root, "背包", "UI/Common/UI_Icon_Bag", 1);
        shopBtn = CreateNavButton(root, "商城", "UI/Common/UI_Icon_Gold", 2);
        settingsBtn = CreateNavButton(root, "设置", "UI/Common/UI_Icon_Settings", 3);

        homeBtn.onClick.AddListener(() =>
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowMainMenuHome();
        });

        bagBtn.onClick.AddListener(() =>
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowInventoryPage();
        });

        shopBtn.onClick.AddListener(() =>
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowShopPage();
        });

        settingsBtn.onClick.AddListener(() =>
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowSettingsPage();
        });
    }

    private static Button CreateNavButton(RectTransform parent, string label, string iconPath, int index)
    {
        var go = new GameObject($"_Nav_{label}");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(index * 0.25f, 0f);
        rt.anchorMax = new Vector2((index + 1) * 0.25f, 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.05f);
        var btn = go.AddComponent<Button>();
        go.AddComponent<UIButtonFeedback>();

        // 图标
        var iconGo = new GameObject("_Icon");
        iconGo.transform.SetParent(go.transform, false);
        var iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.62f);
        iconRt.anchorMax = new Vector2(0.5f, 0.62f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = Vector2.zero;
        iconRt.sizeDelta = new Vector2(64f, 64f);
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.color = Color.white;
        iconImg.raycastTarget = false;
        UITheme.ApplyImageSprite(iconImg, iconPath, preserveAspect: true);

        // 文本
        var textGo = new GameObject("_Text");
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0.5f, 0.22f);
        textRt.anchorMax = new Vector2(0.5f, 0.22f);
        textRt.pivot = new Vector2(0.5f, 0.5f);
        textRt.anchoredPosition = Vector2.zero;
        textRt.sizeDelta = new Vector2(200f, 40f);
        var text = textGo.AddComponent<Text>();
        text.text = label;
        text.font = UITheme.DefaultFont;
        text.fontSize = 24;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;

        return btn;
    }
}

