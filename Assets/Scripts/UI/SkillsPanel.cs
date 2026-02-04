// UI-07 技能（一级界面）
// 首版：展示 2 个技能（来自 SkillConfigs.json），支持金币升级技能等级（存档持久化），战斗倍率随等级生效。
using System;
using UnityEngine;
using UnityEngine.UI;

public class SkillsPanel : MonoBehaviour
{
    private Text goldText;
    private Text hintText;
    private SkillRow row0;
    private SkillRow row1;

    private Action<object> onGoldChanged;

    // UI策划案 v3.2：金币数字平滑变化（0.5s）
    private int targetGold;
    private float displayedGold;
    private float goldVel;
    private bool goldInited;

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

        CreateText(safe, "_Text_Title", "技能", new Vector2(0.5f, 0.92f), new Vector2(500f, 70f), 42, TextAnchor.MiddleCenter);

        CreateImage(safe, "_GoldIcon", "UI/Common/UI_Icon_Gold", new Vector2(0.72f, 0.82f), new Vector2(56f, 56f), true);
        goldText = CreateText(safe, "_Text_Gold", "0", new Vector2(0.80f, 0.82f), new Vector2(240f, 56f), 28, TextAnchor.MiddleLeft);

        row0 = CreateSkillRow(safe, 0, new Vector2(0.5f, 0.64f));
        row1 = CreateSkillRow(safe, 1, new Vector2(0.5f, 0.38f));

        hintText = CreateText(safe, "_Text_Hint", "", new Vector2(0.5f, 0.20f), new Vector2(980f, 44f), 22, TextAnchor.MiddleCenter);
        hintText.color = new Color(1f, 1f, 1f, 0.9f);

        var back = CreateButton(safe, "_Button_Back", "返回", new Vector2(0.5f, 0.08f), new Vector2(420f, 120f));
        back.onClick.AddListener(() =>
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowMainMenuHome();
        });

        Refresh();
    }

    private void OnEnable()
    {
        onGoldChanged = _ => Refresh();
        EventManager.AddListener("GOLD_CHANGED", onGoldChanged);
        Refresh();
    }

    private void OnDisable()
    {
        if (onGoldChanged != null) EventManager.RemoveListener("GOLD_CHANGED", onGoldChanged);
    }

    private void Update()
    {
        if (goldText == null) return;
        float dt = Time.unscaledDeltaTime;
        displayedGold = Mathf.SmoothDamp(displayedGold, targetGold, ref goldVel, 0.5f, Mathf.Infinity, dt);
        goldText.text = Mathf.RoundToInt(displayedGold).ToString();
    }

    private void Refresh()
    {
        SaveSystem.EnsureLoaded();
        var save = SaveSystem.GetCached();
        int gold = save?.player != null ? save.player.gold : 0;
        targetGold = gold;
        if (!goldInited)
        {
            goldInited = true;
            displayedGold = targetGold;
        }

        RefreshRow(row0, "SK001");
        RefreshRow(row1, "SK002");
    }

    private void RefreshRow(SkillRow row, string skillId)
    {
        if (row == null) return;

        var cfg = (ConfigManager.Instance != null && ConfigManager.Instance.SkillConfigs != null &&
                   ConfigManager.Instance.SkillConfigs.TryGetValue(skillId, out var c)) ? c : null;

        string name = cfg != null && !string.IsNullOrEmpty(cfg.skill_name) ? cfg.skill_name : skillId;
        float cd = cfg != null ? cfg.cooldown : (skillId == "SK001" ? 8f : 5f);
        float mult = cfg != null ? cfg.damage_multiplier : (skillId == "SK001" ? 0.8f : 1.2f);

        int lv = GetSkillLevelFromSave(skillId);
        int cost = GetUpgradeCost(lv);

        if (row.nameText != null) row.nameText.text = $"{name} ({skillId})";
        if (row.descText != null) row.descText.text = $"冷却 {cd:F1}s  伤害倍率 {mult:F2}";
        if (row.levelText != null) row.levelText.text = $"Lv.{lv}";
        if (row.costText != null) row.costText.text = $"升级消耗：{cost}";

        if (row.upgradeBtn != null)
        {
            row.upgradeBtn.onClick.RemoveAllListeners();
            row.upgradeBtn.onClick.AddListener(() => RequestUpgrade(skillId));
        }
    }

    private void RequestUpgrade(string skillId)
    {
        SaveSystem.EnsureLoaded();
        var save = SaveSystem.GetCached();
        if (save == null || save.player == null) return;

        int lv = GetSkillLevelFromSave(skillId);
        int cost = GetUpgradeCost(lv);

        string skillName = GetSkillName(skillId);
        bool canAfford = save.player.gold >= cost;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowSkillUpgradeConfirmation(
                skillId,
                skillName,
                lv,
                lv + 1,
                cost,
                canAfford,
                () => ExecuteUpgrade(skillId, lv, cost)
            );
        }
        else
        {
            if (!canAfford)
            {
                if (hintText != null) hintText.text = "金币不足";
                return;
            }
            ExecuteUpgrade(skillId, lv, cost);
        }
    }

    private void ExecuteUpgrade(string skillId, int currentLv, int cost)
    {
        SaveSystem.EnsureLoaded();
        var save = SaveSystem.GetCached();
        if (save == null || save.player == null) return;
        if (save.player.gold < cost)
        {
            if (hintText != null) hintText.text = "金币不足";
            if (UIManager.Instance != null) UIManager.Instance.ShowToast("金币不足");
            return;
        }

        save.player.gold -= cost;
        SetSkillLevelToSave(skillId, currentLv + 1);
        SaveSystem.SaveNow();

        EventManager.TriggerEvent("GOLD_CHANGED", new object[] { save.player.gold, -cost });
        if (hintText != null) hintText.text = "升级成功";
        if (UIManager.Instance != null) UIManager.Instance.ShowToast("升级成功");

        // 若当前有运行态玩家，同步到 PlayerStats（避免本局升级后不生效）
        var player = GameObject.FindGameObjectWithTag("Player");
        var ps = player != null ? player.GetComponent<PlayerStats>() : null;
        if (ps != null)
        {
            if (skillId == "SK001") ps.SetSkillLevel(0, currentLv + 1);
            else if (skillId == "SK002") ps.SetSkillLevel(1, currentLv + 1);
        }

        Refresh();
    }

    private static string GetSkillName(string skillId)
    {
        if (ConfigManager.Instance != null && ConfigManager.Instance.SkillConfigs != null &&
            ConfigManager.Instance.SkillConfigs.TryGetValue(skillId, out var cfg) && cfg != null &&
            !string.IsNullOrEmpty(cfg.skill_name))
            return cfg.skill_name;
        return skillId;
    }

    private static int GetUpgradeCost(int currentLevel)
    {
        // 首版成本规则：100 * 当前等级（可在 M3 配置化）
        int lv = Mathf.Max(1, currentLevel);
        return Mathf.Clamp(100 * lv, 100, 999999);
    }

    private static int GetSkillLevelFromSave(string skillId)
    {
        SaveSystem.EnsureLoaded();
        var save = SaveSystem.GetCached();
        if (save?.player == null) return 1;
        if (skillId == "SK001") return Mathf.Max(1, save.player.skill_lv_sk001);
        if (skillId == "SK002") return Mathf.Max(1, save.player.skill_lv_sk002);
        return 1;
    }

    private static void SetSkillLevelToSave(string skillId, int lv)
    {
        SaveSystem.EnsureLoaded();
        var save = SaveSystem.GetCached();
        if (save?.player == null) return;
        int v = Mathf.Max(1, lv);
        if (skillId == "SK001") save.player.skill_lv_sk001 = v;
        else if (skillId == "SK002") save.player.skill_lv_sk002 = v;
    }

    private class SkillRow
    {
        public Text nameText;
        public Text descText;
        public Text levelText;
        public Text costText;
        public Button upgradeBtn;
    }

    private SkillRow CreateSkillRow(RectTransform parent, int index, Vector2 anchorPos)
    {
        var row = new SkillRow();

        var card = new GameObject($"_Skill_{index}");
        card.transform.SetParent(parent, false);
        var rt = card.AddComponent<RectTransform>();
        rt.anchorMin = anchorPos;
        rt.anchorMax = anchorPos;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(900f, 200f);
        var img = card.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.25f);

        row.nameText = CreateText(rt, "_Text_Name", "技能", new Vector2(0.08f, 0.72f), new Vector2(640f, 44f), 28, TextAnchor.MiddleLeft);
        row.descText = CreateText(rt, "_Text_Desc", "", new Vector2(0.08f, 0.46f), new Vector2(740f, 44f), 22, TextAnchor.MiddleLeft);
        row.levelText = CreateText(rt, "_Text_Level", "Lv.1", new Vector2(0.08f, 0.22f), new Vector2(240f, 44f), 22, TextAnchor.MiddleLeft);
        row.costText = CreateText(rt, "_Text_Cost", "", new Vector2(0.36f, 0.22f), new Vector2(420f, 44f), 22, TextAnchor.MiddleLeft);
        row.upgradeBtn = CreateButton(rt, "_Button_Upgrade", "升级", new Vector2(0.88f, 0.5f), new Vector2(220f, 110f));

        return row;
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

