// 依据：程序基础知识库 5.8；GDD 3.5 数值护栏
using UnityEngine;

/// <summary>
/// Gameplay 场景启动时自检：核心对象存在性、数值护栏。可自动修复项执行修复。
/// </summary>
public class SanityCheck : MonoBehaviour
{
    [Header("数值护栏（GDD 3.5）")]
    [SerializeField] private float playerHpMin = 100f;
    [SerializeField] private float playerHpMax = 10000f;
    [SerializeField] private float playerAttackMin = 10f;
    [SerializeField] private float playerAttackMax = 1000f;
    [SerializeField] private float playerDefenseMin = 5f;
    [SerializeField] private float playerDefenseMax = 500f;
    [SerializeField] private float playerMoveSpeedMin = 3f;
    [SerializeField] private float playerMoveSpeedMax = 10f;

    private void Start()
    {
        bool ok = true;
        ok &= CheckPlayer();
        ok &= CheckSpawner();
        ok &= CheckUICanvas();
        ok &= CheckPlayerGuardrails();
        if (ok)
            Debug.Log("[SanityCheck] 全部通过");
        else
            Debug.LogWarning("[SanityCheck] 存在项未通过，请检查上述日志");
    }

    private bool CheckPlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("[SanityCheck] 未找到 Tag=Player 的对象");
            return false;
        }
        if (player.GetComponent<PlayerStats>() == null)
            Debug.LogWarning("[SanityCheck] Player 缺少 PlayerStats 组件");
        if (player.GetComponent<PlayerController>() == null)
            Debug.LogWarning("[SanityCheck] Player 缺少 PlayerController 组件");
        return true;
    }

    private bool CheckSpawner()
    {
        var spawner = FindObjectOfType<MonsterSpawner>();
        if (spawner == null)
        {
            Debug.LogWarning("[SanityCheck] 场景中未找到 MonsterSpawner");
            return false;
        }
        if (spawner.spawnPoints == null || spawner.spawnPoints.Length == 0)
            Debug.LogWarning("[SanityCheck] MonsterSpawner 未配置刷怪点");
        return true;
    }

    private bool CheckUICanvas()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null && UIManager.Instance == null)
        {
            Debug.LogWarning("[SanityCheck] 未找到 Canvas 或 UIManager（UIManager 可能在其他场景）");
            return false;
        }
        return true;
    }

    private bool CheckPlayerGuardrails()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return false;
        var ps = player.GetComponent<PlayerStats>();
        if (ps == null) return false;

        float maxHp = ps.MaxHp;
        float attack = ps.GetStat(StatType.Attack);
        float defense = ps.GetStat(StatType.Defense);
        float moveSpeed = ps.GetStat(StatType.MoveSpeed);

        // 装备加成会推高攻击/防御：护栏应对“基础数值”（不含装备）做约束
        GetEquippedBonus(out float eqAtk, out float eqDef);
        float baseAttack = attack - eqAtk;
        float baseDefense = defense - eqDef;

        bool ok = true;
        if (maxHp < playerHpMin || maxHp > playerHpMax)
        {
            Debug.LogWarning($"[SanityCheck] 玩家 MaxHp={maxHp} 超出护栏 [{playerHpMin},{playerHpMax}]");
            ok = false;
        }
        if (baseAttack < playerAttackMin || baseAttack > playerAttackMax)
        {
            Debug.LogWarning($"[SanityCheck] 玩家 Attack(Base)={baseAttack} 超出护栏 [{playerAttackMin},{playerAttackMax}]（装备加成 atk={eqAtk}）");
            ok = false;
        }
        if (baseDefense < playerDefenseMin || baseDefense > playerDefenseMax)
        {
            Debug.LogWarning($"[SanityCheck] 玩家 Defense(Base)={baseDefense} 超出护栏 [{playerDefenseMin},{playerDefenseMax}]（装备加成 def={eqDef}）");
            ok = false;
        }
        if (moveSpeed < playerMoveSpeedMin || moveSpeed > playerMoveSpeedMax)
        {
            Debug.LogWarning($"[SanityCheck] 玩家 MoveSpeed={moveSpeed} 超出护栏 [{playerMoveSpeedMin},{playerMoveSpeedMax}]");
            ok = false;
        }
        return ok;
    }

    private static void GetEquippedBonus(out float atk, out float def)
    {
        atk = 0f;
        def = 0f;
        if (EquipmentManager.Instance == null) return;
        if (ConfigManager.Instance == null || ConfigManager.Instance.EquipmentConfigs == null) return;

        var equipped = EquipmentManager.Instance.GetEquippedSnapshot();
        if (equipped == null) return;

        foreach (var kv in equipped)
        {
            int equipmentId = kv.Value;
            if (equipmentId <= 0) continue;
            if (!ConfigManager.Instance.EquipmentConfigs.TryGetValue(equipmentId, out var cfg) || cfg == null) continue;
            atk += cfg.attack_bonus;
            def += cfg.defense_bonus;
        }
    }
}
