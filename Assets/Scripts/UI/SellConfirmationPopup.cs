// UI-12 出售确认弹窗
using UnityEngine;
using UnityEngine.UI;

public class SellConfirmationPopup : MonoBehaviour
{
    private Text titleText;
    private Text descText;
    private Button confirmBtn;
    private Button cancelBtn;

    private int itemId;
    private int count;
    private int unitPrice;

    public void Show(int equipmentId, int sellCount, int unitSellPrice)
    {
        itemId = equipmentId;
        count = Mathf.Max(1, sellCount);
        unitPrice = Mathf.Max(0, unitSellPrice);

        string name = EconomyRules.GetEquipmentName(itemId);
        int total = unitPrice * count;

        if (titleText != null) titleText.text = "出售确认";
        if (descText != null) descText.text = $"是否出售 {name} x{count} ？\n获得金币：{total}";

        if (confirmBtn != null)
        {
            confirmBtn.onClick.RemoveAllListeners();
            confirmBtn.onClick.AddListener(() =>
            {
                bool ok = InventoryService.TrySell(itemId, count, unitPrice);
                if (!ok) Debug.LogWarning($"[SellConfirmationPopup] 出售失败: itemId={itemId}, count={count}");
                if (ok && UIManager.Instance != null) UIManager.Instance.ShowToast("出售成功");
                Hide();
            });
        }
        if (cancelBtn != null)
        {
            cancelBtn.onClick.RemoveAllListeners();
            cancelBtn.onClick.AddListener(Hide);
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
        prt.sizeDelta = new Vector2(760f, 520f);
        var pimg = panelGo.AddComponent<Image>();
        pimg.color = Color.white;
        pimg.raycastTarget = false;
        UITheme.ApplyImageSprite(pimg, "UI/Common/UI_Panel_Background", preserveAspect: true);

        titleText = CreateText(prt, "_Text_Title", "出售确认", new Vector2(0.5f, 0.82f), new Vector2(700f, 70f), 40, TextAnchor.MiddleCenter);
        descText = CreateText(prt, "_Text_Desc", "", new Vector2(0.5f, 0.58f), new Vector2(700f, 160f), 24, TextAnchor.MiddleCenter);

        confirmBtn = CreateButton(prt, "_Button_Confirm", "确认", new Vector2(0.5f, 0.24f), new Vector2(420f, 120f));
        cancelBtn = CreateButton(prt, "_Button_Cancel", "取消", new Vector2(0.5f, 0.08f), new Vector2(420f, 120f));

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

