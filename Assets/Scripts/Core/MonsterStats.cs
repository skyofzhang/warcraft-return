// 依据：程序基础知识库 5.2、5.3、5.9 第二层
using UnityEngine;

public class MonsterStats : MonoBehaviour, IStatsProvider
{
    [Header("怪物配置ID")]
    public int monsterId;

    [Header("基础属性")]
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private float attack = 10f;
    [SerializeField] private float defense = 5f;
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float attackSpeed = 1f;
    [SerializeField] private float critChance = 0.05f;
    // 程序知识库 v2.1.1：CritDamage 为系数（如 1.3 表示 130% 伤害）
    [SerializeField] private float critDamage = 1.3f;

    private float currentHp;

    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;

    private void Awake()
    {
        currentHp = maxHp;
    }

    public void InitFromConfig(MonsterConfig config)
    {
        if (config == null) return;
        monsterId = config.monster_id;
        maxHp = config.hp;
        attack = config.attack;
        defense = config.defense;
        moveSpeed = config.move_speed;
        currentHp = maxHp;
    }

    public float GetStat(StatType type)
    {
        switch (type)
        {
            case StatType.HP: return currentHp;
            case StatType.MaxHP: return maxHp;
            case StatType.Attack: return attack;
            case StatType.Defense: return defense;
            case StatType.MoveSpeed: return moveSpeed;
            case StatType.AttackSpeed: return attackSpeed;
            case StatType.CritChance: return critChance;
            case StatType.CritDamage: return critDamage;
            case StatType.Level: return 0f;
            case StatType.CurrentExp: return 0f;
            case StatType.NextLevelExp: return 0f;
            case StatType.Gold: return 0f;
            case StatType.LifeSteal: return 0f;
            case StatType.DamageReduction: return 0f;
            default: return 0f;
        }
    }

    public void ModifyStat(StatType type, float delta)
    {
        switch (type)
        {
            case StatType.HP:
                currentHp = Mathf.Clamp(currentHp + delta, 0f, maxHp);
                break;
            default:
                break;
        }
    }

    public void TakeDamage(float damage)
    {
        ModifyStat(StatType.HP, -damage);
    }

    public bool IsDead => currentHp <= 0f;
}
