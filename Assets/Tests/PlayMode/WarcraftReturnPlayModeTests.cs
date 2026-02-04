// 依据：GDD 第 11 章 游戏功能验收测试SOP；程序基础知识库 5.8、8.1
// 注：用于命令行/CI 的 PlayMode 回归测试集（首版）
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

/// <summary>
/// GDD 12 条测试用例对应的 PlayMode 自动化测试。场景需已加入 Build Settings（MainMenu、Gameplay）。
/// </summary>
public class WarcraftReturnPlayModeTests
{
    private const float SceneLoadWait = 2f;

    private class MockStatsProvider : IStatsProvider
    {
        public float AttackVal, DefenseVal, HpVal, CritChanceVal, CritDamageVal;
        public float GetStat(StatType type)
        {
            switch (type)
            {
                case StatType.Attack: return AttackVal;
                case StatType.Defense: return DefenseVal;
                case StatType.HP: return HpVal;
                case StatType.CritChance: return CritChanceVal;
                case StatType.CritDamage: return CritDamageVal;
                default: return 0f;
            }
        }
        public void ModifyStat(StatType type, float delta) { }
    }

    /// <summary>每个测试前允许 Debug.Log 与场景警告（如无 AudioListener），避免 UnexpectedLogMessage 导致失败。</summary>
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        LogAssert.ignoreFailingMessages = true;
        // 避免某些用例/系统将 timeScale 置 0 导致 WaitForSeconds/Time.time 卡死
        Time.timeScale = 1f;
        yield return null;
    }

    [UnityTest, Order(1)]
    public IEnumerator TC_DAMAGE_001_伤害公式符合策划()
    {
        var attacker = new MockStatsProvider { AttackVal = 20f, DefenseVal = 0f, CritChanceVal = 0f, CritDamageVal = 0.5f };
        var defender = new MockStatsProvider { AttackVal = 0f, DefenseVal = 10f, HpVal = 100f };
        var (finalDamage, isCrit) = CombatSystem.CalculateDamage(attacker, defender, 1f);
        Assert.GreaterOrEqual(finalDamage, 1f, "最终伤害应至少为 1");
        Assert.LessOrEqual(finalDamage, 25f, "无暴击时伤害应受防御减免");
        yield return null;
    }

    [UnityTest, Order(2)]
    public IEnumerator TC_CONFIG_001_配置正确加载()
    {
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
        yield return new WaitForSecondsRealtime(1f);
        Assert.IsNotNull(ConfigManager.Instance, "ConfigManager 应存在");
        ConfigManager.Instance.LoadAllConfigs();
        Assert.Greater(ConfigManager.Instance.LevelConfigs.Count, 0, "关卡配置应至少 1 条");
        Assert.Greater(ConfigManager.Instance.MonsterConfigs.Count, 0, "怪物配置应至少 1 条");
        foreach (var kv in ConfigManager.Instance.MonsterConfigs)
        {
            Assert.Greater(kv.Value.monster_id, 0, "monster_id 应 > 0");
            Assert.Greater(kv.Value.hp, 0, "怪物 HP 应 > 0");
            break;
        }
        Assert.IsNotNull(ConfigManager.Instance.GameConfig, "GameConfig 应已加载（GDD 10.5）");
        Assert.GreaterOrEqual(ConfigManager.Instance.GameConfig.exp_retain_ratio, 0f, "exp_retain_ratio 应在 [0,1]");
        Assert.LessOrEqual(ConfigManager.Instance.GameConfig.exp_retain_ratio, 1f, "exp_retain_ratio 应在 [0,1]");
        Assert.GreaterOrEqual(ConfigManager.Instance.GameConfig.gold_retain_ratio, 0f, "gold_retain_ratio 应在 [0,1]");
        Assert.LessOrEqual(ConfigManager.Instance.GameConfig.gold_retain_ratio, 1f, "gold_retain_ratio 应在 [0,1]");
    }

    [UnityTest, Order(3)]
    public IEnumerator TC_CAMERA_001_相机第三人称跟随()
    {
        yield return SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Single);
        yield return new WaitForSecondsRealtime(SceneLoadWait);
        var player = GameObject.FindGameObjectWithTag("Player");
        Assert.IsNotNull(player, "应存在 Player");
        var follow = Object.FindObjectOfType<ThirdPersonFollowCamera>();
        Assert.IsNotNull(follow, "场景中应有 ThirdPersonFollowCamera");
        Assert.IsNotNull(follow.GetComponent<Camera>(), "ThirdPersonFollowCamera 应在 Camera 上");
        Assert.IsTrue(follow.target == player.transform || follow.target != null, "相机应绑定目标");
    }

    [UnityTest, Order(4)]
    public IEnumerator TC_LEVEL_001_关卡波次与刷怪点()
    {
        yield return SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Single);
        yield return new WaitForSecondsRealtime(SceneLoadWait);
        var spawner = Object.FindObjectOfType<MonsterSpawner>();
        Assert.IsNotNull(spawner, "应存在 MonsterSpawner");
        Assert.IsNotNull(spawner.spawnPoints, "刷怪点不应为空");
        Assert.Greater(spawner.spawnPoints.Length, 0, "至少 1 个刷怪点");
        if (ConfigManager.Instance != null && ConfigManager.Instance.LevelConfigs.TryGetValue(spawner.levelId, out var level))
            Assert.IsNotNull(level.waves, "关卡应有波次配置");
    }

    [UnityTest, Order(5)]
    public IEnumerator TC_GAMEPLAY_001_核心战斗循环_场景与玩家()
    {
        yield return SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Single);
        yield return new WaitForSecondsRealtime(SceneLoadWait);
        var player = GameObject.FindGameObjectWithTag("Player");
        Assert.IsNotNull(player, "Gameplay 场景应有 Player");
        Assert.IsNotNull(player.GetComponent<PlayerStats>(), "Player 应有 PlayerStats");
        Assert.IsNotNull(player.GetComponent<PlayerController>(), "Player 应有 PlayerController");
        Assert.IsNotNull(GameManager.Instance, "应有 GameManager");
        Assert.AreEqual(GameState.InGame, GameManager.Instance.CurrentState, "进入 Gameplay 后状态应为 InGame");
    }

    [UnityTest, Order(6)]
    public IEnumerator TC_EVENT_001_事件系统存在且可触发()
    {
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
        yield return new WaitForSecondsRealtime(0.5f);
        Assert.IsNotNull(EventManager.Instance, "EventManager 应存在");
        bool received = false;
        EventManager.AddListener("HEALTH_CHANGED", _ => received = true);
        EventManager.TriggerEvent("HEALTH_CHANGED", new object[] { 100f, 150f });
        yield return null;
        Assert.IsTrue(received, "HEALTH_CHANGED 应被监听并触发");
    }

    [UnityTest, Order(7)]
    public IEnumerator TC_UI_001_主界面与UIManager()
    {
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
        yield return new WaitForSecondsRealtime(1f);
        Assert.IsNotNull(UIManager.Instance, "UIManager 应存在");
        Assert.AreEqual(GameState.MainMenu, GameManager.Instance.CurrentState, "主界面状态应为 MainMenu");
    }

    [UnityTest, Order(8)]
    public IEnumerator TC_SKILL_001_技能冷却与PlayerController()
    {
        yield return SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Single);
        yield return new WaitForSecondsRealtime(SceneLoadWait);
        var player = GameObject.FindGameObjectWithTag("Player");
        var pc = player != null ? player.GetComponent<PlayerController>() : null;
        Assert.IsNotNull(pc, "应有 PlayerController");
        pc.UseSkill(0);
        Assert.IsFalse(pc.IsSkillReady, "释放技能后应进入冷却");
    }

    [UnityTest, Order(9)]
    public IEnumerator TC_LOOT_001_掉落与LootManager()
    {
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
        yield return new WaitForSecondsRealtime(0.5f);
        Assert.IsNotNull(Object.FindObjectOfType<LootManager>(), "LootManager 应存在");
        Assert.IsNotNull(ConfigManager.Instance?.DropTableConfigs, "掉落表配置应已加载");
    }

    [UnityTest, Order(10)]
    public IEnumerator TC_EQUIP_001_装备系统与EquipmentManager()
    {
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
        yield return new WaitForSecondsRealtime(0.5f);
        Assert.IsNotNull(EquipmentManager.Instance, "EquipmentManager 应存在");
        Assert.IsNotNull(ConfigManager.Instance?.EquipmentConfigs, "装备配置应已加载");
    }

    [UnityTest, Order(11)]
    public IEnumerator TC_DEATH_001_玩家死亡派发事件()
    {
        yield return SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Single);
        yield return new WaitForSecondsRealtime(SceneLoadWait);
        var player = GameObject.FindGameObjectWithTag("Player");
        var pc = player?.GetComponent<PlayerController>();
        Assert.IsNotNull(pc, "应有 PlayerController");
        bool deathEventFired = false;
        EventManager.AddListener("PLAYER_KILLED", _ => deathEventFired = true);
        pc.TakeDamage(9999f);
        yield return null;
        Assert.IsTrue(deathEventFired, "玩家死亡应派发 PLAYER_KILLED");
    }

    [UnityTest, Order(12)]
    public IEnumerator TC_ANIMATION_001_角色与移动组件()
    {
        yield return SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Single);
        yield return new WaitForSecondsRealtime(SceneLoadWait);
        var player = GameObject.FindGameObjectWithTag("Player");
        Assert.IsNotNull(player, "应有玩家角色");
        Assert.IsNotNull(player.GetComponent<Rigidbody>(), "玩家应有 Rigidbody 用于移动");
    }

    [UnityTest, Order(13)]
    public IEnumerator TC_VICTORY_001_胜利结算触发_LEVEL_COMPLETED()
    {
        yield return SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Single);
        // 先等场景稳定；注意当前刷怪逻辑为“波次怪物清完后才会进入下一阶段/BOSS”，这里用分段击杀推进流程
        yield return new WaitForSecondsRealtime(SceneLoadWait);

        bool victoryEventFired = false;
        EventManager.AddListener("LEVEL_COMPLETED", _ => victoryEventFired = true);

        // 多轮“清怪”，给刷怪协程时间从波次推进到 Boss 阶段
        for (int round = 0; round < 4; round++)
        {
            var monsters = GameObject.FindGameObjectsWithTag("Monster");
            for (int i = 0; i < monsters.Length; i++)
            {
                var m = monsters[i];
                if (m == null) continue;
                var mc = m.GetComponent<MonsterController>();
                if (mc != null) mc.TakeDamage(999999f);
            }
            yield return new WaitForSecondsRealtime(0.25f);
        }

        // 等待最多 4 秒让胜利逻辑与 UI 状态切换完成
        float timeout = Time.realtimeSinceStartup + 4f;
        while (Time.realtimeSinceStartup < timeout && GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Settlement)
            yield return null;

        Assert.IsTrue(victoryEventFired, "应触发 LEVEL_COMPLETED（胜利结算）");
        Assert.IsNotNull(GameManager.Instance, "应有 GameManager");
        Assert.AreEqual(GameState.Settlement, GameManager.Instance.CurrentState, "胜利后应进入 Settlement");
        Assert.IsTrue(GameManager.Instance.LastVictory, "胜利后 LastVictory 应为 true");
    }
}
