// 经济规则（首版占位）：用于商城定价与出售回收。
// M3 会由数值策划案配置化替换，这里先保证 UI-08/UI-12 可用且一致。
using UnityEngine;

public static class EconomyRules
{
    public static int GetEquipmentBuyPrice(int equipmentId)
    {
        // 数值策划案 / UI策划案 v3.2：商城首版固定售卖“铁剑 500”
        if (equipmentId == 1002) return 500;

        // 简化：按装备加成定价
        if (ConfigManager.Instance != null && ConfigManager.Instance.EquipmentConfigs != null &&
            ConfigManager.Instance.EquipmentConfigs.TryGetValue(equipmentId, out var cfg) && cfg != null)
        {
            return Mathf.Clamp(100 + cfg.attack_bonus * 20 + cfg.defense_bonus * 20, 100, 9999);
        }
        return 200;
    }

    public static int GetEquipmentSellPrice(int equipmentId)
    {
        // 数值策划案 v3.2（装备表）：生锈的剑/铁剑出售价格
        if (equipmentId == 1001) return 20;
        if (equipmentId == 1002) return 50;

        // 简化：回收 50%
        return Mathf.Max(10, Mathf.RoundToInt(GetEquipmentBuyPrice(equipmentId) * 0.5f));
    }

    public static string GetEquipmentName(int equipmentId)
    {
        if (ConfigManager.Instance != null && ConfigManager.Instance.EquipmentConfigs != null &&
            ConfigManager.Instance.EquipmentConfigs.TryGetValue(equipmentId, out var cfg) && cfg != null &&
            !string.IsNullOrEmpty(cfg.name))
            return cfg.name;
        return "装备";
    }
}

