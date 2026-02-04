// UI-04 背包（一级界面）
// 依据：UI策划案 v3.2（6装备槽 + 30格背包 + 详情/穿戴/出售/丢弃）
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryPanel : MonoBehaviour
{
    public Action OnClose;

    private RectTransform root;
    private RectTransform safe;
    private RectTransform equipArea;
    private RectTransform invArea;
    private RectTransform detailArea;

    private Text statsText;
    private Text invCountText;

    private Image detailIcon;
    private Image detailBorder;
    private Text detailName;
    private Text detailStats;
    private Text detailHint;
    private Button detailEquipBtn;
    private Button detailSellBtn;
    private Button detailDropBtn;

    private readonly Dictionary<string, EquipSlotView> equipSlots = new Dictionary<string, EquipSlotView>();
    private readonly SlotView[] invSlots = new SlotView[30];
    private readonly List<int> invItems = new List<int>(30);
    private Dictionary<int, int> invSnapshot = new Dictionary<int, int>();
    private Dictionary<string, int> equippedSnapshot = new Dictionary<string, int>();

    private int selectedItemId;
    private string selectedEquippedSlot;
    private int selectedInvIndex = -1;

    private Action<object> onInvChanged;
    private Action<object> onEquipChanged;

    // “新”标签：首版用运行期缓存即可（不写入存档）
    private static readonly HashSet<int> seenEquipmentIds = new HashSet<int>();

    private static readonly string[] SlotOrder =
    {
        "helmet", "weapon", "armor", "accessory", "pants", "boots"
    };

    private void Start()
    {
        BuildUI();
        Refresh();
    }

    private void OnEnable()
    {
        onInvChanged = _ => Refresh();
        onEquipChanged = _ => Refresh();
        EventManager.AddListener("INVENTORY_UPDATED", onInvChanged);
        EventManager.AddListener("EQUIPMENT_CHANGED", onEquipChanged);
        Refresh();
    }

    private void OnDisable()
    {
        if (onInvChanged != null) EventManager.RemoveListener("INVENTORY_UPDATED", onInvChanged);
        if (onEquipChanged != null) EventManager.RemoveListener("EQUIPMENT_CHANGED", onEquipChanged);
    }

    private void BuildUI()
    {
        root = GetComponent<RectTransform>();
        if (root == null) root = gameObject.AddComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        var bg = CreateImage(root, new Color(0f, 0f, 0f, 0.35f));
        bg.raycastTarget = false;

        safe = CreateSafeArea(root);

        var title = CreateText(safe, "_Title", "背包", new Vector2(0.5f, 0.96f), new Vector2(400f, 70f), 42, TextAnchor.MiddleCenter);

        var back = CreateButton(safe, "_Back", "返回", new Vector2(0.12f, 0.96f), new Vector2(220f, 86f));
        back.onClick.AddListener(() => OnClose?.Invoke());

        // 上方信息条
        statsText = CreateText(safe, "_Stats", "", new Vector2(0.50f, 0.90f), new Vector2(900f, 44f), 22, TextAnchor.MiddleCenter);
        invCountText = CreateText(safe, "_InvCount", "", new Vector2(0.90f, 0.90f), new Vector2(220f, 44f), 22, TextAnchor.MiddleRight);

        // 左：装备槽
        equipArea = CreateArea(safe, "_EquipArea", new Vector2(0.00f, 0.32f), new Vector2(0.42f, 0.88f));
        CreateAreaBackground(equipArea);
        CreateText(equipArea, "_EquipTitle", "已穿戴", new Vector2(0.5f, 0.94f), new Vector2(360f, 50f), 28, TextAnchor.MiddleCenter);
        BuildEquipSlots(equipArea);

        // 右：背包格
        invArea = CreateArea(safe, "_InvArea", new Vector2(0.42f, 0.32f), new Vector2(1.00f, 0.88f));
        CreateAreaBackground(invArea);
        CreateText(invArea, "_InvTitle", "背包", new Vector2(0.5f, 0.94f), new Vector2(360f, 50f), 28, TextAnchor.MiddleCenter);
        BuildInventoryGrid(invArea);

        // 底：详情区
        detailArea = CreateArea(safe, "_DetailArea", new Vector2(0.00f, 0.00f), new Vector2(1.00f, 0.30f));
        CreateAreaBackground(detailArea);
        CreateText(detailArea, "_DetailTitle", "详情", new Vector2(0.10f, 0.86f), new Vector2(220f, 44f), 26, TextAnchor.MiddleLeft);

        var iconGo = new GameObject("_DetailIcon");
        iconGo.transform.SetParent(detailArea, false);
        var iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.08f, 0.42f);
        iconRt.anchorMax = new Vector2(0.08f, 0.42f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.sizeDelta = new Vector2(120f, 120f);
        detailBorder = iconGo.AddComponent<Image>();
        detailBorder.color = new Color(1f, 1f, 1f, 0.25f);

        var innerIcon = new GameObject("Icon");
        innerIcon.transform.SetParent(iconGo.transform, false);
        var innerRt = innerIcon.AddComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero;
        innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(10f, 10f);
        innerRt.offsetMax = new Vector2(-10f, -10f);
        detailIcon = innerIcon.AddComponent<Image>();
        detailIcon.color = Color.white;
        detailIcon.raycastTarget = false;

        detailName = CreateText(detailArea, "_DetailName", "未选择物品", new Vector2(0.25f, 0.78f), new Vector2(760f, 44f), 28, TextAnchor.MiddleLeft);
        detailStats = CreateText(detailArea, "_DetailStats", "", new Vector2(0.25f, 0.52f), new Vector2(760f, 90f), 22, TextAnchor.UpperLeft);
        detailHint = CreateText(detailArea, "_DetailHint", "", new Vector2(0.25f, 0.24f), new Vector2(760f, 44f), 20, TextAnchor.MiddleLeft);
        detailHint.color = new Color(1f, 1f, 1f, 0.85f);

        detailEquipBtn = CreateButton(detailArea, "_Btn_Equip", "穿戴", new Vector2(0.76f, 0.70f), new Vector2(260f, 90f));
        detailSellBtn = CreateButton(detailArea, "_Btn_Sell", "出售", new Vector2(0.76f, 0.42f), new Vector2(260f, 90f));
        detailDropBtn = CreateButton(detailArea, "_Btn_Drop", "丢弃", new Vector2(0.76f, 0.14f), new Vector2(260f, 90f));

        ClearSelection();
        title.raycastTarget = false;
    }

    public void Refresh()
    {
        invSnapshot = new Dictionary<int, int>(InventoryService.GetInventorySnapshot());
        equippedSnapshot = new Dictionary<string, int>(InventoryService.GetEquippedSnapshot());

        UpdateEquipSlots();
        RebuildInventoryItems();
        UpdateTopSummary();
        UpdateInventoryGrid();
        UpdateDetail();
    }

    private void UpdateTopSummary()
    {
        float hp = 0f;
        float atk = 0f, def = 0f;

        // 优先运行态玩家（战斗内/可暂停打开背包）
        var player = GameObject.FindGameObjectWithTag("Player");
        var ps = player != null ? player.GetComponent<PlayerStats>() : null;
        if (ps != null)
        {
            hp = ps.GetStat(StatType.MaxHP);
            atk = ps.GetStat(StatType.Attack);
            def = ps.GetStat(StatType.Defense);
        }
        else
        {
            // 主菜单：无 Player 时用存档等级 + 装备加成推导总属性
            int level = 1;
            SaveSystem.EnsureLoaded();
            var save = SaveSystem.GetCached();
            if (save != null && save.player != null) level = Mathf.Max(1, save.player.level);

            // 与 PlayerStats 默认值保持一致（首版）
            const float BaseMaxHp = 150f;
            const float BaseAttack = 15f;
            const float BaseDefense = 10f;
            const float LevelGrowthRate = 0.07f;

            float factor = 1f + (level - 1) * LevelGrowthRate;
            float eqAtk = 0f, eqDef = 0f;
            if (ConfigManager.Instance != null && ConfigManager.Instance.EquipmentConfigs != null)
            {
                foreach (var kv in equippedSnapshot)
                {
                    int id = kv.Value;
                    if (id <= 0) continue;
                    if (!ConfigManager.Instance.EquipmentConfigs.TryGetValue(id, out var cfg) || cfg == null) continue;
                    eqAtk += cfg.attack_bonus;
                    eqDef += cfg.defense_bonus;
                }
            }

            hp = BaseMaxHp * factor;
            atk = BaseAttack * factor + eqAtk;
            def = BaseDefense * factor + eqDef;
        }

        if (statsText != null) statsText.text = $"总属性  HP: {hp:0}   Attack: {atk:0}   Defense: {def:0}";

        int used = invItems.Count;
        if (invCountText != null) invCountText.text = $"{used}/30";
    }

    private void BuildEquipSlots(RectTransform parent)
    {
        CreateEquipSlot(parent, "helmet", "头", new Vector2(0.5f, 0.78f));
        CreateEquipSlot(parent, "weapon", "武", new Vector2(0.25f, 0.56f));
        CreateEquipSlot(parent, "armor", "甲", new Vector2(0.5f, 0.56f));
        CreateEquipSlot(parent, "accessory", "饰", new Vector2(0.75f, 0.56f));
        CreateEquipSlot(parent, "pants", "裤", new Vector2(0.5f, 0.34f));
        CreateEquipSlot(parent, "boots", "靴", new Vector2(0.5f, 0.14f));
    }

    private void CreateEquipSlot(RectTransform parent, string slot, string shortLabel, Vector2 anchor)
    {
        var go = new GameObject($"Slot_{slot}");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(120f, 120f);

        var border = go.AddComponent<Image>();
        border.color = new Color(1f, 1f, 1f, 0.25f);

        var btn = go.AddComponent<Button>();
        go.AddComponent<UIButtonFeedback>();

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(go.transform, false);
        var irt = iconGo.AddComponent<RectTransform>();
        irt.anchorMin = Vector2.zero;
        irt.anchorMax = Vector2.one;
        irt.offsetMin = new Vector2(10f, 10f);
        irt.offsetMax = new Vector2(-10f, -10f);
        var icon = iconGo.AddComponent<Image>();
        icon.color = Color.white;
        icon.raycastTarget = false;

        var label = CreateText(rt, "_Label", shortLabel, new Vector2(0.5f, 0.5f), new Vector2(120f, 120f), 28, TextAnchor.MiddleCenter);
        label.color = new Color(1f, 1f, 1f, 0.65f);
        label.raycastTarget = false;

        var v = new EquipSlotView { slot = slot, btn = btn, border = border, icon = icon, label = label, equippedId = 0 };
        equipSlots[slot] = v;

        btn.onClick.AddListener(() =>
        {
            if (v.equippedId > 0) SelectEquipped(slot, v.equippedId);
            else ClearSelection();
        });
    }

    private void UpdateEquipSlots()
    {
        foreach (string slot in SlotOrder)
        {
            if (!equipSlots.TryGetValue(slot, out var v)) continue;
            int id = equippedSnapshot.TryGetValue(slot, out var eq) ? eq : 0;
            v.equippedId = id;

            if (id > 0 && TryGetEquipmentConfig(id, out var cfg))
            {
                v.label.text = "";
                bool selected = selectedEquippedSlot == slot && selectedItemId == id;
                v.border.color = selected ? new Color(1f, 0.85f, 0.2f, 1f) : GetQualityColor(cfg.quality);
                v.icon.enabled = true;
                v.icon.sprite = LoadSprite(cfg.icon_path);
                v.icon.color = v.icon.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0.15f);
            }
            else
            {
                bool selected = selectedEquippedSlot == slot;
                v.border.color = selected ? new Color(1f, 0.85f, 0.2f, 1f) : new Color(1f, 1f, 1f, 0.25f);
                v.icon.sprite = null;
                v.icon.enabled = false;
                v.label.text = GetSlotShort(slot);
            }
        }
    }

    private void BuildInventoryGrid(RectTransform parent)
    {
        float startX = 0.10f;
        float startY = 0.80f;
        float dx = 0.14f;
        float dy = 0.16f;

        int index = 0;
        for (int r = 0; r < 5; r++)
        {
            for (int c = 0; c < 6; c++)
            {
                var anchor = new Vector2(startX + c * dx, startY - r * dy);
                invSlots[index] = CreateInventorySlot(parent, index, anchor, new Vector2(120f, 120f));
                index++;
            }
        }
    }

    private SlotView CreateInventorySlot(RectTransform parent, int index, Vector2 anchor, Vector2 size)
    {
        var go = new GameObject($"InvSlot_{index:00}");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;

        var border = go.AddComponent<Image>();
        border.color = new Color(1f, 1f, 1f, 0.25f);

        var btn = go.AddComponent<Button>();
        go.AddComponent<UIButtonFeedback>();

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(go.transform, false);
        var irt = iconGo.AddComponent<RectTransform>();
        irt.anchorMin = Vector2.zero;
        irt.anchorMax = Vector2.one;
        irt.offsetMin = new Vector2(10f, 10f);
        irt.offsetMax = new Vector2(-10f, -10f);
        var icon = iconGo.AddComponent<Image>();
        icon.color = Color.white;
        icon.raycastTarget = false;

        var newTag = CreateText(rt, "_Tag", "新", new Vector2(0.82f, 0.86f), new Vector2(68f, 44f), 18, TextAnchor.MiddleCenter);
        newTag.color = new Color(1f, 0.25f, 0.25f, 1f);
        newTag.raycastTarget = false;

        var compareTag = CreateText(rt, "_Compare", "", new Vector2(0.18f, 0.86f), new Vector2(44f, 44f), 24, TextAnchor.MiddleCenter);
        compareTag.raycastTarget = false;

        var v = new SlotView
        {
            index = index,
            btn = btn,
            border = border,
            icon = icon,
            newTag = newTag,
            compareTag = compareTag,
            itemId = 0
        };

        btn.onClick.AddListener(() =>
        {
            if (v.itemId > 0) SelectInventory(v.index, v.itemId);
            else ClearSelection();
        });

        return v;
    }

    private void RebuildInventoryItems()
    {
        invItems.Clear();
        if (invSnapshot == null) return;

        foreach (var kv in invSnapshot)
        {
            int itemId = kv.Key;
            int count = kv.Value;
            if (itemId <= 0 || count <= 0) continue;
            for (int i = 0; i < count && invItems.Count < 30; i++)
                invItems.Add(itemId);
            if (invItems.Count >= 30) break;
        }

        invItems.Sort();
    }

    private void UpdateInventoryGrid()
    {
        for (int i = 0; i < invSlots.Length; i++)
        {
            int itemId = i < invItems.Count ? invItems[i] : 0;
            ApplySlot(invSlots[i], itemId);
        }
    }

    private void ApplySlot(SlotView v, int itemId)
    {
        if (v == null) return;
        v.itemId = itemId;
        bool has = itemId > 0;
        v.btn.interactable = has;

        if (!has)
        {
            v.border.color = new Color(1f, 1f, 1f, 0.18f);
            v.icon.sprite = null;
            v.icon.color = new Color(1f, 1f, 1f, 0.15f);
            v.icon.enabled = false;
            v.newTag.enabled = false;
            v.compareTag.text = "";
            return;
        }

        if (TryGetEquipmentConfig(itemId, out var cfg))
        {
            bool selected = selectedEquippedSlot == null && selectedInvIndex == v.index && selectedItemId == itemId;
            v.border.color = selected ? new Color(1f, 0.85f, 0.2f, 1f) : GetQualityColor(cfg.quality);
            v.icon.enabled = true;
            v.icon.sprite = LoadSprite(cfg.icon_path);
            v.icon.color = v.icon.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0.15f);

            bool isNew = !seenEquipmentIds.Contains(itemId);
            bool isUpgrade = false;
            if (!string.IsNullOrEmpty(cfg.type) && equippedSnapshot != null)
            {
                int equippedId = equippedSnapshot.TryGetValue(cfg.type, out var eq) ? eq : 0;
                if (equippedId > 0 && TryGetEquipmentConfig(equippedId, out var eqCfg))
                {
                    float a = Score(eqCfg);
                    float b = Score(cfg);
                    isUpgrade = b > a;
                }
            }

            // UI策划案 v3.2：右上角标签（新/可升级）
            if (isNew)
            {
                v.newTag.enabled = true;
                v.newTag.text = "新";
                v.newTag.color = new Color(1f, 0.25f, 0.25f, 1f);
            }
            else if (isUpgrade)
            {
                v.newTag.enabled = true;
                v.newTag.text = "可升级";
                v.newTag.color = new Color(0.25f, 1f, 0.25f, 1f);
            }
            else
            {
                v.newTag.enabled = false;
            }

            v.compareTag.text = "";
        }
        else
        {
            bool selected = selectedEquippedSlot == null && selectedInvIndex == v.index && selectedItemId == itemId;
            v.border.color = selected ? new Color(1f, 0.85f, 0.2f, 1f) : new Color(1f, 1f, 1f, 0.25f);
            v.icon.enabled = false;
            v.newTag.enabled = false;
            v.compareTag.text = "";
        }
    }

    private void SelectInventory(int index, int itemId)
    {
        selectedItemId = itemId;
        selectedEquippedSlot = null;
        selectedInvIndex = index;
        seenEquipmentIds.Add(itemId);
        UpdateInventoryGrid(); // 刷新“新”标签
        UpdateDetail();
    }

    private void SelectEquipped(string slot, int equippedId)
    {
        selectedItemId = equippedId;
        selectedEquippedSlot = slot;
        selectedInvIndex = -1;
        UpdateDetail();
    }

    private void ClearSelection()
    {
        selectedItemId = 0;
        selectedEquippedSlot = null;
        selectedInvIndex = -1;
        UpdateDetail();
    }

    private void UpdateDetail()
    {
        if (detailName == null || detailStats == null) return;

        if (selectedItemId <= 0 || !TryGetEquipmentConfig(selectedItemId, out var cfg))
        {
            detailName.text = "未选择物品";
            detailStats.text = "点击右侧背包格或左侧装备槽查看详情";
            detailHint.text = "";
            detailBorder.color = new Color(1f, 1f, 1f, 0.25f);
            detailIcon.sprite = null;
            detailIcon.enabled = false;
            SetDetailButtons(false, false, false);
            return;
        }

        bool isEquipped = !string.IsNullOrEmpty(selectedEquippedSlot);
        int countInBag = invSnapshot != null && invSnapshot.TryGetValue(selectedItemId, out var c) ? c : 0;

        detailName.text = $"{cfg.name}（{GetQualityName(cfg.quality)}）";
        detailBorder.color = GetQualityColor(cfg.quality);
        detailIcon.enabled = true;
        detailIcon.sprite = LoadSprite(cfg.icon_path);
        detailIcon.color = detailIcon.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0.15f);

        string type = string.IsNullOrEmpty(cfg.type) ? "未知槽位" : cfg.type;
        detailStats.text =
            $"槽位：{type}\n" +
            $"HP +0\n" +
            $"Attack +{cfg.attack_bonus:0}\n" +
            $"Defense +{cfg.defense_bonus:0}\n" +
            $"{(isEquipped ? "状态：已穿戴" : $"背包数量：{countInBag}")}";

        if (detailEquipBtn != null)
        {
            var t = detailEquipBtn.GetComponentInChildren<Text>();
            if (t != null) t.text = isEquipped ? "卸下" : "穿戴";
            detailEquipBtn.onClick.RemoveAllListeners();
            detailEquipBtn.onClick.AddListener(() =>
            {
                if (isEquipped)
                {
                    InventoryService.TryUnequip(selectedEquippedSlot);
                    if (UIManager.Instance != null) UIManager.Instance.ShowToast("已卸下");
                }
                else
                {
                    bool ok = InventoryService.TryEquip(selectedItemId);
                    if (UIManager.Instance != null) UIManager.Instance.ShowToast(ok ? "已穿戴" : "穿戴失败");
                }
            });
        }

        if (detailSellBtn != null)
        {
            detailSellBtn.onClick.RemoveAllListeners();
            detailSellBtn.onClick.AddListener(() =>
            {
                if (isEquipped)
                {
                    if (UIManager.Instance != null) UIManager.Instance.ShowToast("请先卸下再出售");
                    return;
                }
                if (countInBag <= 0)
                {
                    if (UIManager.Instance != null) UIManager.Instance.ShowToast("背包中无此物品");
                    return;
                }
                int unit = EconomyRules.GetEquipmentSellPrice(selectedItemId);
                if (UIManager.Instance != null) UIManager.Instance.ShowSellConfirmation(selectedItemId, 1, unit);
            });
        }

        if (detailDropBtn != null)
        {
            detailDropBtn.onClick.RemoveAllListeners();
            detailDropBtn.onClick.AddListener(() =>
            {
                if (isEquipped)
                {
                    if (UIManager.Instance != null) UIManager.Instance.ShowToast("请先卸下再丢弃");
                    return;
                }
                if (countInBag <= 0)
                {
                    if (UIManager.Instance != null) UIManager.Instance.ShowToast("背包中无此物品");
                    return;
                }

                string n = string.IsNullOrEmpty(cfg.name) ? "物品" : cfg.name;
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowConfirm(
                        "丢弃确认",
                        $"是否丢弃 {n} x1 ？\n（丢弃不会获得金币）",
                        "丢弃",
                        "取消",
                        () =>
                        {
                            bool ok = InventoryService.TryDrop(selectedItemId, 1);
                            if (UIManager.Instance != null) UIManager.Instance.ShowToast(ok ? "已丢弃" : "丢弃失败");
                        });
                }
            });
        }

        detailHint.text = "";
        SetDetailButtons(true, true, true);
    }

    private void SetDetailButtons(bool equip, bool sell, bool drop)
    {
        if (detailEquipBtn != null) detailEquipBtn.interactable = equip;
        if (detailSellBtn != null) detailSellBtn.interactable = sell;
        if (detailDropBtn != null) detailDropBtn.interactable = drop;
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

    private static RectTransform CreateArea(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(10f, 10f);
        rt.offsetMax = new Vector2(-10f, -10f);
        return rt;
    }

    private static void CreateAreaBackground(RectTransform rt)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.color = Color.white;
        img.raycastTarget = false;
        UITheme.ApplyImageSprite(img, "UI/Common/UI_Panel_Background", preserveAspect: true);
    }

    private static Image CreateImage(RectTransform parent, Color color)
    {
        var go = new GameObject("_BG");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
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
        UITheme.ApplySpriteSwapButton(btn,
            size.x >= 240f ? "UI_Button_Big_Normal" : "UI_Button_Small_Normal",
            size.x >= 240f ? "UI_Button_Big_Pressed" : "UI_Button_Small_Pressed",
            size.x >= 240f ? "UI_Button_Big_Disabled" : "UI_Button_Small_Disabled");

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
        text.fontSize = 24;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        return btn;
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

    private static bool TryGetEquipmentConfig(int equipmentId, out EquipmentConfig cfg)
    {
        cfg = null;
        if (ConfigManager.Instance == null || ConfigManager.Instance.EquipmentConfigs == null) return false;
        return ConfigManager.Instance.EquipmentConfigs.TryGetValue(equipmentId, out cfg) && cfg != null;
    }

    private static Sprite LoadSprite(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        return Resources.Load<Sprite>(path);
    }

    private static float Score(EquipmentConfig c)
    {
        if (c == null) return 0f;
        return c.attack_bonus + c.defense_bonus;
    }

    private static string GetSlotShort(string slot) => slot switch
    {
        "helmet" => "头",
        "weapon" => "武",
        "armor" => "甲",
        "accessory" => "饰",
        "pants" => "裤",
        "boots" => "靴",
        _ => "装"
    };

    private static string GetQualityName(string q) => q switch
    {
        "common" => "普通",
        "uncommon" => "精良",
        "rare" => "稀有",
        "epic" => "史诗",
        "legendary" => "传说",
        _ => "未知"
    };

    private static Color GetQualityColor(string q)
    {
        // 首版按色值区分即可（品质边框/名字颜色）
        return q switch
        {
            "common" => new Color(0.75f, 0.75f, 0.75f, 0.95f),
            "uncommon" => new Color(0.25f, 0.85f, 0.35f, 0.95f),
            "rare" => new Color(0.20f, 0.55f, 1.00f, 0.95f),
            "epic" => new Color(0.70f, 0.35f, 1.00f, 0.95f),
            "legendary" => new Color(1.00f, 0.60f, 0.15f, 0.95f),
            _ => new Color(1f, 1f, 1f, 0.25f)
        };
    }

    private class SlotView
    {
        public int index;
        public Button btn;
        public Image border;
        public Image icon;
        public Text newTag;
        public Text compareTag;
        public int itemId;
    }

    private class EquipSlotView
    {
        public string slot;
        public Button btn;
        public Image border;
        public Image icon;
        public Text label;
        public int equippedId;
    }
}

