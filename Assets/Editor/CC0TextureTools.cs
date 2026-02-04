using System;
using UnityEditor;
using UnityEngine;

public static class CC0TextureTools
{
    private const string Root = "Assets/Resources/Environment/Textures";

    [MenuItem("Tools/CC0/Reimport Map Textures (Fix Normal/AO/Roughness)")]
    public static void ReimportMapTextures()
    {
        if (!AssetDatabase.IsValidFolder(Root))
        {
            Debug.LogWarning($"[CC0] Folder not found: {Root}");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Root });
        int changed = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;

            string file = System.IO.Path.GetFileName(path);
            bool isNormal = file.IndexOf("_NormalGL", StringComparison.OrdinalIgnoreCase) >= 0
                            || file.IndexOf("_NormalDX", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isAo = file.IndexOf("_AmbientOcclusion", StringComparison.OrdinalIgnoreCase) >= 0
                        || file.IndexOf("_AO", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isRough = file.IndexOf("_Roughness", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isDisp = file.IndexOf("_Displacement", StringComparison.OrdinalIgnoreCase) >= 0
                          || file.IndexOf("_Height", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isColor = file.IndexOf("_Color", StringComparison.OrdinalIgnoreCase) >= 0
                           || file.IndexOf("_Albedo", StringComparison.OrdinalIgnoreCase) >= 0
                           || file.IndexOf("_Diffuse", StringComparison.OrdinalIgnoreCase) >= 0;

            bool dirty = false;
            if (ti.wrapMode != TextureWrapMode.Repeat) { ti.wrapMode = TextureWrapMode.Repeat; dirty = true; }
            if (ti.filterMode != FilterMode.Bilinear) { ti.filterMode = FilterMode.Bilinear; dirty = true; }
            if (ti.mipmapEnabled != true) { ti.mipmapEnabled = true; dirty = true; }
            if (ti.maxTextureSize != 1024) { ti.maxTextureSize = 1024; dirty = true; }

            if (isNormal)
            {
                if (ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; dirty = true; }
                if (ti.sRGBTexture != false) { ti.sRGBTexture = false; dirty = true; }
            }
            else if (isAo || isRough || isDisp)
            {
                if (ti.textureType != TextureImporterType.Default) { ti.textureType = TextureImporterType.Default; dirty = true; }
                if (ti.sRGBTexture != false) { ti.sRGBTexture = false; dirty = true; }
            }
            else if (isColor)
            {
                if (ti.textureType != TextureImporterType.Default) { ti.textureType = TextureImporterType.Default; dirty = true; }
                if (ti.sRGBTexture != true) { ti.sRGBTexture = true; dirty = true; }
            }

            if (dirty)
            {
                changed++;
                ti.SaveAndReimport();
            }
        }

        Debug.Log($"[CC0] Reimport done. Updated={changed}, Total={guids.Length}");
    }
}

