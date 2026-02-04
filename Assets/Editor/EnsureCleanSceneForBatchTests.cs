#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 解决 batchmode 运行测试时触发 “Scene(s) Have Been Modified” 的保存弹窗（batchmode 不允许弹窗）：
/// 在检测到 -runTests/-runEditorTests 时，强制切到一个已保存的场景，避免出现 Untitled 脏场景。
/// </summary>
[InitializeOnLoad]
public static class EnsureCleanSceneForBatchTests
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

    static EnsureCleanSceneForBatchTests()
    {
        if (!ShouldRunTestsFromCommandLine()) return;

        // 重要：Unity 在启动过程中会经历多次域重载/延迟回调，可能把场景又切回 Untitled。
        // 因此这里同时挂多个时机，尽量保证在 TestRunner 的 SaveModiedSceneTask 之前把场景切到“已保存场景”。
        AssemblyReloadEvents.afterAssemblyReload += EnsureLoop;
        EditorApplication.delayCall += EnsureLoop;
    }

    private static bool ShouldRunTestsFromCommandLine()
    {
        try
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                var a = args[i];
                if (string.Equals(a, "-runTests", StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(a, "-runEditorTests", StringComparison.OrdinalIgnoreCase)) return true;
            }
        }
        catch
        {
            // ignore
        }
        return false;
    }

    private static void EnsureLoop()
    {
        if (EditorApplication.isCompiling) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Ensure();

        // 关键：delayCall 在 update 之前执行（Internal_CallDelayFunctions），比 TestStarter.UpdateWatch 更早。
        // 这里自我续订，持续把场景维持在“已保存且不脏”的状态，直到测试接管。
        EditorApplication.delayCall += EnsureLoop;
    }

    private static void Ensure()
    {
        try
        {
            var active = EditorSceneManager.GetActiveScene();
            // 任何时刻如果场景被标脏，优先静默保存，避免后续 SaveCurrentModifiedScenesIfUserWantsTo 弹窗中断测试
            if (active.isDirty && !string.IsNullOrEmpty(active.path))
            {
                EditorSceneManager.SaveScene(active);
                UnityEngine.Debug.Log($"[EnsureCleanSceneForBatchTests] Saved scene to clear dirty: {active.path}");
                // 刷新一下 active（保存可能会触发内部状态更新）
                active = EditorSceneManager.GetActiveScene();
            }

            bool need = string.IsNullOrEmpty(active.path);
            if (!need) return;

            if (File.Exists(MainMenuScenePath))
            {
                EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
                UnityEngine.Debug.Log($"[EnsureCleanSceneForBatchTests] Opened scene: {MainMenuScenePath}");
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"[EnsureCleanSceneForBatchTests] Failed: {e}");
        }
    }
}
#endif

