// 依据：程序基础知识库 5.8（交付前自检）、GDD 3.3/10.3；P0 玩家数据持久化
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 本地存档系统：使用 PlayerPrefs 持久化玩家进度/背包/穿戴/设置。
/// - JsonUtility 不支持 Dictionary，因此以 List<Pair> 形式序列化。
/// - 读写时机由 GameManager 统一触发（进关/回主菜单/暂停/退出）。
/// </summary>
public static class SaveSystem
{
    private const string SaveKey = "WR_SAVE_V1";
    private const int CurrentSaveVersion = 2;
    private static GameSaveData cached;
    private static bool loaded;

    public static GameSaveData GetCached()
    {
        EnsureLoaded();
        return cached;
    }

    public static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;
        cached = LoadInternal() ?? new GameSaveData();
        if (cached == null) cached = new GameSaveData();

        // 兼容迁移：旧存档字段缺失时补默认值，并抬升 version（不改 SaveKey，避免丢存档）
        if (cached.settings == null) cached.settings = new SettingsSaveData();
        if (cached.player == null) cached.player = new PlayerSaveData();
        if (cached.equipment == null) cached.equipment = new EquipmentSaveData();

        // v2+：职业字段（首版默认“猎人”）；旧存档缺失时补默认，避免 UI-06 出现空值
        if (cached.player != null && string.IsNullOrEmpty(cached.player.profession))
            cached.player.profession = "猎人";

        if (cached.version <= 0) cached.version = 1;
        if (cached.version < CurrentSaveVersion)
        {
            // v2：新增 qualityLevel/targetFps/language 字段（缺失时用类默认值）
            cached.version = CurrentSaveVersion;
            SaveInternal(cached);
        }
    }

    public static void SaveNow()
    {
        EnsureLoaded();
        SaveInternal(cached);
    }

    /// <summary>从当前运行态抓取（PlayerStats/EquipmentManager/AudioManager）并写入缓存。</summary>
    public static void CaptureFromRuntime()
    {
        EnsureLoaded();

        // Player
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        var ps = playerGo != null ? playerGo.GetComponent<PlayerStats>() : null;
        if (ps != null)
            cached.player = ps.ExportSaveData();

        // Equipment
        if (EquipmentManager.Instance != null)
            cached.equipment = EquipmentManager.Instance.ExportSaveData();

        // Settings
        if (AudioManager.Instance != null)
        {
            cached.settings.bgmVolume = AudioManager.Instance.bgmVolume;
            cached.settings.sfxVolume = AudioManager.Instance.sfxVolume;
        }
    }

    /// <summary>将缓存应用到当前运行态（进入 Gameplay 后调用）。</summary>
    public static void ApplyToRuntime()
    {
        EnsureLoaded();

        // Settings
        ApplySettingsToRuntime(cached.settings);

        // Equipment（先导入，后续会自动给 PlayerStats 叠加加成）
        if (EquipmentManager.Instance != null && cached.equipment != null)
            EquipmentManager.Instance.ImportSaveData(cached.equipment);

        // Player
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        var ps = playerGo != null ? playerGo.GetComponent<PlayerStats>() : null;
        if (ps != null && cached.player != null)
            ps.ImportSaveData(cached.player);

        // 导入装备后需要重新计算加成
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.ReapplyEquipmentBonusToPlayer();
    }

    private static void ApplySettingsToRuntime(SettingsSaveData s)
    {
        if (s == null) return;

        // Audio
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBGMVolume(s.bgmVolume);
            AudioManager.Instance.SetSFXVolume(s.sfxVolume);
        }

        // FPS
        int fps = (s.targetFps <= 30) ? 30 : 60;
        Application.targetFrameRate = fps;
        QualitySettings.vSyncCount = 0; // 使用 targetFrameRate 控制

        // Quality（映射到 Unity 的 QualitySettings 档位：尽量在范围内）
        int qCount = QualitySettings.names != null ? QualitySettings.names.Length : 0;
        if (qCount > 0)
        {
            int idx = Mathf.Clamp(s.qualityLevel, 0, qCount - 1);
            if (QualitySettings.GetQualityLevel() != idx)
                QualitySettings.SetQualityLevel(idx, applyExpensiveChanges: false);
        }
    }

    private static GameSaveData LoadInternal()
    {
        if (!PlayerPrefs.HasKey(SaveKey)) return null;
        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return JsonUtility.FromJson<GameSaveData>(json);
        }
        catch
        {
            return null;
        }
    }

    private static void SaveInternal(GameSaveData data)
    {
        if (data == null) return;
        try
        {
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSystem] Save failed: {e.Message}");
        }
    }
}

