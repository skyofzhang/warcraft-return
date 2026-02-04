// 依据：程序基础知识库 5.2、5.3、5.9 第二层；GDD 3.3 成长系统、6.5 数值护栏
using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour, IStatsProvider
{
    [Header("角色信息（UI显示）")]
    [SerializeField] private string professionName = "猎人";

    [Header("基础属性（1级数值）")]
    [SerializeField] private float baseMaxHp = 150f;
    [SerializeField] private float baseAttack = 15f;
    [SerializeField] private float baseDefense = 10f;
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float attackSpeed = 1f;
    [SerializeField] private float critChance = 0.1f;
    // 程序知识库 v2.1.1：CritDamage 为系数（如 1.5 表示 150% 伤害）
    [SerializeField] private float critDamage = 1.5f;

    /// <summary>每级属性增长比例，符合 GDD：初始值的 5%～10%，此处取 7%。</summary>
    private const float LevelGrowthRate = 0.07f;
    /// <summary>每级所需经验（简化）。</summary>
    private const int ExpPerLevel = 100;

    private float currentHp;
    private float maxHp;
    private float attack;
    private float defense;
    private int gold;
    private int exp;
    private int level = 1;
    // 关卡进度
    private int unlockedLevelId = 1;
    // 治疗瓶（首版：初始3，恢复30%HP）
    private int potionCount = 3;
    // 技能等级（首版2个技能：SK001/SK002）
    private int skillLvSk001 = 1;
    private int skillLvSk002 = 1;
    /// <summary>本局获得的金币（用于失败保留比例结算，GDD 10.5）。</summary>
    private int sessionGold;
    /// <summary>本局获得的经验（用于失败保留比例结算，GDD 10.5）。</summary>
    private int sessionExp;

    /// <summary>装备提供的攻击力加成，由 EquipmentManager 设置。</summary>
    private float equipmentAttackBonus;
    /// <summary>装备提供的防御力加成，由 EquipmentManager 设置。</summary>
    private float equipmentDefenseBonus;

    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;
    public string ProfessionName => string.IsNullOrEmpty(professionName) ? "猎人" : professionName;
    public int Gold => gold;
    public int Exp => exp;
    public int Level => level;
    public int UnlockedLevelId => Mathf.Max(1, unlockedLevelId);
    public int PotionCount => Mathf.Max(0, potionCount);
    public int SkillLvSk001 => skillLvSk001;
    public int SkillLvSk002 => skillLvSk002;
    /// <summary>当前等级经验进度（0～ExpPerLevel）。</summary>
    public int ExpInCurrentLevel => exp - (level - 1) * ExpPerLevel;

    private void Awake()
    {
        sessionGold = 0;
        sessionExp = 0;
        unlockedLevelId = 1;
        potionCount = 3;
        skillLvSk001 = 1;
        skillLvSk002 = 1;
        RecomputeLevelStats();
        currentHp = maxHp;
    }

    /// <summary>根据等级重算 maxHp、attack、defense（不含装备加成）。</summary>
    private void RecomputeLevelStats()
    {
        float factor = 1f + (level - 1) * LevelGrowthRate;
        maxHp = baseMaxHp * factor;
        attack = baseAttack * factor;
        defense = baseDefense * factor;
    }

    public float GetStat(StatType type)
    {
        switch (type)
        {
            case StatType.HP: return currentHp;
            case StatType.MaxHP: return maxHp;
            case StatType.Attack: return attack + equipmentAttackBonus;
            case StatType.Defense: return defense + equipmentDefenseBonus;
            case StatType.MoveSpeed: return moveSpeed;
            case StatType.AttackSpeed: return attackSpeed;
            case StatType.CritChance: return critChance;
            case StatType.CritDamage: return critDamage;
            case StatType.Level: return level;
            case StatType.CurrentExp: return ExpInCurrentLevel;
            case StatType.NextLevelExp: return ExpPerLevel;
            case StatType.Gold: return gold;
            case StatType.LifeSteal: return 0f; // 预留
            case StatType.DamageReduction: return 0f; // 预留
            default: return 0f;
        }
    }

    public void ModifyStat(StatType type, float delta)
    {
        switch (type)
        {
            case StatType.HP:
                currentHp = Mathf.Clamp(currentHp + delta, 0f, maxHp);
                EventManager.TriggerEvent("HEALTH_CHANGED", new object[] { currentHp, maxHp });
                break;
            default:
                break;
        }
    }

    /// <summary>由 EquipmentManager 调用，设置装备带来的攻击/防御加成。UI 刷新由 EQUIPMENT_CHANGED 驱动。</summary>
    public void SetEquipmentBonus(float attackBonus, float defenseBonus)
    {
        equipmentAttackBonus = attackBonus;
        equipmentDefenseBonus = defenseBonus;
    }

    public void TakeDamage(float damage)
    {
        ModifyStat(StatType.HP, -damage);
    }

    public void AddGold(int amount)
    {
        gold += amount;
        sessionGold += amount;
        EventManager.TriggerEvent("GOLD_CHANGED", new object[] { gold, amount });
    }

    public void AddExp(int amount)
    {
        exp += amount;
        sessionExp += amount;
        EventManager.TriggerEvent("EXP_GAINED", amount);
        int newLevel = 1 + exp / ExpPerLevel;
        if (newLevel > level)
        {
            float oldMaxHp = maxHp;
            level = newLevel;
            RecomputeLevelStats();
            float ratio = oldMaxHp > 0 ? currentHp / oldMaxHp : 1f;
            currentHp = Mathf.Clamp(maxHp * ratio, 1f, maxHp);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX_LevelUp();
            EventManager.TriggerEvent("LEVEL_UP", new object[] { level, 0 });
        }
    }

    /// <summary>失败结算时按 game_config 比例保留本局金/经验，扣除未保留部分（GDD 2.2、10.5；开发计划 3.4）。返回 (保留金币, 保留经验) 供 UI 展示。</summary>
    public (int retainedGold, int retainedExp) ApplyFailureRetain()
    {
        if (ConfigManager.Instance?.GameConfig == null)
        {
            sessionGold = 0;
            sessionExp = 0;
            return (0, 0);
        }
        var cfg = ConfigManager.Instance.GameConfig;
        int retainedGold = Mathf.RoundToInt(sessionGold * cfg.gold_retain_ratio);
        int retainedExp = Mathf.RoundToInt(sessionExp * cfg.exp_retain_ratio);
        gold = gold - sessionGold + retainedGold;
        exp = exp - sessionExp + retainedExp;
        sessionGold = 0;
        sessionExp = 0;
        level = Mathf.Max(1, 1 + exp / ExpPerLevel);
        RecomputeLevelStats();
        currentHp = Mathf.Min(currentHp, maxHp);
        EventManager.TriggerEvent("GOLD_CHANGED", new object[] { gold, 0 });
        return (retainedGold, retainedExp);
    }

    public float GetBaseAttack() => attack;
    public float GetBaseDefense() => defense;

    /// <summary>导出持久化存档数据（P0）。</summary>
    public PlayerSaveData ExportSaveData()
    {
        return new PlayerSaveData
        {
            profession = ProfessionName,
            gold = gold,
            exp = exp,
            level = level,
            unlocked_level_id = Mathf.Max(1, unlockedLevelId),
            potion_count = Mathf.Max(0, potionCount),
            skill_lv_sk001 = Mathf.Max(1, skillLvSk001),
            skill_lv_sk002 = Mathf.Max(1, skillLvSk002)
        };
    }

    /// <summary>导入持久化存档数据（P0）。注意：会重算等级属性并重置本局统计。</summary>
    public void ImportSaveData(PlayerSaveData data)
    {
        if (data == null) return;
        professionName = string.IsNullOrEmpty(data.profession) ? "猎人" : data.profession;
        gold = Mathf.Max(0, data.gold);
        exp = Mathf.Max(0, data.exp);
        level = Mathf.Max(1, data.level > 0 ? data.level : (1 + exp / ExpPerLevel));
        unlockedLevelId = Mathf.Max(1, data.unlocked_level_id);
        potionCount = Mathf.Max(0, data.potion_count);
        skillLvSk001 = Mathf.Max(1, data.skill_lv_sk001);
        skillLvSk002 = Mathf.Max(1, data.skill_lv_sk002);
        sessionGold = 0;
        sessionExp = 0;
        RecomputeLevelStats();
        currentHp = maxHp;
        EventManager.TriggerEvent("GOLD_CHANGED", new object[] { gold, 0 });
        EventManager.TriggerEvent("HEALTH_CHANGED", new object[] { currentHp, maxHp });
        EventManager.TriggerEvent("POTION_CHANGED", new object[] { potionCount, 0 });
        EventManager.TriggerEvent("LEVEL_UNLOCKED", unlockedLevelId);
    }

    public int GetSkillLevel(int skillIndex)
    {
        if (skillIndex == 0) return Mathf.Max(1, skillLvSk001);
        if (skillIndex == 1) return Mathf.Max(1, skillLvSk002);
        return 1;
    }

    public void SetSkillLevel(int skillIndex, int levelValue)
    {
        int lv = Mathf.Max(1, levelValue);
        if (skillIndex == 0) skillLvSk001 = lv;
        else if (skillIndex == 1) skillLvSk002 = lv;
    }

    public void UnlockLevel(int levelId)
    {
        int lv = Mathf.Clamp(levelId, 1, 10);
        if (lv <= unlockedLevelId) return;
        unlockedLevelId = lv;
        EventManager.TriggerEvent("LEVEL_UNLOCKED", unlockedLevelId);
    }

    public void AddPotion(int count)
    {
        if (count == 0) return;
        int old = potionCount;
        potionCount = Mathf.Clamp(potionCount + count, 0, 99);
        EventManager.TriggerEvent("POTION_CHANGED", new object[] { potionCount, potionCount - old });
    }

    public bool TryUsePotion(float healRatio = 0.3f)
    {
        if (potionCount <= 0) return false;
        healRatio = Mathf.Clamp01(healRatio);
        potionCount -= 1;
        float heal = maxHp * healRatio;
        ModifyStat(StatType.HP, heal);
        EventManager.TriggerEvent("POTION_CHANGED", new object[] { potionCount, -1 });
        return true;
    }
}
