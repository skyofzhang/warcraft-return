// 依据：程序基础知识库 5.2、5.9 第三层；AI程序工作指南 2.4、1.7
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerController : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f;

    [Header("战斗设置")]
    public float attackRange = 2f;
    public float attackCooldown = 1f;
    private float lastAttackTime;
    [Header("技能（首版：2个技能）")]
    [Tooltip("技能0（SK001 多重箭）冷却（秒）。会被 SkillConfigs.json 覆盖。")]
    public float skill0Cooldown = 8f;
    [Tooltip("技能0（SK001 多重箭）伤害倍率。会被 SkillConfigs.json 覆盖。")]
    public float skill0DamageMultiplier = 0.8f;
    [Tooltip("技能1（SK002 穿透箭）冷却（秒）。会被 SkillConfigs.json 覆盖。")]
    public float skill1Cooldown = 5f;
    [Tooltip("技能1（SK002 穿透箭）伤害倍率。会被 SkillConfigs.json 覆盖。")]
    public float skill1DamageMultiplier = 1.2f;

    private readonly float[] lastSkillTimes = new float[2];

    [Header("翻滚/闪避（技能2：表现&位移）")]
    [Tooltip("闪避冷却（秒）。")]
    public float dodgeCooldown = 6f;
    [Tooltip("闪避水平速度（VelocityChange 近似）。")]
    public float dodgeSpeed = 12f;
    [Tooltip("闪避位移持续时间（秒）。")]
    public float dodgeDuration = 0.16f;
    [Tooltip("闪避无敌时间（秒）。")]
    public float dodgeInvuln = 0.22f;
    private float lastDodgeTime = -999f;
    private float dodgeUntil;
    private float dodgeInvulnUntil;
    private Vector3 dodgeVel;

    private Rigidbody rb;
    private PlayerStats stats;
    private Transform nearestEnemy;
    private Transform visualRoot;
    private Animation visualAnim;
    private float moveInputSqr;
    private float animLockUntil;
    private bool deathLocked;
    private float lastPotionTime;
    private float hitStunUntil;

    [Header("角色外观（从 Resources 加载 FBX/Prefab）")]
    [Tooltip("Resources 路径（不含扩展名）。例如：Models/Player/Player_Elf")]
    public string playerModelPath = "Models/Player/Player_Elf";

    [Header("治疗瓶（首版：初始3，恢复30%HP）")]
    public float potionHealRatio = 0.3f;
    public float potionCooldown = 0.6f;

    [Header("受击反馈（最低体验：僵直+后退）")]
    [Tooltip("受击僵直时长（秒）。")]
    public float hitStunDuration = 0.12f;
    [Tooltip("受击后退冲量（Impulse）。")]
    public float hitKnockbackImpulse = 2.8f;

    private AnimationClip clipIdle;
    private AnimationClip clipMove;
    private AnimationClip clipAttack;
    private AnimationClip clipSkill;
    private AnimationClip clipHit;
    private AnimationClip clipDeath;

    private static bool s_loggedMissingVisualPrefab;
    private static bool s_loggedNoVisualRenderers;
    private static Material s_fallbackMaterial;

    [Header("移动边界（占位关卡防止走出区域）")]
    // 当场景已用墙体/碰撞限制边界时应关闭，避免与物理碰撞“打架”
    public bool clampToBounds = false;
    public float minX = -25f;
    public float maxX = 25f;
    public float minZ = -25f;
    public float maxZ = 25f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        stats = GetComponent<PlayerStats>();
        EnsureVisual();
    }

    /// <summary>
    /// 从 Resources 加载角色模型并挂到 Player 下（不影响物理与脚本组件）。
    /// </summary>
    private void EnsureVisual()
    {
        // 已挂载则不重复
        visualRoot = transform.Find("_Visual");
        if (visualRoot != null) return;

        var visualPrefab = Resources.Load<GameObject>(playerModelPath);
        if (visualPrefab == null)
        {
            if (!s_loggedMissingVisualPrefab)
            {
                s_loggedMissingVisualPrefab = true;
                Debug.LogWarning($"[PlayerController] 未能加载角色视觉资源：Resources.Load(\"{playerModelPath}\") 返回 null。将继续使用占位外观。");
            }
            return; // 允许没有美术资源（占位模式）
        }

        var go = Instantiate(visualPrefab, transform);
        go.name = "_Visual";
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        // 1) 强制 Layer 与玩家一致（避免相机裁剪层导致不可见）
        ApplyLayerRecursively(go, gameObject.layer);

        // 2) 若 Renderer 没有材质，补一个默认材质（否则可能完全不渲染）
        EnsureDefaultMaterials(go);

        // 2.5) 命中闪白（战斗表现）
        if (go.GetComponent<HitFlash>() == null) go.AddComponent<HitFlash>();

        // 3) 尺寸/落地对齐：按 CapsuleCollider 高度自动缩放，并把模型底部贴到碰撞体底部
        FitVisualToCapsule(go);

        // 4) 绑定 Legacy Animation：从模型 sub-assets 中加载动画，并做一次“模糊匹配”
        visualAnim = go.GetComponent<Animation>() ?? go.GetComponentInChildren<Animation>(true);
        if (visualAnim == null) visualAnim = go.AddComponent<Animation>();
        BindClipsFromResources(playerModelPath);
        if (clipIdle != null) visualAnim.Play(clipIdle.name);
        else if (visualAnim.clip != null) visualAnim.Play();

        // 关闭占位 Capsule 的渲染（保留 Collider/Rigidbody）
        var mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;
        var mf = GetComponent<MeshFilter>();
        if (mf != null) mf.sharedMesh = null;
    }

    private void BindClipsFromResources(string resPath)
    {
        if (visualAnim == null) return;
        if (string.IsNullOrEmpty(resPath)) return;

        var clips = Resources.LoadAll<AnimationClip>(resPath);
        if (clips == null || clips.Length == 0) return;

        // 将所有 clip 塞进 Animation，方便 CrossFade/Play
        // 注意：Animation 组件只能播放 Legacy clip；很多 FBX 默认导入的是 Mecanim。
        // 这里运行时复制一份 clip 并强制 legacy=true，避免播放报错导致测试失败。
        for (int i = 0; i < clips.Length; i++)
        {
            var c = clips[i];
            if (c == null) continue;
            if (visualAnim.GetClip(c.name) != null) continue;

            var legacy = Instantiate(c);
            legacy.name = c.name;
            legacy.legacy = true;
            visualAnim.AddClip(legacy, legacy.name);
        }

        // 模糊匹配：尽量适配不同资源包的命名
        clipIdle = PickClip(clips, "idle", "stand", "ready");
        clipMove = PickClip(clips, "walk", "run", "move");
        clipAttack = PickClip(clips, "attack", "shoot", "punch", "hit");
        clipSkill = PickClip(clips, "skill", "cast", "shoot", "attack");
        clipHit = PickClip(clips, "hurt", "hit", "damage");
        clipDeath = PickClip(clips, "death", "die");
    }

    private static AnimationClip PickClip(AnimationClip[] clips, params string[] keywords)
    {
        if (clips == null || clips.Length == 0) return null;
        for (int k = 0; k < keywords.Length; k++)
        {
            string key = keywords[k];
            if (string.IsNullOrEmpty(key)) continue;
            for (int i = 0; i < clips.Length; i++)
            {
                var c = clips[i];
                if (c == null) continue;
                if (c.name.IndexOf(key, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return c;
            }
        }
        // fallback：取第一个非空
        for (int i = 0; i < clips.Length; i++)
            if (clips[i] != null) return clips[i];
        return null;
    }

    private static void ApplyLayerRecursively(GameObject root, int layer)
    {
        if (root == null) return;
        var stack = new System.Collections.Generic.Stack<Transform>();
        stack.Push(root.transform);
        while (stack.Count > 0)
        {
            var t = stack.Pop();
            if (t == null) continue;
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++)
                stack.Push(t.GetChild(i));
        }
    }

    private static void EnsureDefaultMaterials(GameObject root)
    {
        if (root == null) return;
        var defaultMat = GetFallbackMaterial();
        if (defaultMat == null) return;

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;
            var mats = r.sharedMaterials;
            if (mats == null || mats.Length == 0)
            {
                r.sharedMaterial = defaultMat;
                continue;
            }
            bool anyValid = false;
            for (int m = 0; m < mats.Length; m++)
            {
                if (mats[m] != null) { anyValid = true; break; }
            }
            if (!anyValid) r.sharedMaterial = defaultMat;
        }
    }

    private static Material GetFallbackMaterial()
    {
        if (s_fallbackMaterial != null) return s_fallbackMaterial;

        // 不同 Unity/渲染管线下内置材质名称并不稳定；这里直接创建一个兜底材质更可靠。
        // URP 项目下 Standard 仍然“存在”，但会在渲染时变粉（不兼容）；所以这里优先用 URP/Lit。
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) return null;

        s_fallbackMaterial = new Material(shader);
        // 尽量显眼，便于发现“材质未配置”的问题
        if (s_fallbackMaterial.HasProperty("_Color"))
            s_fallbackMaterial.color = new Color(0.85f, 0.85f, 0.85f, 1f);

        s_fallbackMaterial.name = "_RuntimeFallbackMat_Player";
        return s_fallbackMaterial;
    }

    private void FitVisualToCapsule(GameObject visual)
    {
        if (visual == null) return;

        var renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            if (!s_loggedNoVisualRenderers)
            {
                s_loggedNoVisualRenderers = true;
                Debug.LogWarning("[PlayerController] 视觉 Prefab 下未找到任何 Renderer，模型可能未正确导入/导出。");
            }
            return;
        }

        // 合并 bounds（世界空间）
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            b.Encapsulate(renderers[i].bounds);
        }

        float currentHeight = b.size.y;
        if (currentHeight < 0.001f) return;

        var capsule = GetComponent<CapsuleCollider>();
        float targetHeight = capsule != null ? capsule.height : 2f;

        // 先缩放：让模型高度接近碰撞体高度
        float scale = targetHeight / currentHeight;
        scale = Mathf.Clamp(scale, 0.05f, 20f);
        visual.transform.localScale = Vector3.one * scale;

        // 再落地：把模型底部对齐到碰撞体底部（世界 y）
        // 重新计算 bounds
        b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            b.Encapsulate(renderers[i].bounds);
        }

        float desiredBottomY = transform.position.y;
        if (capsule != null)
        {
            // collider bottom = center.y - height/2（中心为本地坐标）
            desiredBottomY = transform.TransformPoint(capsule.center).y - capsule.height * 0.5f;
        }

        float deltaY = desiredBottomY - b.min.y;
        var lp = visual.transform.localPosition;
        lp.y += deltaY; // 父物体无旋转的情况下足够；当前玩家也无旋转
        visual.transform.localPosition = lp;
    }

    private void Update()
    {
        HandleMovement();
        UpdateVisualAnimation();
        HandleAutoAttack();
    }

    private void LateUpdate()
    {
        if (!clampToBounds) return;
        var pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
        transform.position = pos;
    }

    private void HandleMovement()
    {
        if (deathLocked)
        {
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
            return;
        }
        // 闪避中：锁定水平速度（仍允许 y 方向物理）
        if (Time.time < dodgeUntil)
        {
            rb.velocity = new Vector3(dodgeVel.x, rb.velocity.y, dodgeVel.z);
            return;
        }
        // 受击僵直：冻结水平移动（仍允许 y 方向物理）
        if (Time.time < hitStunUntil)
        {
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
            return;
        }

        UnityEngine.Vector2 input = VirtualJoystick.GetInput();
        moveInputSqr = input.sqrMagnitude;
        Vector3 moveInput = new Vector3(input.x, 0f, input.y);
        Vector3 movement = moveInput.normalized * (stats != null ? stats.GetStat(StatType.MoveSpeed) : moveSpeed);
        rb.velocity = new Vector3(movement.x, rb.velocity.y, movement.z);
    }

    private void UpdateVisualAnimation()
    {
        if (visualAnim == null) return;
        if (deathLocked) return;
        if (Time.time < animLockUntil) return;

        var target = moveInputSqr > 0.01f ? clipMove : clipIdle;
        if (target == null) return;
        if (visualAnim.GetClip(target.name) == null) return;
        if (!visualAnim.IsPlaying(target.name))
            visualAnim.CrossFade(target.name, 0.12f);
    }

    private void PlayAction(AnimationClip clip, bool lockByLength = true)
    {
        if (visualAnim == null) return;
        if (clip == null) return;
        var c = visualAnim.GetClip(clip.name);
        if (c == null) return;

        // 死亡播放一次后锁住
        if (clip == clipDeath) deathLocked = true;

        visualAnim.CrossFade(clip.name, 0.05f);

        if (lockByLength)
        {
            float len = c.length;
            if (len < 0.05f) len = 0.25f;
            animLockUntil = Time.time + len;
        }
    }

    private void HandleAutoAttack()
    {
        if (deathLocked) return;
        if (Time.time < hitStunUntil) return;
        Transform target = FindNearestEnemy();
        if (target == null) return;

        float distance = Vector3.Distance(transform.position, target.position);
        if (distance <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            Attack(target.gameObject);
            lastAttackTime = Time.time;
        }
    }

    private Transform FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Monster");
        Transform nearest = null;
        float minDist = float.MaxValue;
        foreach (GameObject go in enemies)
        {
            float d = Vector3.Distance(transform.position, go.transform.position);
            if (d < minDist) { minDist = d; nearest = go.transform; }
        }
        return nearest;
    }

    private void Attack(GameObject target)
    {
        MonsterController mc = target.GetComponent<MonsterController>();
        MonsterStats defenderStats = target.GetComponent<MonsterStats>();
        if (mc != null && defenderStats != null && stats != null)
        {
            PlayAction(clipAttack);
            // Attack VFX hook
            EventManager.TriggerEvent("BASIC_ATTACK", new object[] { gameObject.GetInstanceID(), target.GetInstanceID(), target.transform.position });
            var (finalDamage, isCrit) = CombatSystem.CalculateDamage(stats, defenderStats, 1f);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX_Attack();
            EventManager.TriggerEvent("DAMAGE_DEALT", new object[] { finalDamage, isCrit, target.GetInstanceID() });
            if (isCrit) EventManager.TriggerEvent("CRITICAL_HIT", new object[] { finalDamage, target.GetInstanceID() });
            mc.TakeDamage(finalDamage, transform.position);
        }
    }

    public void TakeDamage(float damage)
    {
        ApplyDamageInternal(damage, hasSource: false, sourcePosition: Vector3.zero);
    }

    /// <summary>
    /// 受击（带来源位置）：用于击退方向计算（最低体验：僵直+后退）。
    /// </summary>
    public void TakeDamageFrom(Vector3 sourcePosition, float damage)
    {
        ApplyDamageInternal(damage, hasSource: true, sourcePosition: sourcePosition);
    }

    private void ApplyDamageInternal(float damage, bool hasSource, Vector3 sourcePosition)
    {
        if (deathLocked) return;
        if (Time.time < dodgeInvulnUntil) return; // 闪避无敌
        if (damage <= 0f) return;
        if (stats == null) return;

        if (stats != null) stats.TakeDamage(damage);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX_Hit();
        EventManager.TriggerEvent("DAMAGE_TAKEN", new object[] { damage, false, gameObject.GetInstanceID() });

        if (stats != null && stats.CurrentHp <= 0f)
        {
            PlayAction(clipDeath, lockByLength: false);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX_Death_Player();
            EventManager.TriggerEvent("PLAYER_KILLED", gameObject.GetInstanceID());
            return;
        }

        // 僵直 + 后退
        hitStunUntil = Time.time + Mathf.Max(0f, hitStunDuration);
        Vector3 dir = hasSource ? (transform.position - sourcePosition) : -transform.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = -transform.forward;
        dir.Normalize();
        rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
        if (hitKnockbackImpulse > 0.01f)
            rb.AddForce(dir * hitKnockbackImpulse, ForceMode.Impulse);

        PlayAction(clipHit);
    }

    public bool TryUsePotion()
    {
        if (deathLocked) return false;
        if (stats == null) return false;
        if (Time.time < hitStunUntil) return false;
        if (Time.time < lastPotionTime + potionCooldown) return false;
        bool ok = stats.TryUsePotion(potionHealRatio);
        if (ok)
        {
            lastPotionTime = Time.time;
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX_Skill();
        }
        return ok;
    }

    /// <summary>技能释放，由 UI 或输入调用。skillId：0=SK001（多重箭），1=SK002（穿透箭）。无目标时也进入冷却，便于测试与 UI 一致。</summary>
    public void UseSkill(int skillId)
    {
        if (skillId < 0 || skillId > 2) return;
        if (deathLocked) return;
        // 允许“受击僵直中”释放技能（更跟手；并避免测试偶发受击导致无法进入冷却）

        // 技能2：闪避（无需目标）
        if (skillId == 2)
        {
            TryDodge();
            return;
        }

        ApplySkillConfigIfPresent(skillId);
        float cd = GetSkillCooldownSeconds(skillId);
        if (Time.time < lastSkillTimes[skillId] + cd) return;
        lastSkillTimes[skillId] = Time.time;

        PlayAction(clipSkill);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX_Skill();

        // 目标选择：cone（扇形）/line（直线穿透）/single（单体）
        var targets = FindSkillTargets(skillId);
        if (targets == null || targets.Count == 0) return;

        // 技能事件：位置取第一个目标位置（用于占位特效/测试）
        EventManager.TriggerEvent("SKILL_USED", new object[] { skillId, gameObject.GetInstanceID(), targets[0].position });

        float mult = GetSkillDamageMultiplier(skillId);
        for (int i = 0; i < targets.Count; i++)
        {
            var t = targets[i];
            if (t == null) continue;
            var defenderStats = t.GetComponent<MonsterStats>();
            if (defenderStats == null || stats == null) continue;
            var (finalDamage, isCrit) = CombatSystem.CalculateDamage(stats, defenderStats, mult);
            EventManager.TriggerEvent("DAMAGE_DEALT", new object[] { finalDamage, isCrit, t.gameObject.GetInstanceID() });
            if (isCrit) EventManager.TriggerEvent("CRITICAL_HIT", new object[] { finalDamage, t.gameObject.GetInstanceID() });
            var mc = t.GetComponent<MonsterController>();
            if (mc != null) mc.TakeDamage(finalDamage, transform.position);
        }
    }

    /// <summary>兼容旧测试：技能0是否就绪。</summary>
    public bool IsSkillReady => IsSkillReadyFor(0);

    public bool IsSkillReadyFor(int skillId)
    {
        if (skillId < 0 || skillId > 1) return false;
        float cd = GetSkillCooldownSeconds(skillId);
        return Time.time >= lastSkillTimes[skillId] + cd;
    }

    public float GetSkillCooldownSeconds(int skillId)
    {
        float baseCd = 0f;
        if (skillId == 0) baseCd = skill0Cooldown;
        else if (skillId == 1) baseCd = skill1Cooldown;
        else return 0f;

        // 技能等级：冷却略降（每级 -3%，最低到 40%）
        int lv = stats != null ? stats.GetSkillLevel(skillId) : 1;
        float factor = 1f - Mathf.Clamp01((lv - 1) * 0.03f);
        factor = Mathf.Clamp(factor, 0.4f, 1f);
        return baseCd * factor;
    }

    public float GetDodgeCooldownSeconds()
    {
        return dodgeCooldown;
    }

    private void TryDodge()
    {
        float cd = Mathf.Max(0.05f, dodgeCooldown);
        if (Time.time < lastDodgeTime + cd) return;
        lastDodgeTime = Time.time;

        // 方向：优先摇杆方向，否则用面朝方向
        Vector2 input = VirtualJoystick.GetInput();
        Vector3 dir = new Vector3(input.x, 0f, input.y);
        if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
        dir.Normalize();

        dodgeVel = dir * Mathf.Max(1f, dodgeSpeed);
        dodgeUntil = Time.time + Mathf.Clamp(dodgeDuration, 0.05f, 0.5f);
        dodgeInvulnUntil = Time.time + Mathf.Clamp(dodgeInvuln, 0.05f, 0.8f);

        // 清掉水平速度，确保闪避立刻生效
        rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
        rb.AddForce(dodgeVel, ForceMode.VelocityChange);

        PlayAction(clipSkill, lockByLength: false);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX_Skill();
        EventManager.TriggerEvent("SKILL_USED", new object[] { 2, gameObject.GetInstanceID(), transform.position });
    }

    public float GetSkillDamageMultiplier(int skillId)
    {
        float baseMult = 1f;
        if (skillId == 0) baseMult = skill0DamageMultiplier;
        else if (skillId == 1) baseMult = skill1DamageMultiplier;
        else return 1f;

        // 技能等级：倍率略升（每级 +8%）
        int lv = stats != null ? stats.GetSkillLevel(skillId) : 1;
        float factor = 1f + Mathf.Max(0, lv - 1) * 0.08f;
        return baseMult * factor;
    }

    private void ApplySkillConfigIfPresent(int skillId)
    {
        if (ConfigManager.Instance == null || ConfigManager.Instance.SkillConfigs == null) return;
        string key = skillId == 0 ? "SK001" : (skillId == 1 ? "SK002" : null);
        if (string.IsNullOrEmpty(key)) return;
        if (!ConfigManager.Instance.SkillConfigs.TryGetValue(key, out var cfg) || cfg == null) return;
        // 覆盖当前脚本参数，使 UI/测试与配置一致
        if (skillId == 0)
        {
            skill0Cooldown = cfg.cooldown;
            skill0DamageMultiplier = cfg.damage_multiplier;
        }
        else if (skillId == 1)
        {
            skill1Cooldown = cfg.cooldown;
            skill1DamageMultiplier = cfg.damage_multiplier;
        }
    }

    private System.Collections.Generic.List<Transform> FindSkillTargets(int skillId)
    {
        // 默认：cone 扇形；支持 single（单体）与 line（直线穿透）
        string key = skillId == 0 ? "SK001" : (skillId == 1 ? "SK002" : null);
        SkillConfig cfg = null;
        if (!string.IsNullOrEmpty(key) && ConfigManager.Instance != null && ConfigManager.Instance.SkillConfigs != null)
            ConfigManager.Instance.SkillConfigs.TryGetValue(key, out cfg);

        if (cfg != null && cfg.aoe_shape == "single")
        {
            var one = FindNearestEnemy();
            var list = new System.Collections.Generic.List<Transform>();
            if (one != null) list.Add(one);
            return list;
        }

        float range = cfg != null ? cfg.aoe_range : 10f;
        float angle = cfg != null ? cfg.aoe_angle : 60f;
        int maxCount = cfg != null ? cfg.arrow_count : 5;

        var enemies = GameObject.FindGameObjectsWithTag("Monster");
        var candidates = new System.Collections.Generic.List<Transform>();
        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();

        for (int i = 0; i < enemies.Length; i++)
        {
            var go = enemies[i];
            if (go == null) continue;
            var t = go.transform;
            var to = t.position - origin;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist > range) continue;
            if (dist < 0.01f) { candidates.Add(t); continue; }

            // line：直线穿透（宽度常量，避免改动配置结构）
            if (cfg != null && cfg.aoe_shape == "line")
            {
                float dot = Vector3.Dot(forward, to.normalized);
                if (dot <= 0f) continue; // 只打前方
                // 计算到前向轴的垂直距离：|to - forward*proj|
                float proj = Vector3.Dot(forward, to);
                if (proj < 0f || proj > range) continue;
                Vector3 perp = to - forward * proj;
                const float LineHalfWidth = 1.2f; // 米（Unity单位）
                if (perp.magnitude <= LineHalfWidth) candidates.Add(t);
                continue;
            }

            // cone：扇形
            float a = Vector3.Angle(forward, to.normalized);
            if (a <= angle * 0.5f) candidates.Add(t);
        }

        // 排序：line 用前向投影距离排序；其他用距离排序
        if (cfg != null && cfg.aoe_shape == "line")
        {
            candidates.Sort((a, b) =>
            {
                float da = Vector3.Dot(forward, (a.position - origin));
                float db = Vector3.Dot(forward, (b.position - origin));
                return da.CompareTo(db);
            });
        }
        else
        {
            candidates.Sort((a, b) =>
            {
                float da = (a.position - origin).sqrMagnitude;
                float db = (b.position - origin).sqrMagnitude;
                return da.CompareTo(db);
            });
        }

        // line：maxCount=0 表示不限制（穿透所有）
        if (cfg != null && cfg.aoe_shape == "line" && maxCount <= 0) return candidates;
        if (maxCount <= 0 || candidates.Count <= maxCount) return candidates;
        return candidates.GetRange(0, maxCount);
    }
}
