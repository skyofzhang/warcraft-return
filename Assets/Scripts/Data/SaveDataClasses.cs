// 依据：GDD 3.3 成长系统、10.3 装备系统；P0 玩家数据持久化（金币/经验/等级/背包/穿戴）
using System;
using System.Collections.Generic;

[Serializable]
public class IntIntPair
{
    public int key;
    public int value;
}

[Serializable]
public class StringIntPair
{
    public string key;
    public int value;
}

[Serializable]
public class PlayerSaveData
{
    /// <summary>职业（首版默认：猎人）。用于 UI-06 显示；后续可扩展多职业。</summary>
    public string profession = "猎人";
    public int gold;
    public int exp;
    public int level;
    /// <summary>已解锁的最高关卡（1~10）。</summary>
    public int unlocked_level_id = 1;
    /// <summary>治疗瓶数量（首版：初始 3）。</summary>
    public int potion_count = 3;
    // UI-07 技能：首版2个技能的等级（用于保存/显示/战斗倍率修正）
    public int skill_lv_sk001 = 1;
    public int skill_lv_sk002 = 1;
}

[Serializable]
public class EquipmentSaveData
{
    public List<IntIntPair> inventory = new List<IntIntPair>();
    public List<StringIntPair> equipped = new List<StringIntPair>();
}

[Serializable]
public class SettingsSaveData
{
    public float bgmVolume = 0.7f;
    public float sfxVolume = 1f;

    /// <summary>画质档位：0=低，1=中，2=高（UI策划案 v3.2 UISettings）。</summary>
    public int qualityLevel = 1;
    /// <summary>目标帧率：30/60（UI策划案 v3.2 UISettings）。</summary>
    public int targetFps = 60;
    /// <summary>语言：简体中文/English（首版仅保存，不做多语言资源切换）。</summary>
    public string language = "zh-CN";
}

[Serializable]
public class GameSaveData
{
    public int version = 2;
    public PlayerSaveData player = new PlayerSaveData();
    public EquipmentSaveData equipment = new EquipmentSaveData();
    public SettingsSaveData settings = new SettingsSaveData();
}

