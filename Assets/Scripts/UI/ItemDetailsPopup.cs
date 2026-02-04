// UI-11 道具详情（首版：装备道具）
using UnityEngine;
using UnityEngine.UI;

public class ItemDetailsPopup : MonoBehaviour
{
    private Text titleText;
    private Text descText;
    private Text countText;
    private Text priceText;
    private Button equipBtn;
    private Button sellBtn;

    private int itemId;
    private int count;

    public void ShowEquipment(int equipmentId, int countInBag)
    {
        itemId = equipmentId;
        count = countInBag;

        string name = GetEquipmentName(itemId);
        var cfg = GetEquipmentConfig(itemId);
        int sellPrice = EconomyRules.GetEquipmentSellPrice(itemId);

        if (titleText != null) titleText.text = name;
        if (countText != null) countText.text = $"数量：{count}";
        if (priceText != null) priceText.text = $"出售单价：{sellPrice}";

        if (descText != null)
        {
            if (cfg != null)
                descText.text = $"槽位：{cfg.type}  攻击+{cfg.attack_bonus}  防御+{cfg.defense_bonus}";
            else
                descText.text = $"装备ID：{itemId}";
        }

        if (equipBtn != null)
        {
            equipBtn.onClick.RemoveAllListeners();
            equipBtn.onClick.AddListener(() =>
            {
                bool ok = InventoryService.TryEquip(itemId);
                if (!ok) Debug.LogWarning($"[ItemDetailsPopup] 穿戴失败: itemId={itemId}");
                Hide();
            });
        }

        if (sellBtn != null)
        {
            sellBtn.onClick.RemoveAllListeners();
            sellBtn.onClick.AddListener(() =>
            {
                if (UIManager.Instance != null)
                    UIManager.Instance.ShowSellConfirmation(itemId, 1, sellPrice);
            });
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
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

        var panelGo = new GameObject("_Panel");
        panelGo.transform.SetParent(transform, false);
        var prt = panelGo.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = Vector2.zero;
        prt.sizeDelta = new Vector2(820f, 620f);
        var pimg = panelGo.AddComponent<Image>();
        pimg.color = Color.white;
        pimg.raycastTarget = false;
        UITheme.ApplyImageSprite(pimg, "UI/Common/UI_Panel_Background", preserveAspect: true);

        titleText = CreateText(prt, "_Text_Title", "道具详情", new Vector2(0.5f, 0.84f), new Vector2(740f, 70f), 40, TextAnchor.MiddleCenter);
        descText = CreateText(prt, "_Text_Desc", "", new Vector2(0.5f, 0.64f), new Vector2(740f, 80f), 24, TextAnchor.MiddleCenter);
        countText = CreateText(prt, "_Text_Count", "", new Vector2(0.5f, 0.52f), new Vector2(740f, 44f), 24, TextAnchor.MiddleCenter);
        priceText = CreateText(prt, "_Text_Price", "", new Vector2(0.5f, 0.44f), new Vector2(740f, 44f), 24, TextAnchor.MiddleCenter);

        equipBtn = CreateButton(prt, "_Button_Equip", "穿戴", new Vector2(0.5f, 0.22f), new Vector2(420f, 120f));
        sellBtn = CreateButton(prt, "_Button_Sell", "出售", new Vector2(0.5f, 0.08f), new Vector2(420f, 120f));

        var close = CreateButton(prt, "_Button_Close", "关闭", new Vector2(0.12f, 0.92f), new Vector2(180f, 80f));
        close.onClick.AddListener(Hide);

        gameObject.SetActive(false);
    }

    private static EquipmentConfig GetEquipmentConfig(int equipmentId)
    {
        if (ConfigManager.Instance != null && ConfigManager.Instance.EquipmentConfigs != null &&
            ConfigManager.Instance.EquipmentConfigs.TryGetValue(equipmentId, out var cfg))
            return cfg;
        return null;
    }

    private static string GetEquipmentName(int equipmentId)
    {
        var cfg = GetEquipmentConfig(equipmentId);
        if (cfg != null && !string.IsNullOrEmpty(cfg.name)) return cfg.name;
        return "装备";
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

