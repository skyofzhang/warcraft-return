// 自动把 Assets/Resources/UI 下的 PNG 设为 Sprite (2D and UI)。
// 目的：保证 Resources.Load<Sprite>("UI/...") 可直接拿到切图，不必手动逐张设置。
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class UISpriteImportPostprocessor : AssetPostprocessor
{
    private const string UiResourcesFolder = "/Resources/UI/";

    private void OnPreprocessTexture()
    {
        if (assetPath == null) return;
        if (!assetPath.EndsWith(".png")) return;
        if (assetPath.IndexOf(UiResourcesFolder) < 0) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.sRGBTexture = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
    }
}
#endif

