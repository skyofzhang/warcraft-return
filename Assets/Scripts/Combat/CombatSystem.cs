// 依据：程序基础知识库 5.2、5.9 第四层；GDD 3.1.2 伤害公式
using UnityEngine;

/// <summary>
/// 执行一次攻击的数值计算，复用 GDD 伤害公式。不负责移动与逻辑，仅计算伤害。
/// </summary>
public static class CombatSystem
{
    private const float DefenseK = 100f; // GDD: Defense / (Defense + K)

    /// <summary>
    /// 计算一次攻击的最终伤害与是否暴击。
    /// 公式：基础伤害 = Attack * 技能倍率；减伤系数 = Defense/(Defense+K)；最终伤害 = 基础*(1-减伤)；暴击则 *CritDamage。
    /// </summary>
    public static (float finalDamage, bool isCrit) CalculateDamage(
        IStatsProvider attacker,
        IStatsProvider defender,
        float skillMultiplier = 1f)
    {
        if (attacker == null || defender == null) return (0f, false);

        float attack = attacker.GetStat(StatType.Attack);
        float defense = defender.GetStat(StatType.Defense);
        float critChance = attacker.GetStat(StatType.CritChance);
        float critDamage = attacker.GetStat(StatType.CritDamage);

        float baseDamage = attack * skillMultiplier;
        float reduction = defense / (defense + DefenseK);
        float finalDamage = baseDamage * (1f - reduction);

        bool isCrit = Random.value < critChance;
        if (isCrit)
            finalDamage *= Mathf.Max(1f, critDamage);

        finalDamage = Mathf.Max(1f, Mathf.Floor(finalDamage)); // 至少 1 点伤害
        return (finalDamage, isCrit);
    }
}
