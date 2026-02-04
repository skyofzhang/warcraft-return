// 安装 URP 后执行：创建 URP Pipeline Asset 并写入 Graphics/Quality Settings
// 目标：Unity 2022.3 + URP 14.x
// 用法（batchmode）：
//   -executeMethod SetupUrp14.Run
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class SetupUrp14
{
    private const string MenuName = "WarcraftReturn/Setup URP (14.x)";
    private const string FolderRoot = "Assets/Settings/RenderPipeline";
    private const string UrpAssetPath = FolderRoot + "/URP_Asset.asset";
    private const string UrpRendererPath = FolderRoot + "/URP_Renderer.asset";

    [MenuItem(MenuName)]
    public static void Run()
    {
        EnsureFolders();

        // 先创建并保存 RendererData（使用内置 ForwardRenderer 模板，避免缺 shader 引用）
        var temp = UniversalRenderPipelineAsset.Create();
        var rendererData = temp.LoadBuiltinRendererData(RendererType.UniversalRenderer);
        if (rendererData == null)
        {
            Debug.LogError("[SetupUrp14] Failed to create builtin UniversalRendererData.");
            return;
        }
        Object.DestroyImmediate(temp);

        // 覆盖旧资产（如果存在）
        TryDeleteAsset(UrpAssetPath);
        TryDeleteAsset(UrpRendererPath);

        // LoadBuiltinRendererData 可能已经在 Assets/ 下创建了默认资产（例如 Assets/UniversalRenderer.asset）
        // 这里统一把它移动到指定目录，避免 CreateAsset 报“已是 asset”。
        string currentRendererPath = AssetDatabase.GetAssetPath(rendererData);
        if (!string.IsNullOrEmpty(currentRendererPath))
        {
            if (currentRendererPath != UrpRendererPath)
            {
                // 目标路径若存在则先删（避免 MoveAsset 失败）
                TryDeleteAsset(UrpRendererPath);
                string moveErr = AssetDatabase.MoveAsset(currentRendererPath, UrpRendererPath);
                if (!string.IsNullOrEmpty(moveErr))
                {
                    Debug.LogError($"[SetupUrp14] Move rendererData failed: {moveErr}");
                    return;
                }
                rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(UrpRendererPath);
            }
        }
        else
        {
            AssetDatabase.CreateAsset(rendererData, UrpRendererPath);
        }

        // 创建并保存 URP Asset，并绑定 rendererData
        var urp = UniversalRenderPipelineAsset.Create(rendererData);
        if (urp == null)
        {
            Debug.LogError("[SetupUrp14] Failed to create URP asset.");
            return;
        }

        // 首版性能约束：关闭不必要开销（可再根据需求调整）
        urp.supportsHDR = false;
        urp.supportsCameraDepthTexture = false;
        urp.supportsCameraOpaqueTexture = false;
        urp.msaaSampleCount = 1;
        urp.useSRPBatcher = true;
        urp.supportsDynamicBatching = true;
        urp.shadowDistance = 0f;

        // 程序知识库 v2.1.1 0.5：禁止实时阴影、禁止后处理
        // URP 的“主光源阴影开关”没有 public setter，这里用 SerializedObject 强制写入。
        var so = new SerializedObject(urp);
        so.FindProperty("m_MainLightShadowsSupported").intValue = 0;
        so.FindProperty("m_AdditionalLightShadowsSupported").intValue = 0;
        so.FindProperty("m_AnyShadowsSupported").intValue = 0;
        so.FindProperty("m_ShadowDistance").floatValue = 0f;
        so.FindProperty("m_ShadowCascadeCount").intValue = 1;
        so.FindProperty("m_SoftShadowsSupported").intValue = 0;
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(urp, UrpAssetPath);
        EditorUtility.SetDirty(urp);
        EditorUtility.SetDirty(rendererData);
        AssetDatabase.SaveAssets();

        // 写入 GraphicsSettings（默认管线）
        GraphicsSettings.renderPipelineAsset = urp;

        // 写入 QualitySettings（每个质量档位都指向同一 URP Asset，避免“切换质量导致丢管线”）
        int old = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
            QualitySettings.renderPipeline = urp;
        }
        QualitySettings.SetQualityLevel(old, applyExpensiveChanges: false);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[SetupUrp14] URP ready. Asset={UrpAssetPath}");
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            AssetDatabase.CreateFolder("Assets", "Settings");
        if (!AssetDatabase.IsValidFolder("Assets/Settings/RenderPipeline"))
            AssetDatabase.CreateFolder("Assets/Settings", "RenderPipeline");

        // 保证目录存在（防止某些 CI 环境下 CreateFolder 失败时静默）
        var abs = Path.Combine(Directory.GetCurrentDirectory(), FolderRoot);
        Directory.CreateDirectory(abs);
    }

    private static void TryDeleteAsset(string path)
    {
        if (File.Exists(path) || AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            AssetDatabase.DeleteAsset(path);
    }
}
#endif

