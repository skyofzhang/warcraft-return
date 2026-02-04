// 依据：程序基础知识库 5.2、5.9 第四层；GDD 10.1 关卡波次
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [Header("关卡")]
    public int levelId = 1;

    [Header("性能护栏（需求知识库 v2.2 0.5：最高同屏怪物数量=15）")]
    public int maxAliveOnScreen = 15;

    [Header("刷怪点（空物体名需与配置中 spawn_points 一致）")]
    public Transform[] spawnPoints;

    [Header("战斗边界（墙体/碰撞）")]
    public bool buildBoundaryWalls = true;
    public float boundaryHalfSize = 25f;
    public float boundaryWallHeight = 5f;
    public float boundaryWallThickness = 1f;

    private Dictionary<string, Transform> spawnPointMap = new Dictionary<string, Transform>();
    private int currentWaveIndex;
    /// <summary>本关已刷出且仍存活的怪物数；归零且刷怪结束后触发胜利（GDD 2.1、核验报告 P0-1）。</summary>
    private int aliveCount;
    /// <summary>本关波次+BOSS 是否已全部刷完。</summary>
    private bool spawnFinished;

    private void Start()
    {
        EnsureBoundaryWalls();

        // 从 GameManager 同步当前关卡 ID（允许选关后生效）
        if (GameManager.Instance != null && GameManager.Instance.CurrentLevelId > 0)
            levelId = GameManager.Instance.CurrentLevelId;

        foreach (Transform t in spawnPoints)
        {
            if (t != null && !string.IsNullOrEmpty(t.name))
                spawnPointMap[t.name] = t;
        }
        EventManager.AddListener("MONSTER_KILLED", OnMonsterKilled);
        if (ConfigManager.Instance != null && ConfigManager.Instance.LevelConfigs.TryGetValue(levelId, out LevelConfig level))
        {
            StartCoroutine(RunWaves(level));
        }
        else if (ConfigManager.Instance != null && ConfigManager.Instance.LevelConfigs.TryGetValue(1, out LevelConfig fallback))
        {
            Debug.LogWarning($"[MonsterSpawner] LevelConfig 缺失: levelId={levelId}，回退到 level 1。");
            StartCoroutine(RunWaves(fallback));
        }
    }

    private void OnDestroy()
    {
        EventManager.RemoveListener("MONSTER_KILLED", OnMonsterKilled);
    }

    private void OnMonsterKilled(object _)
    {
        aliveCount = Mathf.Max(0, aliveCount - 1);
        if (spawnFinished && aliveCount <= 0 && GameManager.Instance != null)
            GameManager.Instance.EndGame(true);
    }

    private void EnsureBoundaryWalls()
    {
        if (!buildBoundaryWalls) return;
        if (GameObject.Find("BoundaryWalls") != null) return;

        var root = new GameObject("BoundaryWalls");
        float half = Mathf.Max(1f, boundaryHalfSize);
        float h = Mathf.Max(0.5f, boundaryWallHeight);
        float t = Mathf.Max(0.2f, boundaryWallThickness);
        float y = h * 0.5f;

        // 四面墙：围住 X/Z 在 [-half, half] 的区域
        CreateWall(root.transform, "Wall_North", new Vector3(0f, y, half + t * 0.5f), new Vector3(half * 2f + t, h, t));
        CreateWall(root.transform, "Wall_South", new Vector3(0f, y, -half - t * 0.5f), new Vector3(half * 2f + t, h, t));
        CreateWall(root.transform, "Wall_East", new Vector3(half + t * 0.5f, y, 0f), new Vector3(t, h, half * 2f + t));
        CreateWall(root.transform, "Wall_West", new Vector3(-half - t * 0.5f, y, 0f), new Vector3(t, h, half * 2f + t));
    }

    private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.position = position;
        go.transform.localScale = scale;
        go.isStatic = true;

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0) go.layer = groundLayer;

        var r = go.GetComponent<Renderer>();
        if (r != null) r.material.color = new Color(0.08f, 0.08f, 0.08f, 1f);
    }

    private IEnumerator RunWaves(LevelConfig level)
    {
        if (level.waves == null || level.waves.Count == 0)
        {
            spawnFinished = true;
            yield break;
        }

        for (int i = 0; i < level.waves.Count; i++)
        {
            WaveConfig wave = level.waves[i];
            EventManager.TriggerEvent("WAVE_STARTED", wave.wave_id);

            foreach (MonsterWaveEntry entry in wave.monsters)
            {
                if (entry == null) continue;
                if (ConfigManager.Instance == null || ConfigManager.Instance.MonsterConfigs == null) continue;
                if (!ConfigManager.Instance.MonsterConfigs.TryGetValue(entry.monster_id, out MonsterConfig config))
                    continue;

                int toSpawn = Mathf.Max(0, entry.count);
                for (int c = 0; c < toSpawn; c++)
                {
                    // 暂停时不推进刷怪流程
                    while (Time.timeScale == 0f)
                        yield return null;

                    // 同屏护栏：超过上限则等待怪物被击杀
                    int cap = Mathf.Clamp(maxAliveOnScreen, 1, 999);
                    while (aliveCount >= cap)
                    {
                        while (Time.timeScale == 0f)
                            yield return null;
                        yield return null;
                    }

                    string pointName = null;
                    if (entry.spawn_points != null && entry.spawn_points.Count > 0)
                        pointName = entry.spawn_points[c % entry.spawn_points.Count];
                    SpawnMonster(config, pointName);
                }
            }

            // 波次结束条件：等待该波次怪物全部被击杀（避免波次叠加导致同屏怪物数爆炸）
            while (aliveCount > 0)
            {
                while (Time.timeScale == 0f)
                    yield return null;
                yield return null;
            }

            EventManager.TriggerEvent("WAVE_COMPLETED", wave.wave_id);
        }

        if (level.boss != null)
        {
            EventManager.TriggerEvent("BOSS_SPAWNED", new object[] { level.boss.monster_id, Vector3.zero });
            if (ConfigManager.Instance != null && ConfigManager.Instance.MonsterConfigs != null &&
                ConfigManager.Instance.MonsterConfigs.TryGetValue(level.boss.monster_id, out MonsterConfig bossConfig))
            {
                while (Time.timeScale == 0f)
                    yield return null;
                int cap = Mathf.Clamp(maxAliveOnScreen, 1, 999);
                while (aliveCount >= cap)
                {
                    while (Time.timeScale == 0f)
                        yield return null;
                    yield return null;
                }
                SpawnMonster(bossConfig, level.boss.spawn_point);
            }
        }
        spawnFinished = true;
    }

    private void SpawnMonster(MonsterConfig monsterConfig, string pointName)
    {
        GameObject prefab = Resources.Load<GameObject>(monsterConfig.prefab_path);
        if (prefab == null)
        {
            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fallback.name = monsterConfig.name + "_Placeholder";
            fallback.tag = "Monster";
            var ms = fallback.AddComponent<MonsterStats>();
            ms.InitFromConfig(monsterConfig);
            fallback.AddComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;
            fallback.AddComponent<MonsterController>();
            // Attach CC0 monster visual if available
            CharacterVisualFactory.AttachVisual(fallback, CharacterVisualFactory.GetMonsterModelPath(monsterConfig.monster_id));
            fallback.transform.position = GetSpawnPosition(pointName);
            fallback.transform.localScale = new Vector3(0.4f, 0.8f, 0.4f);
            aliveCount++;
            return;
        }
        Vector3 pos = GetSpawnPosition(pointName);
        GameObject go = Instantiate(prefab, pos, Quaternion.identity);
        var stats = go.GetComponent<MonsterStats>();
        if (stats != null) stats.InitFromConfig(monsterConfig);
        aliveCount++;
    }

    private Vector3 GetSpawnPosition(string pointName)
    {
        if (!string.IsNullOrEmpty(pointName) && spawnPointMap.TryGetValue(pointName, out Transform t))
            return t.position;
        return transform.position + Random.insideUnitSphere * 3f;
    }
}
