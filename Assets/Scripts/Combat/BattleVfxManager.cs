using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight runtime VFX manager:
/// - hit sparks / crit burst
/// - skill VFX (arrow-like traces)
/// Uses Kenney particle textures under Resources/VFX/KenneyParticlePack (CC0).
/// </summary>
public class BattleVfxManager : MonoBehaviour
{
    private static BattleVfxManager s_instance;

    private static readonly Dictionary<int, Transform> s_idToTransform = new Dictionary<int, Transform>();
    private System.Action<object> onDamageDealt;
    private System.Action<object> onCrit;
    private System.Action<object> onSkill;
    private System.Action<object> onBasicAttack;
    private System.Action<object> onMonsterAttack;

    private Material matUnlit;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (s_instance != null) return;
        var go = new GameObject("__BattleVfx");
        DontDestroyOnLoad(go);
        s_instance = go.AddComponent<BattleVfxManager>();
    }

    private void Awake()
    {
        if (s_instance != null && s_instance != this) { Destroy(gameObject); return; }
        s_instance = this;

        // Prefer URP particle shader, fallback to built-in particle shaders.
        var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
        if (sh == null) sh = Shader.Find("Particles/Additive");
        if (sh != null) matUnlit = new Material(sh);
    }

    private void OnEnable()
    {
        onDamageDealt = OnDamage;
        onCrit = OnCrit;
        onSkill = OnSkill;
        EventManager.AddListener("DAMAGE_DEALT", onDamageDealt);
        EventManager.AddListener("CRITICAL_HIT", onCrit);
        EventManager.AddListener("SKILL_USED", onSkill);
        onBasicAttack = OnBasicAttack;
        onMonsterAttack = OnMonsterAttack;
        EventManager.AddListener("BASIC_ATTACK", onBasicAttack);
        EventManager.AddListener("MONSTER_ATTACK", onMonsterAttack);
    }

    private void OnDisable()
    {
        if (onDamageDealt != null) EventManager.RemoveListener("DAMAGE_DEALT", onDamageDealt);
        if (onCrit != null) EventManager.RemoveListener("CRITICAL_HIT", onCrit);
        if (onSkill != null) EventManager.RemoveListener("SKILL_USED", onSkill);
        if (onBasicAttack != null) EventManager.RemoveListener("BASIC_ATTACK", onBasicAttack);
        if (onMonsterAttack != null) EventManager.RemoveListener("MONSTER_ATTACK", onMonsterAttack);
    }

    private void OnDamage(object data)
    {
        // payload: [damage(float), isCrit(bool), targetInstanceId(int)]
        if (!(data is object[] arr) || arr.Length < 3) return;
        float dmg = arr[0] is float f ? f : 0f;
        if (dmg <= 0f) return;
        bool isCrit = arr[1] is bool b && b;
        int id = arr[2] is int iid ? iid : 0;

        var pos = ResolveWorldPos(id);
        if (pos == Vector3.zero) return;

        SpawnHit(pos, isCrit);
        TryHitFlash(id);
    }

    private void OnCrit(object data)
    {
        // payload: [damage(float), targetInstanceId(int)]
        if (!(data is object[] arr) || arr.Length < 2) return;
        int id = arr[1] is int iid ? iid : 0;
        var pos = ResolveWorldPos(id);
        if (pos == Vector3.zero) return;
        SpawnBurst(pos, texName: "star_07", color: new Color(1f, 0.85f, 0.2f, 1f), count: 18, size: 0.55f, speed: 5.8f, life: 0.45f);
    }

    private void OnSkill(object data)
    {
        // payload: [skillId(int), casterInstanceId(int), pos(Vector3)]
        if (!(data is object[] arr) || arr.Length < 3) return;
        int skillId = arr[0] is int i0 ? i0 : 0;
        int casterId = arr[1] is int i1 ? i1 : 0;
        Vector3 pos = arr[2] is Vector3 v ? v : Vector3.zero;
        var caster = ResolveTransform(casterId);
        if (caster == null) return;

        if (skillId == 0)
        {
            // Multi-arrow: muzzle flash + several traces forward
            SpawnBurst(caster.position + Vector3.up * 1.2f, "muzzle_03", new Color(1f, 0.85f, 0.45f, 1f), 10, 0.35f, 6.2f, 0.18f);
            SpawnTraces(caster.position + Vector3.up * 1.1f, caster.forward, 6, spreadDeg: 18f, texName: "trace_03", color: new Color(1f, 0.95f, 0.75f, 1f));
        }
        else if (skillId == 2)
        {
            // Dodge: smoke puff + short twirl
            SpawnBurst(caster.position + Vector3.up * 0.8f, "smoke_05", new Color(0.85f, 0.9f, 1f, 1f), 14, 0.55f, 2.6f, 0.55f);
            SpawnBurst(caster.position + Vector3.up * 0.9f, "twirl_01", new Color(0.4f, 0.85f, 1f, 1f), 10, 0.45f, 2.2f, 0.45f);
        }
        else
        {
            // Pierce arrow: one strong trace + magic swirl at first hit
            SpawnTraces(caster.position + Vector3.up * 1.1f, caster.forward, 1, spreadDeg: 0f, texName: "trace_06", color: new Color(0.75f, 0.9f, 1f, 1f), speed: 14f, life: 0.28f);
            if (pos != Vector3.zero)
                SpawnBurst(pos + Vector3.up * 0.8f, "magic_04", new Color(0.4f, 0.85f, 1f, 1f), 14, 0.45f, 3.5f, 0.55f);
        }
    }

    private void OnBasicAttack(object data)
    {
        // payload: [attackerId(int), targetId(int), targetPos(Vector3)]
        if (!(data is object[] arr) || arr.Length < 3) return;
        int attackerId = arr[0] is int a ? a : 0;
        int targetId = arr[1] is int t ? t : 0;
        Vector3 targetPos = arr[2] is Vector3 p ? p : Vector3.zero;
        var attacker = ResolveTransform(attackerId);
        if (attacker == null) return;

        // muzzle + trace to target
        SpawnBurst(attacker.position + Vector3.up * 1.15f, "muzzle_02", new Color(1f, 0.9f, 0.55f, 1f), 8, 0.26f, 5.2f, 0.16f);
        var dir = (targetPos != Vector3.zero) ? (targetPos - attacker.position) : attacker.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = attacker.forward;
        SpawnDirectional(attacker.position + Vector3.up * 1.05f, dir.normalized, "trace_02", new Color(1f, 0.95f, 0.8f, 1f), 12f, 0.22f, 0.18f);
        TryHitFlash(attackerId);
    }

    private void OnMonsterAttack(object data)
    {
        // payload: [attackerId(int), targetId(int)]
        if (!(data is object[] arr) || arr.Length < 2) return;
        int attackerId = arr[0] is int a ? a : 0;
        int targetId = arr[1] is int t ? t : 0;
        var attacker = ResolveTransform(attackerId);
        var target = ResolveTransform(targetId);
        if (attacker == null || target == null) return;

        Vector3 dir = (target.position - attacker.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = attacker.forward;
        SpawnDirectional(attacker.position + Vector3.up * 0.9f, dir.normalized, "slash_03", new Color(1f, 0.9f, 0.9f, 1f), 6.5f, 0.18f, 0.28f);
        TryHitFlash(attackerId);
    }

    private void SpawnHit(Vector3 pos, bool crit)
    {
        var c = crit ? new Color(1f, 0.8f, 0.25f, 1f) : new Color(1f, 1f, 1f, 1f);
        SpawnBurst(pos + Vector3.up * 0.35f, texName: "spark_06", color: c, count: crit ? 14 : 9, size: crit ? 0.38f : 0.28f, speed: crit ? 4.8f : 3.6f, life: 0.22f);
    }

    private void SpawnTraces(Vector3 origin, Vector3 forward, int count, float spreadDeg, string texName, Color color, float speed = 10f, float life = 0.25f)
    {
        for (int i = 0; i < count; i++)
        {
            float yaw = (count == 1) ? 0f : Mathf.Lerp(-spreadDeg, spreadDeg, i / Mathf.Max(1f, count - 1f));
            var dir = Quaternion.Euler(0f, yaw, 0f) * forward.normalized;
            SpawnDirectional(origin, dir, texName, color, speed, life, size: 0.22f);
        }
    }

    private void SpawnDirectional(Vector3 origin, Vector3 dir, string texName, Color color, float speed, float life, float size)
    {
        var tex = LoadTex(texName);
        if (tex == null) return;
        var go = new GameObject("_VFX_Trace");
        go.transform.position = origin;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = life;
        main.startLifetime = life;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 64;

        var em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 0f;
        shape.radius = 0.01f;
        shape.rotation = Quaternion.LookRotation(dir, Vector3.up).eulerAngles;

        var colOverLife = ps.colorOverLifetime;
        colOverLife.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colOverLife.color = grad;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.material = BuildMat(tex, color);

        ps.Play(true);
        Destroy(go, life + 0.25f);
    }

    private void SpawnBurst(Vector3 pos, string texName, Color color, int count, float size, float speed, float life)
    {
        var tex = LoadTex(texName);
        if (tex == null) return;
        var go = new GameObject("_VFX_Burst");
        go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = life;
        main.startLifetime = life;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.Clamp(count * 2, 16, 256);

        var em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(count, 1, 200)) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        vel.radial = 1.0f;

        var colOverLife = ps.colorOverLifetime;
        colOverLife.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colOverLife.color = grad;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.material = BuildMat(tex, color);

        ps.Play(true);
        Destroy(go, life + 0.25f);
    }

    private Texture2D LoadTex(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        return Resources.Load<Texture2D>($"VFX/KenneyParticlePack/{name}");
    }

    private Material BuildMat(Texture2D tex, Color tint)
    {
        if (matUnlit == null || tex == null) return null;
        var m = new Material(matUnlit);
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
        if (m.HasProperty("_Color")) m.SetColor("_Color", Color.white);
        return m;
    }

    private static Vector3 ResolveWorldPos(int instanceId)
    {
        var t = ResolveTransform(instanceId);
        return t != null ? (t.position + Vector3.up * 1.0f) : Vector3.zero;
    }

    private static Transform ResolveTransform(int instanceId)
    {
        if (instanceId == 0) return null;

        if (s_idToTransform.TryGetValue(instanceId, out var cached) && cached != null &&
            cached.gameObject != null && cached.gameObject.GetInstanceID() == instanceId)
            return cached;

        s_idToTransform.Remove(instanceId);

        var all = Object.FindObjectsOfType<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            var t = all[i];
            if (t != null && t.gameObject != null && t.gameObject.GetInstanceID() == instanceId)
            {
                s_idToTransform[instanceId] = t;
                return t;
            }
        }
        return null;
    }

    private static void TryHitFlash(int instanceId)
    {
        var t = ResolveTransform(instanceId);
        if (t == null) return;
        var flash = t.GetComponentInChildren<HitFlash>(true);
        if (flash != null) flash.Play();
    }
}

