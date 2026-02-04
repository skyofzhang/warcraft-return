// 依据：程序知识库（Unity）v2.1.1 3.1（显式枚举值 + 分组注释）
public enum StatType
{
    // ========== 基础属性 ==========
    /// <summary>当前生命值</summary>
    HP = 0,

    /// <summary>最大生命值</summary>
    MaxHP = 1,

    /// <summary>攻击力</summary>
    Attack = 2,

    /// <summary>防御力</summary>
    Defense = 3,

    /// <summary>移动速度（米/秒）</summary>
    MoveSpeed = 4,

    /// <summary>攻击速度（攻击间隔秒数，越小越快）</summary>
    AttackSpeed = 5,

    // ========== 暴击属性 ==========
    /// <summary>暴击率（0-1小数，如0.15表示15%）</summary>
    CritChance = 10,

    /// <summary>暴击伤害系数（如1.5表示150%伤害）</summary>
    CritDamage = 11,

    // ========== 成长属性 ==========
    /// <summary>当前等级</summary>
    Level = 20,

    /// <summary>当前经验值（当前等级进度）</summary>
    CurrentExp = 21,

    /// <summary>升级所需经验（当前等级）</summary>
    NextLevelExp = 22,

    // ========== 资源属性 ==========
    /// <summary>金币数量</summary>
    Gold = 30,

    // ========== 特殊属性（预留） ==========
    /// <summary>生命偷取（0-1小数）</summary>
    LifeSteal = 40,

    /// <summary>伤害减免（0-1小数）</summary>
    DamageReduction = 41,
}
