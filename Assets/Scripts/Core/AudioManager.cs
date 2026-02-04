// 依据：程序基础知识库 5.2、5.9 第五层；GDD 4.2 首版最小音效清单
using UnityEngine;

/// <summary>
/// 背景音乐与音效播放。接口与 GDD 4.2 首版最小音效清单对应；资源就绪后从 Resources/Audio 加载，可占位。
/// </summary>
public class AudioManager : MonoBehaviour
{
    /// <summary>GDD 4.2 首版最小音效清单 — Resources 下路径（无扩展名），可占位。</summary>
    public static class SoundId
    {
        public const string BGM_MainMenu = "Audio/Music/MainMenu";
        public const string BGM_Gameplay = "Audio/Music/Gameplay";
        public const string SFX_ButtonClick = "Audio/SFX/ButtonClick";
        public const string SFX_Attack = "Audio/SFX/Attack";
        public const string SFX_Hit = "Audio/SFX/Hit";
        public const string SFX_Death_Monster = "Audio/SFX/Death_Monster";
        public const string SFX_Death_Player = "Audio/SFX/Death_Player";
        public const string SFX_Skill = "Audio/SFX/Skill";
        public const string SFX_LevelUp = "Audio/SFX/LevelUp";
        public const string SFX_GoldPickup = "Audio/SFX/GoldPickup";
        public const string SFX_Pickup = "Audio/SFX/Pickup";
        public const string SFX_UI_Error = "Audio/SFX/UI_Error";
    }

    public static AudioManager Instance { get; private set; }

    [Header("BGM 音量")]
    [Range(0f, 1f)] public float bgmVolume = 0.7f;
    [Header("SFX 音量")]
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private AudioSource bgmSource;
    private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
    }

    /// <summary>播放背景音乐。path 为 Resources 下路径，如 "Audio/Music/MainMenu"。</summary>
    public void PlayBGM(string path)
    {
        var clip = Resources.Load<AudioClip>(path);
        if (clip != null)
        {
            bgmSource.clip = clip;
            bgmSource.volume = bgmVolume;
            bgmSource.Play();
        }
    }

    /// <summary>停止 BGM。</summary>
    public void StopBGM()
    {
        bgmSource.Stop();
    }

    /// <summary>播放音效。path 为 Resources 下路径，如 "Audio/SFX/Click"。</summary>
    public void PlaySFX(string path)
    {
        var clip = Resources.Load<AudioClip>(path);
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }

    public void SetBGMVolume(float v) { bgmVolume = Mathf.Clamp01(v); if (bgmSource != null) bgmSource.volume = bgmVolume; }
    public void SetSFXVolume(float v) { sfxVolume = Mathf.Clamp01(v); }

    /// <summary>主界面 BGM（GDD 4.2 进入主界面）。</summary>
    public void PlayBGM_MainMenu() { PlayBGM(SoundId.BGM_MainMenu); }
    /// <summary>战斗场景 BGM（GDD 4.2 进入 Gameplay）。</summary>
    public void PlayBGM_Gameplay() { PlayBGM(SoundId.BGM_Gameplay); }
    /// <summary>按钮点击（GDD 4.2 任意可点击按钮按下）。</summary>
    public void PlaySFX_ButtonClick() { PlaySFX(SoundId.SFX_ButtonClick); }
    /// <summary>普攻命中（GDD 4.2 玩家普攻命中怪物）。</summary>
    public void PlaySFX_Attack() { PlaySFX(SoundId.SFX_Attack); }
    /// <summary>受击（GDD 4.2 玩家或怪物受击）。</summary>
    public void PlaySFX_Hit() { PlaySFX(SoundId.SFX_Hit); }
    /// <summary>怪物死亡（GDD 4.2 怪物死亡）。</summary>
    public void PlaySFX_Death_Monster() { PlaySFX(SoundId.SFX_Death_Monster); }
    /// <summary>玩家死亡（GDD 4.2 玩家死亡）。</summary>
    public void PlaySFX_Death_Player() { PlaySFX(SoundId.SFX_Death_Player); }
    /// <summary>技能释放（GDD 4.2 技能释放）。</summary>
    public void PlaySFX_Skill() { PlaySFX(SoundId.SFX_Skill); }
    /// <summary>升级（GDD 4.2 角色升级）。</summary>
    public void PlaySFX_LevelUp() { PlaySFX(SoundId.SFX_LevelUp); }
    /// <summary>拾取金币（GDD 4.2 拾取金币）。</summary>
    public void PlaySFX_GoldPickup() { PlaySFX(SoundId.SFX_GoldPickup); }
    /// <summary>拾取物品（交付文档/音效最小规范与清单_v3.2）。</summary>
    public void PlaySFX_Pickup() { PlaySFX(SoundId.SFX_Pickup); }
    /// <summary>UI 不可用/错误提示（如治疗瓶数量=0）。</summary>
    public void PlaySFX_UI_Error() { PlaySFX(SoundId.SFX_UI_Error); }
}
