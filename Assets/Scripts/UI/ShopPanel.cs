// UI-08 商城（一级界面）
// 首版：售卖“铁剑(500)”与“治疗药水(100)”（UI策划案 v3.2 / 数值策划案 v3.2），购买扣金币并入背包/数量+1。
using System;
using UnityEngine;
using UnityEngine.UI;

public class ShopPanel : MonoBehaviour
{
    private Text hintText;
    private Action<object> onGoldChanged;

    private void Start()
    {
        var root = GetComponent<RectTransform>();
        if (root == null) root = gameObject.AddComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        var bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.35f);
        bg.raycastTarget = false;

        var safe = CreateSafeArea(root);

        CreateText(safe, "_Text_Title", "商城", new Vector2(0.5f, 0.92f), new Vector2(500f, 70f), 42, TextAnchor.MiddleCenter);
        hintText = CreateText(safe, "_Text_Hint", "", new Vector2(0.5f, 0.18f), new Vector2(900f, 40f), 22, TextAnchor.MiddleCenter);
        hintText.color = new Color(1f, 1f, 1f, 0.9f);

        // 商品列表（首版口径：铁剑 + 治疗药水）
        float y = 0.70f;
        CreateShopItem(safe, 1002, new Vector2(0.5f, y));
        y -= 0.22f;
        CreatePotionItem(safe, new Vector2(0.5f, y));

        var back = CreateButton(safe, "_Button_Back", "返回", new Vector2(0.5f, 0.08f), new Vector2(420f, 120f));
        back.onClick.AddListener(() =>
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowMainMenuHome();
        });
    }

    private void OnEnable()
    {
        onGoldChanged = _ => { if (hintText != null) hintText.text = ""; };
        EventManager.AddListener("GOLD_CHANGED", onGoldChanged);
    }

    private void OnDisable()
    {
        if (onGoldChanged != null) EventManager.RemoveListener("GOLD_CHANGED", onGoldChanged);
    }

    private void CreateShopItem(RectTransform parent, int equipmentId, Vector2 anchorPos)
    {
        // 卡片容器
        var card = new GameObject($"_Item_{equipmentId}");
        card.transform.SetParent(parent, false);
        var rt = card.AddComponent<RectTransform>();
        rt.anchorMin = anchorPos;
        rt.anchorMax = anchorPos;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(860f, 180f);
        var img = card.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.25f);

        string name = "商品";
        int price = 200;
        string iconPath = "UI/Common/UI_Icon_Gold";

        if (ConfigManager.Instance != null && ConfigManager.Instance.EquipmentConfigs != null &&
            ConfigManager.Instance.EquipmentConfigs.TryGetValue(equipmentId, out var cfg) && cfg != null)
        {
            if (!string.IsNullOrEmpty(cfg.name)) name = cfg.name;
            iconPath = string.IsNullOrEmpty(cfg.icon_path) ? iconPath : cfg.icon_path;
            price = EconomyRules.GetEquipmentBuyPrice(equipmentId);
        }

        // Icon
        var iconGo = new GameObject("_Icon");
        iconGo.transform.SetParent(card.transform, false);
        var iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.12f, 0.5f);
        iconRt.anchorMax = new Vector2(0.12f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = Vector2.zero;
        iconRt.sizeDelta = new Vector2(120f, 120f);
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.color = Color.white;
        iconImg.raycastTarget = false;
        UITheme.ApplyImageSprite(iconImg, iconPath, preserveAspect: true);

        // Name + price
        CreateText(rt, "_Text_Name", name, new Vector2(0.34f, 0.64f), new Vector2(420f, 50f), 30, TextAnchor.MiddleLeft);
        CreateText(rt, "_Text_PriceLabel", "价格：", new Vector2(0.34f, 0.34f), new Vector2(140f, 44f), 24, TextAnchor.MiddleLeft);
        CreateText(rt, "_Text_Price", price.ToString(), new Vector2(0.46f, 0.34f), new Vector2(200f, 44f), 24, TextAnchor.MiddleLeft);
        CreateImage(rt, "_GoldIcon", "UI/Common/UI_Icon_Gold", new Vector2(0.58f, 0.34f), new Vector2(44f, 44f), true);

        var buy = CreateButton(rt, "_Button_Buy", "购买", new Vector2(0.86f, 0.5f), new Vector2(220f, 100f));
        buy.onClick.AddListener(() => TryBuyEquipment(equipmentId, price));
    }

    private void CreatePotionItem(RectTransform parent, Vector2 anchorPos)
    {
        var card = new GameObject("_Item_Potion");
        card.transform.SetParent(parent, false);
        var rt = card.AddComponent<RectTransform>();
        rt.anchorMin = anchorPos;
        rt.anchorMax = anchorPos;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(860f, 180f);
        var img = card.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.25f);

        const string name = "治疗药水";
        const int price = 100;

        // Icon
        var iconGo = new GameObject("_Icon");
        iconGo.transform.SetParent(card.transform, false);
        var iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.12f, 0.5f);
        iconRt.anchorMax = new Vector2(0.12f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = Vector2.zero;
        iconRt.sizeDelta = new Vector2(120f, 120f);
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.color = Color.white;
        iconImg.raycastTarget = false;
        UITheme.ApplyImageSprite(iconImg, "UI/Gameplay/UI_Icon_Skill_Heal", preserveAspect: true);

        CreateText(rt, "_Text_Name", name, new Vector2(0.34f, 0.64f), new Vector2(420f, 50f), 30, TextAnchor.MiddleLeft);
        CreateText(rt, "_Text_Desc", "恢复 30% 生命值（战斗内使用）", new Vector2(0.34f, 0.44f), new Vector2(520f, 44f), 22, TextAnchor.MiddleLeft);
        CreateText(rt, "_Text_PriceLabel", "价格：", new Vector2(0.34f, 0.24f), new Vector2(140f, 44f), 24, TextAnchor.MiddleLeft);
        CreateText(rt, "_Text_Price", price.ToString(), new Vector2(0.46f, 0.24f), new Vector2(200f, 44f), 24, TextAnchor.MiddleLeft);
        CreateImage(rt, "_GoldIcon", "UI/Common/UI_Icon_Gold", new Vector2(0.58f, 0.24f), new Vector2(44f, 44f), true);

        var buy = CreateButton(rt, "_Button_Buy", "购买", new Vector2(0.86f, 0.5f), new Vector2(220f, 100f));
        buy.onClick.AddListener(() => TryBuyPotion(price));
    }

    private void TryBuyPotion(int price)
    {
        int gold = InventoryService.GetGold();
        if (gold < price)
        {
            if (hintText != null) hintText.text = "金币不足";
            if (UIManager.Instance != null) UIManager.Instance.ShowToast("金币不足");
            return;
        }
        InventoryService.AddGold(-price);
        InventoryService.AddPotion(1);
        if (hintText != null) hintText.text = "购买成功";
        if (UIManager.Instance != null) UIManager.Instance.ShowToast("购买成功");
    }

    private void TryBuyEquipment(int equipmentId, int price)
    {
        SaveSystem.EnsureLoaded();
        var save = SaveSystem.GetCached();
        if (save == null || save.player == null) return;

        if (save.player.gold < price)
        {
            if (hintText != null) hintText.text = "金币不足";
            if (UIManager.Instance != null) UIManager.Instance.ShowToast("金币不足");
            return;
        }

        save.player.gold -= price;
        SaveSystem.SaveNow();
        EventManager.TriggerEvent("GOLD_CHANGED", new object[] { save.player.gold, -price });

        // 入背包：优先走运行态 EquipmentManager，否则直接写存档结构
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.AddItem(equipmentId, 1);
            SaveSystem.CaptureFromRuntime();
            SaveSystem.SaveNow();
        }
        else
        {
            AddEquipmentToSave(save, equipmentId, 1);
            SaveSystem.SaveNow();
            EventManager.TriggerEvent("INVENTORY_UPDATED", new object[] { equipmentId, 1 });
        }

        if (hintText != null) hintText.text = "购买成功";
        if (UIManager.Instance != null) UIManager.Instance.ShowToast("购买成功");
    }

    private static void AddEquipmentToSave(GameSaveData save, int equipmentId, int count)
    {
        if (save == null) return;
        if (save.equipment == null) save.equipment = new EquipmentSaveData();
        if (save.equipment.inventory == null) save.equipment.inventory = new System.Collections.Generic.List<IntIntPair>();

        for (int i = 0; i < save.equipment.inventory.Count; i++)
        {
            var p = save.equipment.inventory[i];
            if (p != null && p.key == equipmentId)
            {
                p.value += count;
                return;
            }
        }
        save.equipment.inventory.Add(new IntIntPair { key = equipmentId, value = count });
    }

    private static RectTransform CreateSafeArea(RectTransform parent)
    {
        var go = new GameObject("_SafeArea");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(40f, 100f);
        rt.offsetMax = new Vector2(-40f, -100f);
        return rt;
    }

    private static void CreateImage(RectTransform parent, string name, string spritePath, Vector2 anchorPos, Vector2 size, bool preserveAspect)
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
        img.raycastTarget = false;
        UITheme.ApplyImageSprite(img, spritePath, preserveAspect);
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
        UITheme.ApplySpriteSwapButton(btn, "UI_Button_Small_Normal", "UI_Button_Small_Pressed", "UI_Button_Small_Disabled");

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
        text.fontSize = 26;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        return btn;
    }
}

