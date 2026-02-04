// UI-13 装备详情（首版：已穿戴装备）
using UnityEngine;
using UnityEngine.UI;

public class EquipmentDetailsPopup : MonoBehaviour
{
    private Text titleText;
    private Text descText;
    private Button unequipBtn;

    private string slot;
    private int equipmentId;

    public void Show(string slotName, int eqId)
    {
        slot = slotName;
        equipmentId = eqId;

        string name = EconomyRules.GetEquipmentName(equipmentId);
        var cfg = (ConfigManager.Instance != null && ConfigManager.Instance.EquipmentConfigs != null &&
                   ConfigManager.Instance.EquipmentConfigs.TryGetValue(equipmentId, out var c)) ? c : null;

        if (titleText != null) titleText.text = name;
        if (descText != null)
        {
            if (cfg != null)
                descText.text = $"槽位：{slot}\n攻击+{cfg.attack_bonus}  防御+{cfg.defense_bonus}\n品质：{cfg.quality}";
            else
                descText.text = $"槽位：{slot}\n装备ID：{equipmentId}";
        }

        if (unequipBtn != null)
        {
            unequipBtn.onClick.RemoveAllListeners();
            unequipBtn.onClick.AddListener(() =>
            {
                bool ok = InventoryService.TryUnequip(slot);
                if (!ok) Debug.LogWarning($"[EquipmentDetailsPopup] 卸下失败: slot={slot}");
                Hide();
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
        prt.sizeDelta = new Vector2(820f, 560f);
        var pimg = panelGo.AddComponent<Image>();
        pimg.color = Color.white;
        pimg.raycastTarget = false;
        UITheme.ApplyImageSprite(pimg, "UI/Common/UI_Panel_Background", preserveAspect: true);

        titleText = CreateText(prt, "_Text_Title", "装备详情", new Vector2(0.5f, 0.82f), new Vector2(740f, 70f), 40, TextAnchor.MiddleCenter);
        descText = CreateText(prt, "_Text_Desc", "", new Vector2(0.5f, 0.58f), new Vector2(740f, 180f), 24, TextAnchor.MiddleCenter);

        unequipBtn = CreateButton(prt, "_Button_Unequip", "卸下", new Vector2(0.5f, 0.20f), new Vector2(420f, 120f));
        var close = CreateButton(prt, "_Button_Close", "关闭", new Vector2(0.5f, 0.06f), new Vector2(420f, 120f));
        close.onClick.AddListener(Hide);

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

