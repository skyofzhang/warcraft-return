// 依据：GDD v2.1 第10章、程序基础知识库 5.9 第一层
using System;
using System.Collections.Generic;

[Serializable]
public class LevelConfig
{
    public int level_id;
        public string level_name;
        public string scene_name;
    // M3：扩展字段（未配置时为 0/默认）
    public int recommended_level;
    public int reward_gold;
    public int reward_exp;
    public List<WaveConfig> waves;
    public BossConfig boss;
}

[Serializable]
public class WaveConfig
{
    public int wave_id;
    public List<MonsterWaveEntry> monsters;
}

[Serializable]
public class MonsterWaveEntry
{
    public int monster_id;
    public int count;
    public List<string> spawn_points;
}

[Serializable]
public class BossConfig
{
    public int monster_id;
    public string spawn_point;
}

[Serializable]
public class LevelConfigList
{
    public List<LevelConfig> levels;
}

[Serializable]
public class MonsterConfig
{
    public int monster_id;
    public string name;
    public string prefab_path;
    public int hp;
    public int attack;
    public int defense;
    public float move_speed;
    public float attack_range;
    public int drop_table_id;
}

[Serializable]
public class MonsterConfigList
{
    public List<MonsterConfig> monsters;
}

[Serializable]
public class EquipmentConfig
{
    public int equipment_id;
    public string name;
    public string type;
    public string quality;
    public string icon_path;
    public int attack_bonus;
    public int defense_bonus;
}

[Serializable]
public class EquipmentConfigList
{
    public List<EquipmentConfig> equipments;
}

[Serializable]
public class DropEntry
{
    public string item_type;
    public int item_id;
    public int count_min;
    public int count_max;
    public float probability;
}

[Serializable]
public class DropTableConfig
{
    public int drop_table_id;
    public List<DropEntry> drops;
}

[Serializable]
public class DropTableConfigList
{
    public List<DropTableConfig> drop_tables;
}

// GDD 3.2 技能配置（首版最小：SK001 多重箭、SK002 穿透箭）
[Serializable]
public class SkillConfig
{
    public string skill_id;
    public string skill_name;
    public float cooldown = 8f;
    public float damage_multiplier = 0.8f;
    public int arrow_count = 5;
    public string aoe_shape = "cone"; // cone / line / single
    public float aoe_range = 10f;
    public float aoe_angle = 60f;
}

[Serializable]
public class SkillConfigList
{
    public List<SkillConfig> skills;
}

// GDD 10.5 游戏全局配置（失败保留比例等）
[Serializable]
public class GameConfig
{
    public float exp_retain_ratio = 0.5f;
    public float gold_retain_ratio = 0.5f;
}
