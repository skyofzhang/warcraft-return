// 依据：程序基础知识库 5.2、5.4、5.8、5.9 第五层；AI程序工作指南 2.1
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }
    private Action<object> onPlayerKilledHandler;
    /// <summary>当前关卡 ID，用于结算后重试。</summary>
    public int CurrentLevelId { get; private set; }
    /// <summary>最近一局是否胜利，供结算界面显示。</summary>
    public bool LastVictory { get; private set; }
    /// <summary>最近一局奖励金币（胜利=关卡奖励；失败=保留金币）。</summary>
    public int LastRewardGold { get; private set; }
    /// <summary>最近一局奖励经验（胜利=关卡奖励；失败=保留经验）。</summary>
    public int LastRewardExp { get; private set; }

    /// <summary>本局拾取到的装备掉落（用于结算 UI 展示）。</summary>
    private readonly System.Collections.Generic.Dictionary<int, int> sessionLootEquip = new System.Collections.Generic.Dictionary<int, int>();
    /// <summary>最近一局的装备掉落快照（equipment_id -> count）。</summary>
    private readonly System.Collections.Generic.Dictionary<int, int> lastLootEquip = new System.Collections.Generic.Dictionary<int, int>();

    private Action<object> onItemPickedUpHandler;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 进入 PlayMode 时，初始场景不会触发 sceneLoaded 回调，这里先做一次“单 Camera/单 AudioListener”修复，避免 Gameplay 场景刷屏告警
        ApplyCameraAndAudioPolicyForCurrentScene();
        onPlayerKilledHandler = OnPlayerKilled;
        EventManager.AddListener("PLAYER_KILLED", onPlayerKilledHandler);

        onItemPickedUpHandler = OnItemPickedUp;
        EventManager.AddListener("ITEM_PICKED_UP", onItemPickedUpHandler);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (onPlayerKilledHandler != null)
            EventManager.RemoveListener("PLAYER_KILLED", onPlayerKilledHandler);
        if (onItemPickedUpHandler != null)
            EventManager.RemoveListener("ITEM_PICKED_UP", onItemPickedUpHandler);
    }

    /// <summary>给结算界面使用的掉落快照（只读）。</summary>
    public System.Collections.Generic.IReadOnlyDictionary<int, int> GetLastLootEquipSnapshot() => lastLootEquip;

    private void ResetSessionLoot()
    {
        sessionLootEquip.Clear();
        lastLootEquip.Clear();
    }

    private void OnItemPickedUp(object data)
    {
        // data: { itemType(string), itemId(int), count(int), position(Vector3) }
        if (!(data is object[] arr) || arr.Length < 3) return;
        if (!(arr[0] is string type)) return;
        if (!(arr[1] is int itemId)) return;
        if (!(arr[2] is int count)) return;
        if (count <= 0 || itemId <= 0) return;

        // 只记录“战斗进行中”的拾取（避免主菜单/其他流程误计入）
        if (CurrentState != GameState.InGame && CurrentState != GameState.Paused) return;

        if (type == "equipment")
        {
            if (!sessionLootEquip.ContainsKey(itemId)) sessionLootEquip[itemId] = 0;
            sessionLootEquip[itemId] += count;
        }
    }

    /// <summary>玩家死亡时派发 PLAYER_KILLED，GameManager 派发 LEVEL_FAILED 并进入 Settlement（开发计划 3.4）。</summary>
    private void OnPlayerKilled(object _)
    {
        EndGame(false);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 兼容 Editor PlayMode 的 Temp/__Backupscenes/*.backup 场景名；避免双 Camera/AudioListener 警告刷屏
        ApplyCameraAndAudioPolicyForCurrentScene();
        // URP 兼容兜底：把 Standard/缺 shader 等不兼容材质改成 URP/Lit，避免画面发粉
        FixUnsupportedMaterialsForCurrentRenderPipeline();

        bool isGameplay = IsLoadedScene(scene, "Gameplay");
        bool isMainMenu = IsLoadedScene(scene, "MainMenu");

        // 未知场景：默认当作主菜单（避免因残留对象误判为 InGame，导致 TC_UI_001 等测试失败）
        if (isGameplay)
        {
            ChangeState(GameState.InGame);
            SaveSystem.ApplyToRuntime();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayBGM_Gameplay();
            EnsureGameplayCamera();
            EnsureWangZheCanyonMap();
        }
        else
        {
            ChangeState(GameState.MainMenu);
            SaveSystem.ApplyToRuntime();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayBGM_MainMenu();
        }
    }

    /// <summary>
    /// Gameplay 场景兜底：生成一张“王者峡谷风格”的可玩地图（占位版）。
    /// </summary>
    private static void EnsureWangZheCanyonMap()
    {
        if (GameObject.Find(WangZheCanyonMapBuilder.RootName) != null) return;
        var go = new GameObject("MapBuilder_WangZheCanyon");
        var builder = go.AddComponent<WangZheCanyonMapBuilder>();
        builder.BuildIfNeeded();
    }

    private static bool IsLoadedScene(Scene scene, string sceneName)
    {
        if (scene.name == sceneName) return true;

        string path = scene.path;
        if (!string.IsNullOrEmpty(path))
        {
            path = path.Replace("\\", "/");
            if (path.EndsWith("/" + sceneName + ".unity")) return true;
        }

        // Temp/__Backupscenes/*.backup 兼容：buildIndex 通常仍能映射回 Build Settings 的原场景路径
        if (scene.buildIndex >= 0)
        {
            string buildPath = SceneUtility.GetScenePathByBuildIndex(scene.buildIndex);
            if (!string.IsNullOrEmpty(buildPath))
            {
                buildPath = buildPath.Replace("\\", "/");
                if (buildPath.EndsWith("/" + sceneName + ".unity")) return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 测试与场景兜底：确保 Gameplay 场景里一定存在 ThirdPersonFollowCamera（TC_CAMERA_001）。
    /// </summary>
    private static void EnsureGameplayCamera()
    {
        var follow = FindObjectOfType<ThirdPersonFollowCamera>();
        if (follow != null) return;

        var player = GameObject.FindGameObjectWithTag("Player");

        Camera cam = Camera.main;
        if (cam == null)
        {
            var cams = FindObjectsOfType<Camera>();
            if (cams != null && cams.Length > 0) cam = cams[0];
        }
        if (cam == null)
            cam = new GameObject("Main Camera").AddComponent<Camera>();

        if (cam.GetComponent<AudioListener>() == null)
            cam.gameObject.AddComponent<AudioListener>();

        follow = cam.gameObject.AddComponent<ThirdPersonFollowCamera>();
        if (player != null) follow.target = player.transform;
    }

    /// <summary>
    /// 保证当前激活场景里始终只有 1 个启用 Camera 与 1 个启用 AudioListener。
    /// 说明：Gameplay 场景中既有 Main Camera（第三人称相机），也有 Managers 上的 Camera/Listener（用于主菜单兜底），需要在运行时关闭其一。
    /// </summary>
    private void ApplyCameraAndAudioPolicyForCurrentScene()
    {
        var myCam = GetComponent<Camera>();
        var myListener = GetComponent<AudioListener>();

        // 先尝试保留“第三人称跟随相机”的 Camera（优先 Gameplay）
        Camera keepCam = null;
        var follow = FindObjectOfType<ThirdPersonFollowCamera>();
        if (follow != null)
            keepCam = follow.GetComponent<Camera>();
        if (keepCam == null)
            keepCam = myCam;

        var cams = FindObjectsOfType<Camera>();
        for (int i = 0; i < cams.Length; i++)
        {
            var c = cams[i];
            if (c == null) continue;
            if (keepCam != null) c.enabled = (c == keepCam);
        }
        // 兜底：如果 keepCam 不存在（极端），至少启用一个 Camera
        if (keepCam == null && cams.Length > 0)
        {
            cams[0].enabled = true;
            for (int i = 1; i < cams.Length; i++) cams[i].enabled = false;
            keepCam = cams[0];
        }
        // 极端兜底：场景里完全没有 Camera（以及可能没有 AudioListener），创建一个
        if (keepCam == null)
        {
            keepCam = new GameObject("Main Camera").AddComponent<Camera>();
            keepCam.enabled = true;
        }

        // AudioListener：优先挂在 keepCam 上；否则保留 myListener；否则保留第一个启用的
        AudioListener keepListener = null;
        // 关键兜底：如果 keepCam 上没有 AudioListener，则直接补一个，避免 Unity 警告刷屏
        if (keepCam != null)
        {
            keepListener = keepCam.GetComponent<AudioListener>();
            if (keepListener == null) keepListener = keepCam.gameObject.AddComponent<AudioListener>();
        }
        if (keepListener == null) keepListener = myListener;

        var listeners = FindObjectsOfType<AudioListener>();
        for (int i = 0; i < listeners.Length; i++)
        {
            var l = listeners[i];
            if (l == null) continue;
            if (keepListener != null) l.enabled = (l == keepListener);
        }

        // 兜底：若 keepListener 为空（极端场景），至少启用一个
        if (keepListener == null && listeners.Length > 0)
        {
            listeners[0].enabled = true;
            for (int i = 1; i < listeners.Length; i++) listeners[i].enabled = false;
        }
    }

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        Debug.Log("=== GameManager 初始化 ===");
        if (EventManager.Instance != null) EventManager.Instance.Initialize();
        if (ConfigManager.Instance != null) ConfigManager.Instance.LoadAllConfigs();
        SaveSystem.EnsureLoaded();
        SaveSystem.ApplyToRuntime();
        ValidateArtAssets();
        // 初始场景（MainMenu 直接 Play）也需要一次材质兜底
        FixUnsupportedMaterialsForCurrentRenderPipeline();
        ChangeState(GameState.MainMenu);
    }

    private static Shader s_urpLitShader;
    private static Material s_urpFallbackMat;
    private static readonly System.Collections.Generic.Dictionary<int, Material> s_convertedMatCache =
        new System.Collections.Generic.Dictionary<int, Material>(64);

    private static bool IsURPActive()
    {
        var rp = GraphicsSettings.currentRenderPipeline;
        if (rp == null) return false;
        // 避免直接引用 URP 类型，降低编译耦合
        return rp.GetType().Name.IndexOf("UniversalRenderPipelineAsset", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static Shader GetUrpLitShader()
    {
        if (s_urpLitShader != null) return s_urpLitShader;
        s_urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
        if (s_urpLitShader == null) s_urpLitShader = Shader.Find("Universal Render Pipeline/Simple Lit");
        return s_urpLitShader;
    }

    private static Material GetUrpFallbackMaterial()
    {
        if (s_urpFallbackMat != null) return s_urpFallbackMat;
        var sh = GetUrpLitShader();
        if (sh == null) sh = Shader.Find("Unlit/Color");
        if (sh == null) sh = Shader.Find("Standard");
        if (sh == null) return null;
        s_urpFallbackMat = new Material(sh);
        s_urpFallbackMat.name = "_RuntimeFallbackMat_URP";
        if (s_urpFallbackMat.HasProperty("_BaseColor")) s_urpFallbackMat.SetColor("_BaseColor", new Color(0.25f, 0.25f, 0.25f, 1f));
        else if (s_urpFallbackMat.HasProperty("_Color")) s_urpFallbackMat.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        return s_urpFallbackMat;
    }

    private static void FixUnsupportedMaterialsForCurrentRenderPipeline()
    {
        // 只在 URP 激活时做“Standard -> URP”的转换；否则不动（避免内置管线项目被误改）
        if (!IsURPActive()) return;

        var renderers = FindObjectsOfType<Renderer>(true);
        if (renderers == null || renderers.Length == 0) return;

        var lit = GetUrpLitShader();
        var fallback = GetUrpFallbackMaterial();
        if (lit == null && fallback == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;
            var mats = r.sharedMaterials;
            if (mats == null || mats.Length == 0) continue;

            bool changed = false;
            for (int m = 0; m < mats.Length; m++)
            {
                var mat = mats[m];
                if (mat == null)
                {
                    if (fallback != null) { mats[m] = fallback; changed = true; }
                    continue;
                }

                var sh = mat.shader;
                var shName = sh != null ? sh.name : null;
                bool isBroken = (sh == null) || (!string.IsNullOrEmpty(shName) && shName.IndexOf("InternalErrorShader", StringComparison.OrdinalIgnoreCase) >= 0);
                if (isBroken)
                {
                    if (fallback != null) { mats[m] = fallback; changed = true; }
                    continue;
                }

                // URP 下常见“发粉”来源：Standard/Legacy Shaders
                bool isBuiltin = string.Equals(shName, "Standard", StringComparison.OrdinalIgnoreCase) ||
                                 (!string.IsNullOrEmpty(shName) && shName.StartsWith("Legacy Shaders/", StringComparison.OrdinalIgnoreCase));
                if (!isBuiltin) continue;

                int key = mat.GetInstanceID();
                if (!s_convertedMatCache.TryGetValue(key, out var converted) || converted == null)
                {
                    if (lit == null)
                    {
                        converted = fallback;
                    }
                    else
                    {
                        converted = new Material(lit);
                        converted.name = mat.name + "_URP";

                        // 尽量保留主贴图与颜色（Standard: _MainTex/_Color；URP: _BaseMap/_BaseColor）
                        Color c = Color.white;
                        if (mat.HasProperty("_Color")) c = mat.GetColor("_Color");
                        else if (mat.HasProperty("_BaseColor")) c = mat.GetColor("_BaseColor");

                        Texture t = null;
                        if (mat.HasProperty("_MainTex")) t = mat.GetTexture("_MainTex");
                        else if (mat.HasProperty("_BaseMap")) t = mat.GetTexture("_BaseMap");

                        if (converted.HasProperty("_BaseColor")) converted.SetColor("_BaseColor", c);
                        else if (converted.HasProperty("_Color")) converted.SetColor("_Color", c);

                        if (t != null)
                        {
                            if (converted.HasProperty("_BaseMap")) converted.SetTexture("_BaseMap", t);
                            else if (converted.HasProperty("_MainTex")) converted.SetTexture("_MainTex", t);
                        }
                    }

                    s_convertedMatCache[key] = converted;
                }

                if (converted != null && mats[m] != converted)
                {
                    mats[m] = converted;
                    changed = true;
                }
            }

            if (changed)
                r.sharedMaterials = mats;
        }
    }

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;
        Debug.Log($"游戏状态切换: {CurrentState} -> {newState}");
        CurrentState = newState;
        EventManager.TriggerEvent("GAME_STATE_CHANGED", newState);
    }

    public void StartGame(int levelId)
    {
        // 防御式兜底：可能从暂停/结算进入游戏，确保 timeScale 恢复
        Time.timeScale = 1f;
        ResetSessionLoot();
        SaveSystem.EnsureLoaded();
        var save = SaveSystem.GetCached();
        int unlocked = save?.player != null ? Mathf.Max(1, save.player.unlocked_level_id) : 1;
        levelId = Mathf.Clamp(levelId, 1, unlocked);

        // 若配置缺失，回退到 1
        if (ConfigManager.Instance != null && ConfigManager.Instance.LevelConfigs != null &&
            !ConfigManager.Instance.LevelConfigs.ContainsKey(levelId))
            levelId = 1;

        Debug.Log($"开始游戏，关卡ID: {levelId}");
        CurrentLevelId = levelId;
        SaveSystem.CaptureFromRuntime();
        SaveSystem.SaveNow();
        ChangeState(GameState.Loading);
        // 场景切换仅通过 GameManager，禁止在普通脚本中直接 LoadScene
        SceneManager.LoadScene("Gameplay");
        ChangeState(GameState.InGame);
    }

    /// <summary>返回主菜单（仅 GameManager 可调场景）。</summary>
    public void LoadMainMenu()
    {
        // 防御式兜底：从结算/暂停返回主菜单时恢复 timeScale
        Time.timeScale = 1f;
        ResetSessionLoot();
        SaveSystem.CaptureFromRuntime();
        SaveSystem.SaveNow();
        ChangeState(GameState.MainMenu);
        SceneManager.LoadScene("MainMenu");
    }

    /// <summary>结算后重试当前关卡。</summary>
    public void RetryLevel()
    {
        StartGame(CurrentLevelId);
    }

    public void PauseGame()
    {
        Time.timeScale = 0f;
        ChangeState(GameState.Paused);
        EventManager.TriggerEvent("PAUSE_GAME", null);
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        ChangeState(GameState.InGame);
        EventManager.TriggerEvent("RESUME_GAME", null);
    }

    public void EndGame(bool victory)
    {
        Debug.Log($"游戏结束，胜利: {victory}");
        // 结算界面期间冻结战斗世界（避免“结算时血条还在变/怪物还在打”）
        Time.timeScale = 0f;
        LastVictory = victory;
        LastRewardGold = 0;
        LastRewardExp = 0;

        // 固化本局装备掉落（仅胜利在 UI-05 展示）
        lastLootEquip.Clear();
        if (victory)
        {
            foreach (var kv in sessionLootEquip)
            {
                if (kv.Key <= 0 || kv.Value <= 0) continue;
                lastLootEquip[kv.Key] = kv.Value;
            }
        }

        // 胜利：结算关卡奖励 + 解锁下一关（最多 10）
        if (victory)
        {
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            var ps = playerGo != null ? playerGo.GetComponent<PlayerStats>() : null;

            int rewardGold = 0;
            int rewardExp = 0;
            if (ConfigManager.Instance != null && ConfigManager.Instance.LevelConfigs != null &&
                ConfigManager.Instance.LevelConfigs.TryGetValue(CurrentLevelId, out var cfg) && cfg != null)
            {
                rewardGold = Mathf.Max(0, cfg.reward_gold);
                rewardExp = Mathf.Max(0, cfg.reward_exp);
            }
            else
            {
                // 兜底：随关卡递增
                rewardGold = 30 + CurrentLevelId * 10;
                rewardExp = 20 + CurrentLevelId * 8;
            }

            if (ps != null)
            {
                if (rewardGold > 0) ps.AddGold(rewardGold);
                if (rewardExp > 0) ps.AddExp(rewardExp);
                ps.UnlockLevel(CurrentLevelId + 1);
            }
            else
            {
                SaveSystem.EnsureLoaded();
                var save = SaveSystem.GetCached();
                if (save?.player != null)
                {
                    save.player.gold = Mathf.Max(0, save.player.gold + rewardGold);
                    save.player.exp = Mathf.Max(0, save.player.exp + rewardExp);
                    save.player.unlocked_level_id = Mathf.Max(save.player.unlocked_level_id, Mathf.Clamp(CurrentLevelId + 1, 1, 10));
                    SaveSystem.SaveNow();
                    EventManager.TriggerEvent("GOLD_CHANGED", new object[] { save.player.gold, rewardGold });
                    EventManager.TriggerEvent("LEVEL_UNLOCKED", save.player.unlocked_level_id);
                }
            }

            LastRewardGold = rewardGold;
            LastRewardExp = rewardExp;
        }
        else
        {
            // 失败：按配置保留比例回收本局收益（用于结算界面展示）
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            var ps = playerGo != null ? playerGo.GetComponent<PlayerStats>() : null;
            if (ps != null && ConfigManager.Instance?.GameConfig != null)
            {
                var (retainedGold, retainedExp) = ps.ApplyFailureRetain();
                LastRewardGold = retainedGold;
                LastRewardExp = retainedExp;
            }
        }

        ChangeState(GameState.Settlement);
        EventManager.TriggerEvent(victory ? "LEVEL_COMPLETED" : "LEVEL_FAILED", new object[] { LastRewardGold, LastRewardExp, CurrentLevelId });
        // 结算时立即落盘
        SaveSystem.CaptureFromRuntime();
        SaveSystem.SaveNow();
    }

    private void OnApplicationPause(bool pause)
    {
        if (!pause) return;
        SaveSystem.CaptureFromRuntime();
        SaveSystem.SaveNow();
    }

    private void OnApplicationQuit()
    {
        SaveSystem.CaptureFromRuntime();
        SaveSystem.SaveNow();
    }

    private void ValidateArtAssets()
    {
        // batchmode/CI 下不刷“占位资源缺失”的日志，避免影响测试产物可读性
        if (Application.isBatchMode) return;

        Debug.Log("=== 验证美术资源加载（3D + UI）===");
        var playerModel = Resources.Load<GameObject>("Models/Player/Player_Elf");
        if (playerModel == null) Debug.Log("玩家角色模型未找到（将使用占位符/动态生成）");
        var murlocPrefab = Resources.Load<GameObject>("Prefabs/Monsters/Murloc");
        if (murlocPrefab == null) Debug.Log("鱼人怪物 Prefab 未找到（使用占位符/动态生成）");
        Debug.Log("=== 美术资源验证完成 ===");
    }
}
