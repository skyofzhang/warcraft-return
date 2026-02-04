// UI-06 角色属性（一级界面）
// 首版：按 PlayerStats 的同款公式推导显示（主菜单无 Player 时也可展示）。
using UnityEngine;
using UnityEngine.UI;

public class CharacterAttributesPanel : MonoBehaviour
{
    private Text jobText;
    private Text levelText;
    private Text hpText;
    private Text atkText;
    private Text defText;
    private Text moveText;
    private Text critText;
    private Text hintText;

    // 与 PlayerStats 保持一致的基础值（首版显示用）
    private const float BaseMaxHp = 150f;
    private const float BaseAttack = 15f;
    private const float BaseDefense = 10f;
    private const float MoveSpeed = 4f;
    private const float CritChance = 0.1f;
    // 程序知识库 v2.1.1：暴伤为系数（如 1.5 表示 150%）
    private const float CritDamage = 1.5f;
    private const float LevelGrowthRate = 0.07f;

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

        CreateText(safe, "_Text_Title", "角色属性", new Vector2(0.5f, 0.92f), new Vector2(500f, 70f), 42, TextAnchor.MiddleCenter);

        levelText = CreateText(safe, "_Text_Level", "Lv.1", new Vector2(0.18f, 0.78f), new Vector2(260f, 44f), 28, TextAnchor.MiddleLeft);
        jobText = CreateText(safe, "_Text_Job", "职业：猎人", new Vector2(0.18f, 0.72f), new Vector2(400f, 44f), 26, TextAnchor.MiddleLeft);
        hpText = CreateText(safe, "_Text_HP", "生命：0/0", new Vector2(0.18f, 0.62f), new Vector2(600f, 44f), 26, TextAnchor.MiddleLeft);
        atkText = CreateText(safe, "_Text_ATK", "攻击：0", new Vector2(0.18f, 0.52f), new Vector2(600f, 44f), 26, TextAnchor.MiddleLeft);
        defText = CreateText(safe, "_Text_DEF", "防御：0", new Vector2(0.18f, 0.42f), new Vector2(600f, 44f), 26, TextAnchor.MiddleLeft);
        moveText = CreateText(safe, "_Text_Move", "移速：0", new Vector2(0.18f, 0.32f), new Vector2(600f, 44f), 26, TextAnchor.MiddleLeft);
        critText = CreateText(safe, "_Text_Crit", "暴击：0", new Vector2(0.18f, 0.22f), new Vector2(900f, 44f), 26, TextAnchor.MiddleLeft);

        hintText = CreateText(safe, "_Text_Hint", "提示：显示为“当前存档/运行态”的最终属性（含装备加成）。", new Vector2(0.5f, 0.16f), new Vector2(980f, 40f), 22, TextAnchor.MiddleCenter);
        hintText.color = new Color(1f, 1f, 1f, 0.85f);

        var back = CreateButton(safe, "_Button_Back", "返回", new Vector2(0.5f, 0.08f), new Vector2(420f, 120f));
        back.onClick.AddListener(() =>
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowMainMenuHome();
        });
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        int level = 1;
        string job = "猎人";
        float curHp = 0f;
        float maxHp = 0f;
        float atk = 0f;
        float def = 0f;
        float move = MoveSpeed;
        float critChance = CritChance;
        float critDmg = CritDamage;

        var player = GameObject.FindGameObjectWithTag("Player");
        var ps = player != null ? player.GetComponent<PlayerStats>() : null;
        if (ps != null)
        {
            // 运行态：直接读取最终值（含装备加成）
            level = ps.Level;
            job = ps.ProfessionName;
            curHp = ps.CurrentHp;
            maxHp = ps.MaxHp;
            atk = ps.GetStat(StatType.Attack);
            def = ps.GetStat(StatType.Defense);
            move = ps.GetStat(StatType.MoveSpeed);
            critChance = ps.GetStat(StatType.CritChance);
            critDmg = ps.GetStat(StatType.CritDamage);
        }
        else
        {
            SaveSystem.EnsureLoaded();
            var save = SaveSystem.GetCached();
            if (save != null && save.player != null) level = Mathf.Max(1, save.player.level);
            if (save != null && save.player != null && !string.IsNullOrEmpty(save.player.profession)) job = save.player.profession;

            // 主菜单：用存档等级推导基础值 + 已穿戴装备加成
            float factor = 1f + (level - 1) * LevelGrowthRate;
            float baseHp = BaseMaxHp * factor;
            float baseAtk = BaseAttack * factor;
            float baseDef = BaseDefense * factor;

            float eqAtk = 0f;
            float eqDef = 0f;
            if (save != null && save.equipment != null && save.equipment.equipped != null &&
                ConfigManager.Instance != null && ConfigManager.Instance.EquipmentConfigs != null)
            {
                for (int i = 0; i < save.equipment.equipped.Count; i++)
                {
                    var p = save.equipment.equipped[i];
                    if (p == null) continue;
                    int id = p.value;
                    if (id <= 0) continue;
                    if (!ConfigManager.Instance.EquipmentConfigs.TryGetValue(id, out var cfg) || cfg == null) continue;
                    eqAtk += cfg.attack_bonus;
                    eqDef += cfg.defense_bonus;
                }
            }

            curHp = baseHp;
            maxHp = baseHp;
            atk = baseAtk + eqAtk;
            def = baseDef + eqDef;
        }

        if (levelText != null) levelText.text = $"Lv.{level}";
        if (jobText != null) jobText.text = $"职业：{job}";
        if (hpText != null) hpText.text = $"生命：{curHp:F0}/{maxHp:F0}";
        if (atkText != null) atkText.text = $"攻击：{atk:F0}";
        if (defText != null) defText.text = $"防御：{def:F0}";
        if (moveText != null) moveText.text = $"移速：{move:F1}";
        if (critText != null) critText.text = $"暴击：{critChance:P0}  暴伤：{critDmg:P0}";
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

