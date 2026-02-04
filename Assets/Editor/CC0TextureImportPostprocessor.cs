using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Auto-fix import settings for CC0 textures (ambientCG naming).
/// Ensures normals are imported as NormalMap, and data maps (AO/Roughness/Displacement) are linear (sRGB off).
/// </summary>
public class CC0TextureImportPostprocessor : AssetPostprocessor
{
    private static bool ContainsAny(string s, params string[] tokens)
    {
        if (string.IsNullOrEmpty(s)) return false;
        for (int i = 0; i < tokens.Length; i++)
        {
            if (s.IndexOf(tokens[i], StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }
        return false;
    }

    private void OnPreprocessTexture()
    {
        var importer = (TextureImporter)assetImporter;
        if (importer == null) return;

        // Only touch our downloaded CC0 textures by default (keeps project stable).
        // Path example: Assets/Resources/Environment/Textures/Ground003_1K/Ground003_1K-JPG_NormalGL.jpg
        if (!assetPath.Replace('\\', '/').Contains("/Resources/Environment/Textures/", StringComparison.OrdinalIgnoreCase))
            return;

        string file = System.IO.Path.GetFileName(assetPath);
        bool isNormal = ContainsAny(file, "_NormalGL", "_NormalDX");
        bool isAo = ContainsAny(file, "_AmbientOcclusion", "_AO");
        bool isRough = ContainsAny(file, "_Roughness");
        bool isDisp = ContainsAny(file, "_Displacement", "_Height");
        bool isColor = ContainsAny(file, "_Color", "_Albedo", "_Diffuse");

        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = true;
        importer.maxTextureSize = 1024;

        if (isNormal)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.sRGBTexture = false;
        }
        else if (isAo || isRough || isDisp)
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
        }
        else if (isColor)
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
        }
    }
}

