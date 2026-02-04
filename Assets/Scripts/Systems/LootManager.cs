// 依据：程序基础知识库 5.2、5.9 第四层；GDD 9.3 击杀怪物掉落流程
using UnityEngine;

public class LootManager : MonoBehaviour
{
    private void OnEnable()
    {
        EventManager.AddListener("MONSTER_KILLED", OnMonsterKilled);
    }

    private void OnDisable()
    {
        EventManager.RemoveListener("MONSTER_KILLED", OnMonsterKilled);
    }

    private void OnMonsterKilled(object data)
    {
        if (ConfigManager.Instance == null) return;
        if (!(data is object[] arr) || arr.Length < 1) return;

        int monsterId = (int)arr[0];
        Vector3 position = Vector3.zero;
        if (arr.Length > 1 && arr[1] is Vector3) position = (Vector3)arr[1];

        if (!ConfigManager.Instance.MonsterConfigs.TryGetValue(monsterId, out MonsterConfig monsterConfig))
            return;
        int dropTableId = monsterConfig.drop_table_id;
        if (!ConfigManager.Instance.DropTableConfigs.TryGetValue(dropTableId, out DropTableConfig dropTable))
            return;
        if (dropTable.drops == null || dropTable.drops.Count == 0)
            return;

        foreach (DropEntry entry in dropTable.drops)
        {
            if (Random.value > entry.probability) continue;

            int count = Random.Range(entry.count_min, entry.count_max + 1);
            if (count <= 0) continue;

            // 生成可见掉落物，由玩家碰撞触发拾取；拾取时再入账（P1：掉落可见+拾取闭环）
            SpawnDropPickup(entry.item_type, entry.item_id, count, position);
            EventManager.TriggerEvent("ITEM_DROPPED", new object[] { entry.item_type, entry.item_id, count, position });
        }
    }

    private void SpawnDropPickup(string itemType, int itemId, int count, Vector3 position)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = $"Drop_{itemType}_{itemId}_{count}";
        go.transform.position = position + Vector3.up * 0.5f + Random.insideUnitSphere * 0.5f;
        go.transform.localScale = Vector3.one * 0.5f;

        var col = go.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        var r = go.GetComponent<Renderer>();
        if (r != null)
        {
            r.material.color = itemType == "gold" ? new Color(1f, 0.82f, 0.1f) : new Color(0.2f, 0.8f, 1f);
        }

        var pickup = go.AddComponent<DropPickup>();
        pickup.itemType = itemType;
        pickup.itemId = itemId;
        pickup.count = count;
    }
}
