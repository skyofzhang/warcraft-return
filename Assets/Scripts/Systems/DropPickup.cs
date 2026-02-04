// 依据：GDD 9.3 掉落数据流、P1 掉落可见+拾取闭环
using UnityEngine;

/// <summary>
/// 场景掉落物：玩家碰到后自动拾取。
/// - itemType: "gold" / "equipment"（可扩展）
/// - 拾取后派发 ITEM_PICKED_UP，并把奖励结算到 PlayerStats/EquipmentManager。
/// </summary>
[RequireComponent(typeof(Collider))]
public class DropPickup : MonoBehaviour
{
    [Header("掉落信息")]
    public string itemType;
    public int itemId;
    public int count;

    [Header("拾取设置")]
    public float pickupDelay = 0.15f;

    private float spawnTime;
    private bool picked;

    private void Awake()
    {
        spawnTime = Time.time;
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (picked) return;
        if (Time.time - spawnTime < pickupDelay) return;
        if (!other.CompareTag("Player")) return;
        PickUp(other.gameObject);
    }

    private void PickUp(GameObject player)
    {
        if (picked) return;
        picked = true;

        if (itemType == "gold")
        {
            var ps = player != null ? player.GetComponent<PlayerStats>() : null;
            if (ps != null) ps.AddGold(count);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX_GoldPickup();
        }
        else if (itemType == "equipment")
        {
            if (EquipmentManager.Instance != null) EquipmentManager.Instance.AddItem(itemId, count);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX_Pickup();
        }
        else if (itemType == "potion")
        {
            var ps = player != null ? player.GetComponent<PlayerStats>() : null;
            if (ps != null) ps.AddPotion(count);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX_Pickup();
        }

        EventManager.TriggerEvent("ITEM_PICKED_UP", new object[] { itemType, itemId, count, transform.position });
        Destroy(gameObject);
    }
}

