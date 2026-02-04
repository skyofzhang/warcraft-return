// 依据：需求知识库 v2.2 / 程序知识库 v2.1.1 0.3：脚本后端=IL2CPP
// 用法（batchmode）：
//   -executeMethod EnforceIl2CppBackend.Run
#if UNITY_EDITOR
using UnityEditor;

public static class EnforceIl2CppBackend
{
    private const string MenuName = "WarcraftReturn/Enforce IL2CPP Backend";

    [MenuItem(MenuName)]
    public static void Run()
    {
        // Android / iOS 明确要求 IL2CPP；Standalone 不强制，避免开发机缺模块导致问题
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        UnityEngine.Debug.Log("[WarcraftReturn] Enforced IL2CPP for Android/iOS.");
    }
}
#endif

