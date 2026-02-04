// 依据：程序基础知识库 5.2、5.9 第四层；GDD 10.3 装备配置、事件表 EQUIPMENT_CHANGED / INVENTORY_UPDATED
using System.Collections.Generic;
using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }

    /// <summary>背包：item_id -> 数量（装备用 equipment_id）。</summary>
    private Dictionary<int, int> inventory = new Dictionary<int, int>();
    /// <summary>当前穿戴：槽位(type) -> equipment_id。</summary>
    private Dictionary<string, int> equipped = new Dictionary<string, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>导出存档数据（P0）。</summary>
    public EquipmentSaveData ExportSaveData()
    {
        var data = new EquipmentSaveData();
        foreach (var kv in inventory)
            data.inventory.Add(new IntIntPair { key = kv.Key, value = kv.Value });
        foreach (var kv in equipped)
            data.equipped.Add(new StringIntPair { key = kv.Key, value = kv.Value });
        return data;
    }

    /// <summary>导入存档数据（P0）。不会触发逐条 AddItem/RemoveItem 的逻辑，导入后会派发一次刷新事件。</summary>
    public void ImportSaveData(EquipmentSaveData data)
    {
        inventory.Clear();
        equipped.Clear();
        if (data != null)
        {
            if (data.inventory != null)
            {
                for (int i = 0; i < data.inventory.Count; i++)
                {
                    var p = data.inventory[i];
                    if (p == null) continue;
                    if (p.value <= 0) continue;
                    inventory[p.key] = p.value;
                }
            }
            if (data.equipped != null)
            {
                for (int i = 0; i < data.equipped.Count; i++)
                {
                    var p = data.equipped[i];
                    if (p == null || string.IsNullOrEmpty(p.key)) continue;
                    if (p.value <= 0) continue;
                    equipped[p.key] = p.value;
                }
            }
        }
        EventManager.TriggerEvent("INVENTORY_UPDATED", null);
        EventManager.TriggerEvent("EQUIPMENT_CHANGED", null);
    }

    /// <summary>导入或穿戴变化后，重新把装备加成应用到玩家。</summary>
    public void ReapplyEquipmentBonusToPlayer()
    {
        ApplyEquipmentBonusToPlayer();
    }

    /// <summary>背包中某物品数量。</summary>
    public int GetInventoryCount(int itemId)
    {
        return inventory.TryGetValue(itemId, out int count) ? count : 0;
    }

    /// <summary>获取背包快照（只读）。</summary>
    public IReadOnlyDictionary<int, int> GetInventory() => inventory;

    /// <summary>指定槽位当前装备的 equipment_id，0 表示未装备。</summary>
    public int GetEquipped(string slot)
    {
        return equipped.TryGetValue(slot, out int id) ? id : 0;
    }

    /// <summary>获取已穿戴快照（只读）。</summary>
    public IReadOnlyDictionary<string, int> GetEquippedSnapshot() => new Dictionary<string, int>(equipped);

    /// <summary>添加物品到背包并派发 INVENTORY_UPDATED。</summary>
    public void AddItem(int itemId, int count)
    {
        if (count <= 0) return;
        if (!inventory.ContainsKey(itemId)) inventory[itemId] = 0;
        inventory[itemId] += count;
        EventManager.TriggerEvent("INVENTORY_UPDATED", new object[] { itemId, GetInventoryCount(itemId) });
    }

    /// <summary>从背包移除物品（不检查是否穿戴，调用方需先卸下）。</summary>
    public bool RemoveItem(int itemId, int count)
    {
        if (count <= 0) return true;
        int have = GetInventoryCount(itemId);
        if (have < count) return false;
        inventory[itemId] = have - count;
        if (inventory[itemId] <= 0) inventory.Remove(itemId);
        EventManager.TriggerEvent("INVENTORY_UPDATED", new object[] { itemId, GetInventoryCount(itemId) });
        return true;
    }

    /// <summary>穿戴装备。槽位由配置 type 决定；若该槽位已有装备则先卸下。</summary>
    public bool Equip(int equipmentId)
    {
        if (ConfigManager.Instance == null) return false;
        if (!ConfigManager.Instance.EquipmentConfigs.TryGetValue(equipmentId, out EquipmentConfig config))
            return false;
        string slot = config.type;
        if (string.IsNullOrEmpty(slot)) return false;

        int have = GetInventoryCount(equipmentId);
        if (have <= 0) return false;

        if (equipped.TryGetValue(slot, out int oldId) && oldId == equipmentId)
            return true;

        if (equipped.ContainsKey(slot))
            UnequipSlot(slot);

        equipped[slot] = equipmentId;
        RemoveItem(equipmentId, 1);
        ApplyEquipmentBonusToPlayer();
        EventManager.TriggerEvent("EQUIPMENT_CHANGED", new object[] { equipmentId, slot });
        return true;
    }

    /// <summary>卸下指定槽位装备，放回背包。</summary>
    public bool Unequip(string slot)
    {
        if (!equipped.TryGetValue(slot, out int equipmentId))
            return false;
        equipped.Remove(slot);
        AddItem(equipmentId, 1);
        ApplyEquipmentBonusToPlayer();
        EventManager.TriggerEvent("EQUIPMENT_CHANGED", new object[] { 0, slot });
        return true;
    }

    private void UnequipSlot(string slot)
    {
        if (!equipped.TryGetValue(slot, out int equipmentId)) return;
        equipped.Remove(slot);
        AddItem(equipmentId, 1);
    }

    private void ApplyEquipmentBonusToPlayer()
    {
        float totalAttack = 0f;
        float totalDefense = 0f;
        GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
        PlayerStats playerStats = playerGo != null ? playerGo.GetComponent<PlayerStats>() : null;

        if (playerStats != null && ConfigManager.Instance != null)
        {
            foreach (var kv in equipped)
            {
                if (!ConfigManager.Instance.EquipmentConfigs.TryGetValue(kv.Value, out EquipmentConfig c))
                    continue;
                totalAttack += c.attack_bonus;
                totalDefense += c.defense_bonus;
            }
            playerStats.SetEquipmentBonus(totalAttack, totalDefense);
        }
    }
}
