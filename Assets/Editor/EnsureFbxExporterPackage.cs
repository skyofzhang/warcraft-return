// 安装 Unity FBX Exporter 包（batchmode 可用）
// -executeMethod EnsureFbxExporterPackage.Ensure
#if UNITY_EDITOR
using System;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

public static class EnsureFbxExporterPackage
{
    private const string PackageName = "com.unity.formats.fbx";

    public static void Ensure()
    {
        Debug.Log("[EnsureFbxExporterPackage] Start");

        if (IsInstalled())
        {
            Debug.Log("[EnsureFbxExporterPackage] Already installed: " + PackageName);
            return;
        }

        Debug.Log("[EnsureFbxExporterPackage] Installing: " + PackageName);
        AddRequest add = Client.Add(PackageName);
        Wait(add);

        if (add.Status == StatusCode.Success)
        {
            Debug.Log("[EnsureFbxExporterPackage] Installed: " + add.Result.name + "@" + add.Result.version);
        }
        else
        {
            Debug.LogError("[EnsureFbxExporterPackage] Install failed: " + add.Error?.message);
        }
    }

    private static bool IsInstalled()
    {
        ListRequest list = Client.List(true);
        Wait(list);
        if (list.Status != StatusCode.Success)
        {
            Debug.LogWarning("[EnsureFbxExporterPackage] Package list failed; assuming not installed. " + list.Error?.message);
            return false;
        }

        return list.Result != null && list.Result.Any(p => string.Equals(p.name, PackageName, StringComparison.OrdinalIgnoreCase));
    }

    private static void Wait(Request req)
    {
        var start = DateTime.UtcNow;
        while (!req.IsCompleted)
        {
            Thread.Sleep(200);
            if ((DateTime.UtcNow - start).TotalSeconds > 180)
            {
                Debug.LogError("[EnsureFbxExporterPackage] Timeout waiting for PackageManager request.");
                break;
            }
        }
    }
}
#endif

