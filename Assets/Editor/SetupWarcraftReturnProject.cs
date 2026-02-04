// 一键完成：项目设置、Layers/Tags、Managers、MainMenu/Gameplay 场景、Build Settings
// 在 Unity 菜单点击：WarcraftReturn -> 一键配置工程与场景
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SetupWarcraftReturnProject
{
    private const string MenuName = "WarcraftReturn/一键配置工程与场景";

    [MenuItem(MenuName)]
    public static void Execute()
    {
        ApplyPlayerSettings();
        ApplyLayersAndTags();
        CreateMainMenuScene();
        CreateGameplayScene();
        AddScenesToBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[WarcraftReturn] 一键配置完成：Player 竖屏、Layers/Tags、MainMenu 与 Gameplay 场景、Build Settings 已就绪。");
    }

    private static void ApplyPlayerSettings()
    {
        PlayerSettings.companyName = "ManusStudio";
        PlayerSettings.productName = "我叫MT之魔兽归来";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
#if UNITY_2022_1_OR_NEWER
        PlayerSettings.defaultScreenWidth = 1080;
        PlayerSettings.defaultScreenHeight = 1920;
#endif

        // 需求/程序知识库：移动端脚本后端统一 IL2CPP（避免不同机器默认值不一致）
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
    }

    private static void ApplyLayersAndTags()
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        string[] needLayers = { "Player", "Monster", "Projectile", "Ground" };
        int idx = 6;
        for (int i = 0; i < needLayers.Length && idx < 31; i++)
        {
            while (idx < 31 && layers.GetArrayElementAtIndex(idx).stringValue != "" && layers.GetArrayElementAtIndex(idx).stringValue != needLayers[i])
                idx++;
            if (idx < 31)
            {
                layers.GetArrayElementAtIndex(idx).stringValue = needLayers[i];
                idx++;
            }
        }
        SerializedProperty tags = tagManager.FindProperty("tags");
        bool hasMonster = false;
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == "Monster") { hasMonster = true; break; }
        if (!hasMonster)
        {
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = "Monster";
        }
        tagManager.ApplyModifiedProperties();
    }

    private static void CreateMainMenuScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject managers = CreateManagers();
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
    }

    private static GameObject CreateManagers()
    {
        GameObject go = new GameObject("Managers");
        go.AddComponent<GameManager>();
        go.AddComponent<EventManager>();
        go.AddComponent<ConfigManager>();
        go.AddComponent<LootManager>();
        go.AddComponent<EquipmentManager>();
        go.AddComponent<UIManager>();
        go.AddComponent<AudioManager>();
        return go;
    }

    private static void CreateGameplayScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject managers = CreateManagers();
        managers.AddComponent<SanityCheck>();

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(2f, 1f, 2f);
        if (ground.GetComponent<Collider>() != null) ground.GetComponent<Collider>().enabled = true;

        GameObject player = CreatePlayerPlaceholder();
        GameObject monster = CreateMonsterPlaceholder();

        int playerLayer = LayerMask.NameToLayer("Player");
        int monsterLayer = LayerMask.NameToLayer("Monster");
        if (playerLayer >= 0) player.layer = playerLayer;
        if (monsterLayer >= 0) monster.layer = monsterLayer;

        GameObject spawner = new GameObject("Spawner");
        var spawnerComp = spawner.AddComponent<MonsterSpawner>();
        spawnerComp.levelId = 1;
        Transform sp1 = new GameObject("SP_01").transform;
        sp1.SetParent(spawner.transform);
        sp1.position = new Vector3(5f, 0f, 0f);
        Transform sp2 = new GameObject("SP_02").transform;
        sp2.SetParent(spawner.transform);
        sp2.position = new Vector3(-4f, 0f, 3f);
        spawnerComp.spawnPoints = new[] { sp1, sp2 };

        Camera cam = Object.FindObjectOfType<Camera>();
        if (cam == null) cam = new GameObject("Main Camera").AddComponent<Camera>();
        cam.gameObject.AddComponent<AudioListener>();
        var follow = cam.gameObject.AddComponent<ThirdPersonFollowCamera>();
        follow.target = player.transform;
        follow.distance = 8f;
        follow.height = 3f;

        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Gameplay.unity");
    }

    private static GameObject CreatePlayerPlaceholder()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Player_Placeholder";
        go.tag = "Player";
        go.transform.position = new Vector3(0f, 1f, 0f);
        go.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null && renderer.sharedMaterial != null)
            renderer.sharedMaterial.color = Color.green;
        var rb = go.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.useGravity = true;
        go.AddComponent<PlayerStats>();
        go.AddComponent<PlayerController>();
        return go;
    }

    private static GameObject CreateMonsterPlaceholder()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Monster_Murloc_Placeholder";
        go.tag = "Monster";
        go.transform.position = new Vector3(3f, 1f, 0f);
        go.transform.localScale = new Vector3(0.4f, 0.8f, 0.4f);
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null && renderer.sharedMaterial != null)
            renderer.sharedMaterial.color = new Color(0f, 0.53f, 1f);
        var rb = go.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.useGravity = true;
        var ms = go.AddComponent<MonsterStats>();
        ms.monsterId = 101;
        go.AddComponent<MonsterController>();
        return go;
    }

    private static void AddScenesToBuildSettings()
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        string[] paths = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Gameplay.unity" };
        foreach (string path in paths)
        {
            if (System.IO.File.Exists(path) && list.All(s => s.path != path))
                list.Add(new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = list.ToArray();
    }
}
#endif
