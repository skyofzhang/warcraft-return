// 依据：程序基础知识库 5.2、5.9 第三层；AI程序工作指南 2.5
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MonsterStats))]
public class MonsterController : MonoBehaviour
{
    [Header("AI")]
    public float detectionRange = 5f;
    public float attackRange = 1.5f;
    public float attackCooldown = 2f;
    private float lastAttackTime;

    private MonsterStats stats;
    private Rigidbody rb;
    private Transform player;
    private float hitStunUntil;

    [Header("受击反馈（最低体验：僵直+后退）")]
    [Tooltip("受击僵直时长（秒）。")]
    public float hitStunDuration = 0.10f;
    [Tooltip("受击后退冲量（Impulse）。")]
    public float hitKnockbackImpulse = 2.2f;

    private enum MonsterState { Idle, Chasing, Attacking }
    private MonsterState state = MonsterState.Idle;

    private void Start()
    {
        stats = GetComponent<MonsterStats>();
        rb = GetComponent<Rigidbody>();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    private void Update()
    {
        if (player == null) return;
        if (Time.time < hitStunUntil)
        {
            rb.velocity = Vector3.zero;
            return;
        }
        float dist = Vector3.Distance(transform.position, player.position);

        switch (state)
        {
            case MonsterState.Idle:
                if (dist <= detectionRange) state = MonsterState.Chasing;
                break;
            case MonsterState.Chasing:
                if (dist <= attackRange) state = MonsterState.Attacking;
                else if (dist > detectionRange) state = MonsterState.Idle;
                else ChasePlayer();
                break;
            case MonsterState.Attacking:
                if (dist > attackRange) state = MonsterState.Chasing;
                else AttackPlayer();
                break;
        }
    }

    private void ChasePlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0f;
        float speed = stats != null ? stats.GetStat(StatType.MoveSpeed) : 2.5f;
        rb.velocity = dir * speed;
    }

    private void AttackPlayer()
    {
        rb.velocity = Vector3.zero;
        if (Time.time < lastAttackTime + attackCooldown) return;

        PlayerController pc = player.GetComponent<PlayerController>();
        PlayerStats playerStats = player.GetComponent<PlayerStats>();
        if (pc != null && stats != null && playerStats != null)
        {
            var (finalDamage, isCrit) = CombatSystem.CalculateDamage(stats, playerStats, 1f);
            EventManager.TriggerEvent("MONSTER_ATTACK", new object[] { gameObject.GetInstanceID(), player.GetInstanceID() });
            EventManager.TriggerEvent("DAMAGE_DEALT", new object[] { finalDamage, isCrit, player.GetInstanceID() });
            if (isCrit) EventManager.TriggerEvent("CRITICAL_HIT", new object[] { finalDamage, player.GetInstanceID() });
            pc.TakeDamageFrom(transform.position, finalDamage);
        }
        lastAttackTime = Time.time;
    }

    /// <summary>
    /// 由外部（如 CombatSystem）调用造成伤害；死亡时派发 MONSTER_KILLED。
    /// </summary>
    public void TakeDamage(float damage)
    {
        // 兼容旧调用：默认以玩家位置作为来源（否则方向不明确）
        Vector3 src = player != null ? player.position : (transform.position - transform.forward);
        TakeDamage(damage, src);
    }

    /// <summary>受击（带来源位置）：用于击退方向计算（最低体验：僵直+后退）。</summary>
    public void TakeDamage(float damage, Vector3 sourcePosition)
    {
        if (stats == null) return;
        stats.TakeDamage(damage);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX_Hit();

        if (stats.IsDead)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX_Death_Monster();
            EventManager.TriggerEvent("MONSTER_KILLED", new object[] { stats.monsterId, transform.position, 0 });
            Destroy(gameObject);
            return;
        }

        hitStunUntil = Time.time + Mathf.Max(0f, hitStunDuration);
        Vector3 dir = (transform.position - sourcePosition);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = -transform.forward;
        dir.Normalize();
        rb.velocity = Vector3.zero;
        if (hitKnockbackImpulse > 0.01f)
            rb.AddForce(dir * hitKnockbackImpulse, ForceMode.Impulse);
    }
}
