// 依据：GDD v2.1 第10章、程序知识库 v1.6 9.3 配置数据验证、程序基础知识库 5.7、5.9 第二层
using System.Collections.Generic;
using UnityEngine;

public class ConfigManager : MonoBehaviour
{
    public static ConfigManager Instance { get; private set; }

    // 需求知识库 v2.2 0.5：最高同屏怪物数量=15（配置与刷怪逻辑都应遵守）
    private const int MaxAliveOnScreen = 15;

    public Dictionary<int, LevelConfig> LevelConfigs { get; private set; }
    public Dictionary<int, MonsterConfig> MonsterConfigs { get; private set; }
    public Dictionary<int, EquipmentConfig> EquipmentConfigs { get; private set; }
    public Dictionary<int, DropTableConfig> DropTableConfigs { get; private set; }
    public Dictionary<string, SkillConfig> SkillConfigs { get; private set; }
    /// <summary>GDD 10.5 游戏全局配置（失败保留比例等）</summary>
    public GameConfig GameConfig { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadAllConfigs()
    {
        Debug.Log("=== ConfigManager 加载配置 ===");
        LevelConfigs = new Dictionary<int, LevelConfig>();
        MonsterConfigs = new Dictionary<int, MonsterConfig>();
        EquipmentConfigs = new Dictionary<int, EquipmentConfig>();
        DropTableConfigs = new Dictionary<int, DropTableConfig>();
        SkillConfigs = new Dictionary<string, SkillConfig>();

        LoadLevelConfigs();
        LoadMonsterConfigs();
        LoadEquipmentConfigs();
        LoadDropTableConfigs();
        LoadSkillConfigs();
        LoadGameConfig();

        // 二次校验：引用完整性/资源路径验证（需要所有配置都已加载）
        ValidateCrossReferences();
    }

    private void LoadLevelConfigs()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Config/LevelConfigs");
        if (jsonFile == null)
        {
            Debug.LogWarning("关卡配置文件未找到 Config/LevelConfigs");
            return;
        }
        var list = JsonUtility.FromJson<LevelConfigList>(jsonFile.text);
        if (list?.levels != null)
        {
            foreach (var c in list.levels)
            {
                LevelConfigs[c.level_id] = c;
                ValidateLevelConfig(c);
            }
            Debug.Log($"加载关卡配置: {LevelConfigs.Count} 个");
        }
    }

    private void LoadMonsterConfigs()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Config/MonsterConfigs");
        if (jsonFile == null)
        {
            Debug.LogWarning("怪物配置文件未找到 Config/MonsterConfigs");
            return;
        }
        var list = JsonUtility.FromJson<MonsterConfigList>(jsonFile.text);
        if (list?.monsters != null)
        {
            foreach (var c in list.monsters)
            {
                MonsterConfigs[c.monster_id] = c;
                ValidateMonsterConfig(c);
            }
            Debug.Log($"加载怪物配置: {MonsterConfigs.Count} 个");
        }
    }

    private void LoadEquipmentConfigs()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Config/EquipmentConfigs");
        if (jsonFile == null)
        {
            Debug.LogWarning("装备配置文件未找到 Config/EquipmentConfigs");
            return;
        }
        var list = JsonUtility.FromJson<EquipmentConfigList>(jsonFile.text);
        if (list?.equipments != null)
        {
            foreach (var c in list.equipments)
            {
                EquipmentConfigs[c.equipment_id] = c;
                ValidateEquipmentConfig(c);
            }
            Debug.Log($"加载装备配置: {EquipmentConfigs.Count} 个");
        }
    }

    private void LoadDropTableConfigs()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Config/DropTableConfigs");
        if (jsonFile == null)
        {
            Debug.LogWarning("掉落表配置文件未找到 Config/DropTableConfigs");
            return;
        }
        var list = JsonUtility.FromJson<DropTableConfigList>(jsonFile.text);
        if (list?.drop_tables != null)
        {
            foreach (var c in list.drop_tables)
            {
                DropTableConfigs[c.drop_table_id] = c;
                ValidateDropTableConfig(c);
            }
            Debug.Log($"加载掉落表配置: {DropTableConfigs.Count} 个");
        }
    }

    private void LoadSkillConfigs()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Config/SkillConfigs");
        if (jsonFile == null)
        {
            Debug.LogWarning("技能配置文件未找到 Config/SkillConfigs（将使用脚本默认技能参数）");
            return;
        }
        var list = JsonUtility.FromJson<SkillConfigList>(jsonFile.text);
        if (list?.skills != null)
        {
            foreach (var c in list.skills)
            {
                if (c == null || string.IsNullOrEmpty(c.skill_id)) continue;
                SkillConfigs[c.skill_id] = c;
                ValidateSkillConfig(c);
            }
            Debug.Log($"加载技能配置: {SkillConfigs.Count} 个");
        }
    }

    private void LoadGameConfig()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Config/GameConfig");
        if (jsonFile == null)
        {
            Debug.LogWarning("GameConfig 未找到 Config/GameConfig，使用默认失败保留比例 0.5");
            GameConfig = new GameConfig { exp_retain_ratio = 0.5f, gold_retain_ratio = 0.5f };
            return;
        }
        GameConfig = JsonUtility.FromJson<GameConfig>(jsonFile.text);
        if (GameConfig == null) GameConfig = new GameConfig { exp_retain_ratio = 0.5f, gold_retain_ratio = 0.5f };
        ValidateGameConfig(GameConfig);
        Debug.Log($"加载 GameConfig: exp_retain_ratio={GameConfig.exp_retain_ratio}, gold_retain_ratio={GameConfig.gold_retain_ratio}");
    }

    private void ValidateGameConfig(GameConfig c)
    {
        if (c.exp_retain_ratio < 0f || c.exp_retain_ratio > 1f)
            Debug.LogWarning($"GameConfig exp_retain_ratio 超出 [0,1]，已钳制: {c.exp_retain_ratio}");
        c.exp_retain_ratio = Mathf.Clamp01(c.exp_retain_ratio);
        if (c.gold_retain_ratio < 0f || c.gold_retain_ratio > 1f)
            Debug.LogWarning($"GameConfig gold_retain_ratio 超出 [0,1]，已钳制: {c.gold_retain_ratio}");
        c.gold_retain_ratio = Mathf.Clamp01(c.gold_retain_ratio);
    }

    private void ValidateEquipmentConfig(EquipmentConfig c)
    {
        if (c.equipment_id <= 0) Debug.LogWarning($"EquipmentConfig equipment_id 无效: {c.equipment_id}");
        if (string.IsNullOrEmpty(c.type)) Debug.LogWarning($"EquipmentConfig type 为空: equipment_id={c.equipment_id}");
        if (c.attack_bonus < 0) Debug.LogWarning($"EquipmentConfig attack_bonus 无效: equipment_id={c.equipment_id}");
        if (c.defense_bonus < 0) Debug.LogWarning($"EquipmentConfig defense_bonus 无效: equipment_id={c.equipment_id}");
    }

    private void ValidateDropTableConfig(DropTableConfig c)
    {
        if (c.drop_table_id <= 0) Debug.LogWarning($"DropTableConfig drop_table_id 无效: {c.drop_table_id}");
        if (c.drops == null || c.drops.Count == 0) Debug.LogWarning($"DropTableConfig drops 为空: drop_table_id={c.drop_table_id}");
        if (c.drops != null)
        {
            for (int i = 0; i < c.drops.Count; i++)
            {
                var d = c.drops[i];
                if (d == null) continue;
                if (d.probability < 0f || d.probability > 1f)
                    Debug.LogWarning($"DropEntry probability 建议在 [0,1]: drop_table_id={c.drop_table_id}, item_type={d.item_type}");
                if (string.IsNullOrEmpty(d.item_type))
                    Debug.LogWarning($"DropEntry item_type 为空: drop_table_id={c.drop_table_id}");
                // 首版允许：gold/equipment/potion
                if (d.item_type != "gold" && d.item_type != "equipment" && d.item_type != "potion")
                    Debug.LogWarning($"DropEntry item_type 未识别: {d.item_type} (drop_table_id={c.drop_table_id})");
            }
        }
    }

    private void ValidateLevelConfig(LevelConfig c)
    {
        if (c.level_id <= 0) Debug.LogWarning($"LevelConfig level_id 无效: {c.level_id}");
        if (string.IsNullOrEmpty(c.scene_name)) Debug.LogWarning($"LevelConfig scene_name 为空: level_id={c.level_id}");
        if (c.waves == null || c.waves.Count == 0) Debug.LogWarning($"LevelConfig waves 为空: level_id={c.level_id}");
        if (c.recommended_level < 0) Debug.LogWarning($"LevelConfig recommended_level 无效: level_id={c.level_id}");
        if (c.reward_gold < 0) Debug.LogWarning($"LevelConfig reward_gold 无效: level_id={c.level_id}");
        if (c.reward_exp < 0) Debug.LogWarning($"LevelConfig reward_exp 无效: level_id={c.level_id}");

        // 护栏：单波刷怪总量不应超过 MaxAliveOnScreen（刷怪器也会做运行时节流+不叠波）
        if (c.waves != null)
        {
            for (int wi = 0; wi < c.waves.Count; wi++)
            {
                var w = c.waves[wi];
                if (w == null) continue;
                int sum = 0;
                if (w.monsters != null)
                {
                    for (int mi = 0; mi < w.monsters.Count; mi++)
                    {
                        var e = w.monsters[mi];
                        if (e == null) continue;
                        sum += Mathf.Max(0, e.count);
                    }
                }
                if (sum > MaxAliveOnScreen)
                    Debug.LogWarning($"LevelConfig 单波怪物总数={sum} 超过同屏护栏 {MaxAliveOnScreen}: level_id={c.level_id}, wave_id={w.wave_id}");
            }
        }
    }

    private void ValidateMonsterConfig(MonsterConfig c)
    {
        if (c.monster_id <= 0) Debug.LogWarning($"MonsterConfig monster_id 无效: {c.monster_id}");
        if (c.hp <= 0 || c.hp > 10000) Debug.LogWarning($"MonsterConfig hp 超出护栏: monster_id={c.monster_id}, hp={c.hp}");
        if (c.attack < 0) Debug.LogWarning($"MonsterConfig attack 无效: monster_id={c.monster_id}");
        // 允许 prefab_path 为空：运行时会生成占位怪物（MonsterSpawner.SpawnMonster 的 fallback）
    }

    private void ValidateSkillConfig(SkillConfig c)
    {
        if (string.IsNullOrEmpty(c.skill_id)) Debug.LogWarning("SkillConfig skill_id 为空");
        if (c.cooldown < 0f) Debug.LogWarning($"SkillConfig cooldown 无效: {c.skill_id}");
        if (c.damage_multiplier <= 0f) Debug.LogWarning($"SkillConfig damage_multiplier 无效: {c.skill_id}");
        if (c.aoe_range < 0f) Debug.LogWarning($"SkillConfig aoe_range 无效: {c.skill_id}");
        if (c.aoe_angle < 0f || c.aoe_angle > 180f) Debug.LogWarning($"SkillConfig aoe_angle 建议在 [0,180]: {c.skill_id}");
    }

    /// <summary>
    /// 二次校验：引用完整性 + 资源路径验证（需要所有配置加载后才可检查）。
    /// 依据：程序知识库（Unity）v2.1.1 9.3 配置数据验证机制。
    /// </summary>
    private void ValidateCrossReferences()
    {
        // Level -> Monster 引用完整性
        if (LevelConfigs != null && MonsterConfigs != null)
        {
            foreach (var kv in LevelConfigs)
            {
                var level = kv.Value;
                if (level == null) continue;

                if (level.waves != null)
                {
                    for (int wi = 0; wi < level.waves.Count; wi++)
                    {
                        var w = level.waves[wi];
                        if (w?.monsters == null) continue;
                        for (int mi = 0; mi < w.monsters.Count; mi++)
                        {
                            var e = w.monsters[mi];
                            if (e == null) continue;
                            if (!MonsterConfigs.ContainsKey(e.monster_id))
                                Debug.LogWarning($"LevelConfig 引用了不存在的 monster_id: level_id={level.level_id}, wave_id={w.wave_id}, monster_id={e.monster_id}");
                        }
                    }
                }

                if (level.boss != null && level.boss.monster_id != 0 && !MonsterConfigs.ContainsKey(level.boss.monster_id))
                    Debug.LogWarning($"LevelConfig 引用了不存在的 Boss monster_id: level_id={level.level_id}, boss_id={level.boss.monster_id}");
            }
        }

        // Monster -> DropTable + Monster prefab_path
        if (MonsterConfigs != null)
        {
            foreach (var kv in MonsterConfigs)
            {
                var m = kv.Value;
                if (m == null) continue;

                if (m.drop_table_id > 0 && (DropTableConfigs == null || !DropTableConfigs.ContainsKey(m.drop_table_id)))
                    Debug.LogWarning($"MonsterConfig 引用了不存在的 drop_table_id: monster_id={m.monster_id}, drop_table_id={m.drop_table_id}");

                if (!string.IsNullOrEmpty(m.prefab_path))
                {
                    var prefab = Resources.Load<GameObject>(m.prefab_path);
                    if (prefab == null)
                        Debug.LogWarning($"MonsterConfig prefab_path 无法 Resources.Load: monster_id={m.monster_id}, path={m.prefab_path}");
                }
            }
        }

        // DropTable -> Equipment
        if (DropTableConfigs != null && EquipmentConfigs != null)
        {
            foreach (var kv in DropTableConfigs)
            {
                var t = kv.Value;
                if (t?.drops == null) continue;
                for (int i = 0; i < t.drops.Count; i++)
                {
                    var d = t.drops[i];
                    if (d == null) continue;
                    if (d.item_type == "equipment" && d.item_id > 0 && !EquipmentConfigs.ContainsKey(d.item_id))
                        Debug.LogWarning($"DropTableConfig 引用了不存在的 equipment_id: drop_table_id={t.drop_table_id}, equipment_id={d.item_id}");
                }
            }
        }

        // Equipment icon_path（可选：缺失不阻塞，但给出提示）
        if (EquipmentConfigs != null)
        {
            foreach (var kv in EquipmentConfigs)
            {
                var e = kv.Value;
                if (e == null) continue;
                if (!string.IsNullOrEmpty(e.icon_path))
                {
                    var sprite = Resources.Load<Sprite>(e.icon_path);
                    if (sprite == null)
                        Debug.LogWarning($"EquipmentConfig icon_path 无法 Resources.Load: equipment_id={e.equipment_id}, path={e.icon_path}");
                }
            }
        }
    }
}
