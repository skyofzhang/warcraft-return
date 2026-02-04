using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Bake URP Lit MaskMap from ambientCG AO + Roughness.
/// URP MaskMap packing: R=Metallic, G=Occlusion, B=DetailMask, A=Smoothness.
/// We pack Metallic=0, DetailMask=1, Occlusion=AO, Smoothness=1-Roughness.
/// </summary>
public static class CC0MaskMapBaker
{
    private const string RootFolder = "Assets/Resources/Environment/Textures";

    [MenuItem("Tools/CC0/Bake URP MaskMaps (AO+Roughness -> MaskMap)")]
    public static void BakeAll()
    {
        if (!AssetDatabase.IsValidFolder(RootFolder))
        {
            Debug.LogWarning($"[CC0] Folder not found: {RootFolder}");
            return;
        }

        // Find all Color textures and attempt to locate corresponding AO/Roughness.
        string[] colorGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { RootFolder });
        int baked = 0;
        int skipped = 0;

        for (int i = 0; i < colorGuids.Length; i++)
        {
            string colorPath = AssetDatabase.GUIDToAssetPath(colorGuids[i]);
            if (!colorPath.EndsWith("_Color.jpg", StringComparison.OrdinalIgnoreCase) &&
                !colorPath.EndsWith("_Color.png", StringComparison.OrdinalIgnoreCase))
                continue;

            string aoPath = ReplaceSuffix(colorPath, "_Color", "_AmbientOcclusion");
            string roughPath = ReplaceSuffix(colorPath, "_Color", "_Roughness");
            if (!File.Exists(aoPath) || !File.Exists(roughPath))
            {
                skipped++;
                continue;
            }

            string maskPath = ReplaceSuffix(colorPath, "_Color", "_MaskMap");
            maskPath = Path.ChangeExtension(maskPath, ".png");

            if (File.Exists(maskPath))
            {
                // already baked
                skipped++;
                continue;
            }

            try
            {
                if (BakeOne(aoPath, roughPath, maskPath))
                {
                    baked++;
                }
                else
                {
                    skipped++;
                }
            }
            catch (Exception ex)
            {
                skipped++;
                Debug.LogError($"[CC0] Mask bake failed: {maskPath}\n{ex}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[CC0] MaskMap bake done. Baked={baked}, Skipped={skipped}");
    }

    private static bool BakeOne(string aoAssetPath, string roughAssetPath, string outAssetPath)
    {
        var aoTex = LoadReadable(aoAssetPath, out var aoPrevReadable);
        var roughTex = LoadReadable(roughAssetPath, out var roughPrevReadable);
        if (aoTex == null || roughTex == null) return false;

        int w = Mathf.Min(aoTex.width, roughTex.width);
        int h = Mathf.Min(aoTex.height, roughTex.height);
        if (w <= 4 || h <= 4) return false;

        var aoPixels = aoTex.GetPixels32();
        var roughPixels = roughTex.GetPixels32();

        // If sizes differ, resample by nearest.
        Func<int, int, Color32> sampleAo = (x, y) =>
        {
            int sx = Mathf.Clamp(Mathf.RoundToInt((x / (float)(w - 1)) * (aoTex.width - 1)), 0, aoTex.width - 1);
            int sy = Mathf.Clamp(Mathf.RoundToInt((y / (float)(h - 1)) * (aoTex.height - 1)), 0, aoTex.height - 1);
            return aoPixels[sy * aoTex.width + sx];
        };
        Func<int, int, Color32> sampleRough = (x, y) =>
        {
            int sx = Mathf.Clamp(Mathf.RoundToInt((x / (float)(w - 1)) * (roughTex.width - 1)), 0, roughTex.width - 1);
            int sy = Mathf.Clamp(Mathf.RoundToInt((y / (float)(h - 1)) * (roughTex.height - 1)), 0, roughTex.height - 1);
            return roughPixels[sy * roughTex.width + sx];
        };

        var mask = new Texture2D(w, h, TextureFormat.RGBA32, mipChain: true, linear: true);
        mask.name = Path.GetFileNameWithoutExtension(outAssetPath);

        var outPixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color32 ao = sampleAo(x, y);
                Color32 ro = sampleRough(x, y);

                byte occlusion = ao.r;                 // G
                byte roughness = ro.r;                 // use R channel
                byte smoothness = (byte)(255 - roughness); // A

                outPixels[y * w + x] = new Color32(
                    0,          // R metallic
                    occlusion,  // G occlusion
                    255,        // B detail mask
                    smoothness  // A smoothness
                );
            }
        }

        mask.SetPixels32(outPixels);
        // IMPORTANT:
        // EncodeToPNG requires the texture to remain readable.
        // We'll keep it readable for encoding, then let the imported asset be non-readable.
        mask.Apply(updateMipmaps: true, makeNoLongerReadable: false);

        Directory.CreateDirectory(Path.GetDirectoryName(outAssetPath) ?? ".");
        var png = mask.EncodeToPNG();
        File.WriteAllBytes(outAssetPath, png);
        UnityEngine.Object.DestroyImmediate(mask);

        // Import and set as linear.
        AssetDatabase.ImportAsset(outAssetPath, ImportAssetOptions.ForceUpdate);
        var ti = AssetImporter.GetAtPath(outAssetPath) as TextureImporter;
        if (ti != null)
        {
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = false;
            ti.isReadable = false;
            ti.mipmapEnabled = true;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.maxTextureSize = 1024;
            ti.SaveAndReimport();
        }

        RestoreReadable(aoAssetPath, aoPrevReadable);
        RestoreReadable(roughAssetPath, roughPrevReadable);
        return true;
    }

    private static Texture2D LoadReadable(string assetPath, out bool prevReadable)
    {
        prevReadable = false;
        var ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (ti == null) return null;

        prevReadable = ti.isReadable;
        if (!ti.isReadable)
        {
            ti.isReadable = true;
            ti.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
    }

    private static void RestoreReadable(string assetPath, bool prevReadable)
    {
        var ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (ti == null) return;
        if (ti.isReadable != prevReadable)
        {
            ti.isReadable = prevReadable;
            ti.SaveAndReimport();
        }
    }

    private static string ReplaceSuffix(string path, string fromSuffix, string toSuffix)
    {
        // Replace only the last occurrence before extension.
        string ext = Path.GetExtension(path);
        string noExt = path.Substring(0, path.Length - ext.Length);
        if (noExt.EndsWith(fromSuffix, StringComparison.OrdinalIgnoreCase))
            return noExt.Substring(0, noExt.Length - fromSuffix.Length) + toSuffix + ext;

        // Fallback: plain replace.
        return path.Replace(fromSuffix, toSuffix, StringComparison.OrdinalIgnoreCase);
    }
}

