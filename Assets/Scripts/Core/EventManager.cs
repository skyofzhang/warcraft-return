// 依据：策划知识库 v1.6 章节 10.2 完整事件定义表、程序基础知识库 5.6、5.9 第二层
using System;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    private Dictionary<string, Action<object>> eventDictionary = new Dictionary<string, Action<object>>();
    // 在 Instance 尚未创建时缓存监听，待 Awake 后统一挂载（注：原注释“Instance 就绪后生效”需要兑现）
    private static readonly Dictionary<string, List<Action<object>>> pendingListeners = new Dictionary<string, List<Action<object>>>();
    // 仅在“未知事件名被自动注册”时提示一次，避免刷屏
    private static readonly HashSet<string> warnedAutoRegistered = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 先注册核心事件，再回放 pending listeners，避免“事件未注册”初始化时序告警
        RegisterCoreEvents();

        // 回放在 Instance 创建前注册的监听
        foreach (var kv in pendingListeners)
        {
            string eventName = kv.Key;
            var listeners = kv.Value;
            if (listeners == null) continue;
            for (int i = 0; i < listeners.Count; i++)
                AddListenerCore(eventName, listeners[i]);
        }
        pendingListeners.Clear();
    }

    public void Initialize()
    {
        Debug.Log("=== EventManager 初始化 ===");
        RegisterCoreEvents();
    }

    /// <summary>
    /// 事件名以《游戏设计的策划知识库 v1.6》章节 10.2 完整事件定义表为准。
    /// </summary>
    private void RegisterCoreEvents()
    {
        RegisterEvent("GAME_STATE_CHANGED");
        RegisterEvent("HEALTH_CHANGED");
        RegisterEvent("MANA_CHANGED");
        RegisterEvent("MONSTER_KILLED");
        RegisterEvent("PLAYER_KILLED");
        RegisterEvent("ITEM_DROPPED");
        RegisterEvent("ITEM_PICKED_UP");
        RegisterEvent("INVENTORY_UPDATED");
        RegisterEvent("EQUIPMENT_CHANGED");
        RegisterEvent("LEVEL_UP");
        RegisterEvent("EXP_GAINED");
        RegisterEvent("GOLD_CHANGED");
        RegisterEvent("POTION_CHANGED");
        RegisterEvent("LEVEL_UNLOCKED");
        RegisterEvent("SKILL_USED");
        RegisterEvent("SKILL_COOLDOWN_FINISHED");
        RegisterEvent("DAMAGE_DEALT");
        RegisterEvent("DAMAGE_TAKEN");
        RegisterEvent("CRITICAL_HIT");
        // 战斗表现用扩展事件（不影响策划知识库事件表，但用于 VFX/反馈解耦）
        RegisterEvent("BASIC_ATTACK");
        RegisterEvent("MONSTER_ATTACK");
        RegisterEvent("WAVE_STARTED");
        RegisterEvent("WAVE_COMPLETED");
        RegisterEvent("BOSS_SPAWNED");
        RegisterEvent("BOSS_DEFEATED");
        RegisterEvent("LEVEL_COMPLETED");
        RegisterEvent("LEVEL_FAILED");
        RegisterEvent("SETTINGS_CHANGED");
        RegisterEvent("PAUSE_GAME");
        RegisterEvent("RESUME_GAME");
    }

    public void RegisterEvent(string eventName)
    {
        if (!eventDictionary.ContainsKey(eventName))
            eventDictionary[eventName] = null;
    }

    private void AddListenerCore(string eventName, Action<object> listener)
    {
        if (!eventDictionary.ContainsKey(eventName))
        {
            // 允许自动注册（避免初始化顺序导致的误报）；若是拼写错误，仅提示一次
            RegisterEvent(eventName);
            if (warnedAutoRegistered.Add(eventName))
                Debug.LogWarning($"事件未注册（已自动注册）: {eventName}");
        }
        eventDictionary[eventName] += listener;
    }

    private void RemoveListenerCore(string eventName, Action<object> listener)
    {
        if (eventDictionary.ContainsKey(eventName))
            eventDictionary[eventName] -= listener;
    }

    /// <summary>静态入口，供无 Instance 时调用（Instance 就绪后生效）。</summary>
    public static void AddListener(string eventName, Action<object> listener)
    {
        if (listener == null) return;
        if (Instance != null)
        {
            Instance.AddListenerCore(eventName, listener);
            return;
        }
        if (!pendingListeners.TryGetValue(eventName, out var list))
        {
            list = new List<Action<object>>();
            pendingListeners[eventName] = list;
        }
        list.Add(listener);
    }

    /// <summary>静态入口，供无 Instance 时调用。</summary>
    public static void RemoveListener(string eventName, Action<object> listener)
    {
        if (listener == null) return;
        if (Instance != null)
        {
            Instance.RemoveListenerCore(eventName, listener);
            return;
        }
        if (pendingListeners.TryGetValue(eventName, out var list))
            list.Remove(listener);
    }

    public static void TriggerEvent(string eventName, object data = null)
    {
        if (Instance == null) return;
        if (!Instance.eventDictionary.ContainsKey(eventName))
        {
            Instance.RegisterEvent(eventName);
            if (warnedAutoRegistered.Add(eventName))
                Debug.LogWarning($"事件未注册（已自动注册）: {eventName}");
        }
        Instance.eventDictionary[eventName]?.Invoke(data);
    }
}
