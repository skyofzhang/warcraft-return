// 统一背包/装备/金币的操作入口：
// - 运行态优先走 EquipmentManager / PlayerStats
// - 主菜单无运行态对象时，直接操作 SaveSystem 缓存并落盘
using System.Collections.Generic;
using UnityEngine;

public static class InventoryService
{
    public static IReadOnlyDictionary<int, int> GetInventorySnapshot()
    {
        if (EquipmentManager.Instance != null) return EquipmentManager.Instance.GetInventory();
        SaveSystem.EnsureLoaded();
        var save = SaveSystem.GetCached();
        var dict = new Dictionary<int, int>();
        if (save?.equipment?.inventory != null)
        {
            for (int i = 0; i < save.equipment.inventory.Count; i++)
            {
                var p = save.equipment.inventory[i];
                if (p == null) continue;
                if (p.value <= 0) continue;
                dict[p.key] = p.value;
            }
        }
        return dict;
    }

    public static IReadOnlyDictionary<string, int> GetEquippedSnapshot()
    {
        if (EquipmentManager.Instance != null) return EquipmentManager.Instance.GetEquippedSnapshot();
        SaveSystem.EnsureLoaded();
        var save = SaveSystem.GetCached();
        var dict = new Dictionary<string, int>();
        if (save?.equipment?.equipped != null)
        {
            for (int i = 0; i < save.equipment.equipped.Count; i++)
            {
                var p = save.equipment.equipped[i];
                if (p == null || string.IsNullOrEmpty(p.key)) continue;
                if (p.value <= 0) continue;
                dict[p.key] = p.value;
            }
        }
        return dict;
    }

    public static int GetGold()
    {
        // 运行态
        var player = GameObject.FindGameObjectWithTag("Player");
        var ps = player != null ? player.GetComponent<PlayerStats>() : null;
        if (ps != null) return ps.Gold;

        // 存档
        SaveSystem.EnsureLoaded();
        var save = SaveSystem.GetCached();
        return save?.player != null ? Mathf.Max(0, save.player.gold) : 0;
    }

    public static void AddGold(int delta)
    {
        if (delta == 0) return;

        var player = GameObject.FindGameObjectWithTag("Player");
        var ps = player != null ? player.GetComponent<PlayerStats>() : null;
        if (ps != null)
        {
            ps.AddGold(delta);
            SaveSystem.CaptureFromRuntime();
            SaveSystem.SaveNow();
            return;
        }

        SaveSystem.EnsureLoaded();
        var save = SaveSystem.GetCached();
        if (save?.player == null) return;
        save.player.gold = Mathf.Max(0, save.player.gold + delta);
        SaveSystem.SaveNow();
        EventManager.TriggerEvent("GOLD_CHANGED", new object[] { save.player.gold, delta });
    }

    public static int GetPotionCount()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        var ps = player != null ? player.GetComponent<PlayerStats>() : null;
        if (ps != null) return ps.PotionCount;

        SaveSystem.EnsureLoaded();
        var save = SaveSystem.GetCached();
        return save?.player != null ? Mathf.Max(0, save.player.potion_count) : 0;
    }

    public static void AddPotion(int delta)
    {
        if (delta == 0) return;

        var player = GameObject.FindGameObjectWithTag("Player");
        var ps = player != null ? player.GetComponent<PlayerStats>() : null;
        if (ps != null)
        {
            ps.AddPotion(delta);
            SaveSystem.CaptureFromRuntime();
            SaveSystem.SaveNow();
            return;
        }

        SaveSystem.EnsureLoaded();
        var save = SaveSystem.GetCached();
        if (save?.player == null) return;
        int old = save.player.potion_count;
        save.player.potion_count = Mathf.Clamp(old + delta, 0, 99);
        SaveSystem.SaveNow();
        EventManager.TriggerEvent("POTION_CHANGED", new object[] { save.player.potion_count, save.player.potion_count - old });
    }

    public static bool TryEquip(int equipmentId)
    {
        if (EquipmentManager.Instance != null) return EquipmentManager.Instance.Equip(equipmentId);

        // 存档模式 equip
        if (ConfigManager.Instance == null || ConfigManager.Instance.EquipmentConfigs == null) return false;
        if (!ConfigManager.Instance.EquipmentConfigs.TryGetValue(equipmentId, out var cfg) || cfg == null) return false;
        if (string.IsNullOrEmpty(cfg.type)) return false;

        SaveSystem.EnsureLoaded();
        var save = SaveSystem.GetCached();
        if (save == null) return false;
        if (save.equipment == null) save.equipment = new EquipmentSaveData();
        if (save.equipment.inventory == null) save.equipment.inventory = new List<IntIntPair>();
        if (save.equipment.equipped == null) save.equipment.equipped = new List<StringIntPair>();

        if (!TryRemoveFromSaveInventory(save, equipmentId, 1)) return false;

        // 若槽位已有装备，先卸下回背包
        int oldId = GetEquippedFromSave(save, cfg.type);
        if (oldId > 0) TryAddToSaveInventory(save, oldId, 1);

        SetEquippedToSave(save, cfg.type, equipmentId);
        SaveSystem.SaveNow();
        EventManager.TriggerEvent("INVENTORY_UPDATED", new object[] { equipmentId, 0 });
        EventManager.TriggerEvent("EQUIPMENT_CHANGED", new object[] { equipmentId, cfg.type });
        return true;
    }

    public static bool TryUnequip(string slot)
    {
        if (EquipmentManager.Instance != null) return EquipmentManager.Instance.Unequip(slot);

        SaveSystem.EnsureLoaded();
        var save = SaveSystem.GetCached();
        if (save?.equipment == null) return false;

        int eqId = GetEquippedFromSave(save, slot);
        if (eqId <= 0) return false;

        SetEquippedToSave(save, slot, 0);
        TryAddToSaveInventory(save, eqId, 1);
        SaveSystem.SaveNow();
        EventManager.TriggerEvent("INVENTORY_UPDATED", new object[] { eqId, 0 });
        EventManager.TriggerEvent("EQUIPMENT_CHANGED", new object[] { 0, slot });
        return true;
    }

    public static bool TrySell(int equipmentId, int count, int unitSellPrice)
    {
        if (count <= 0) return true;
        SaveSystem.EnsureLoaded();
        var save = SaveSystem.GetCached();
        if (save == null) return false;

        if (EquipmentManager.Instance != null)
        {
            // 运行态卖：先移除物品，再加钱，再落盘
            bool ok = EquipmentManager.Instance.RemoveItem(equipmentId, count);
            if (!ok) return false;
            AddGold(unitSellPrice * count);
            SaveSystem.CaptureFromRuntime();
            SaveSystem.SaveNow();
            return true;
        }

        if (!TryRemoveFromSaveInventory(save, equipmentId, count)) return false;
        AddGold(unitSellPrice * count);
        SaveSystem.SaveNow();
        EventManager.TriggerEvent("INVENTORY_UPDATED", new object[] { equipmentId, 0 });
        return true;
    }

    /// <summary>丢弃（不获得金币）。UI策划案 v3.2：背包丢弃需要确认。</summary>
    public static bool TryDrop(int equipmentId, int count)
    {
        if (count <= 0) return true;
        SaveSystem.EnsureLoaded();
        var save = SaveSystem.GetCached();
        if (save == null) return false;

        if (EquipmentManager.Instance != null)
        {
            bool ok = EquipmentManager.Instance.RemoveItem(equipmentId, count);
            if (!ok) return false;
            SaveSystem.CaptureFromRuntime();
            SaveSystem.SaveNow();
            return true;
        }

        if (!TryRemoveFromSaveInventory(save, equipmentId, count)) return false;
        SaveSystem.SaveNow();
        EventManager.TriggerEvent("INVENTORY_UPDATED", new object[] { equipmentId, 0 });
        return true;
    }

    private static int GetEquippedFromSave(GameSaveData save, string slot)
    {
        if (save?.equipment?.equipped == null) return 0;
        for (int i = 0; i < save.equipment.equipped.Count; i++)
        {
            var p = save.equipment.equipped[i];
            if (p == null) continue;
            if (p.key == slot) return p.value;
        }
        return 0;
    }

    private static void SetEquippedToSave(GameSaveData save, string slot, int equipmentId)
    {
        if (save.equipment.equipped == null) save.equipment.equipped = new List<StringIntPair>();
        for (int i = 0; i < save.equipment.equipped.Count; i++)
        {
            var p = save.equipment.equipped[i];
            if (p == null) continue;
            if (p.key == slot)
            {
                p.value = Mathf.Max(0, equipmentId);
                return;
            }
        }
        save.equipment.equipped.Add(new StringIntPair { key = slot, value = Mathf.Max(0, equipmentId) });
    }

    private static void TryAddToSaveInventory(GameSaveData save, int equipmentId, int count)
    {
        if (count <= 0) return;
        if (save.equipment.inventory == null) save.equipment.inventory = new List<IntIntPair>();
        for (int i = 0; i < save.equipment.inventory.Count; i++)
        {
            var p = save.equipment.inventory[i];
            if (p == null) continue;
            if (p.key == equipmentId)
            {
                p.value += count;
                return;
            }
        }
        save.equipment.inventory.Add(new IntIntPair { key = equipmentId, value = count });
    }

    private static bool TryRemoveFromSaveInventory(GameSaveData save, int equipmentId, int count)
    {
        if (count <= 0) return true;
        if (save?.equipment?.inventory == null) return false;

        for (int i = 0; i < save.equipment.inventory.Count; i++)
        {
            var p = save.equipment.inventory[i];
            if (p == null) continue;
            if (p.key != equipmentId) continue;
            if (p.value < count) return false;
            p.value -= count;
            if (p.value <= 0) save.equipment.inventory.RemoveAt(i);
            return true;
        }
        return false;
    }
}

