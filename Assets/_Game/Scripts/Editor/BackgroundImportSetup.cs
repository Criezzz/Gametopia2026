#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor utility to set correct import settings for backgrounds.
/// Run via menu: Tools > Setup Background Imports.
/// </summary>
public class BackgroundImportSetup
{
    [MenuItem("Tools/Setup Background Imports")]
    public static void SetupImports()
    {
        // Cloud: user imports manually as Sprite with Wrap Mode = Repeat

        // Building: Sprite, Point filter
        SetSpriteImport("Assets/_Game/Art/Background/farthest_building.png", 16);

        // Platform: Sprite, Point filter
        SetSpriteImport("Assets/_Game/Art/Background/platform_background.png", 16);

        // Near building: Sprite, Point filter
        SetSpriteImport("Assets/_Game/Art/Background/building_near.png", 16);

        // Color background: Sprite, Point filter
        SetSpriteImport("Assets/_Game/Art/Background/color_bg.png", 16);

        Debug.Log("[BackgroundImportSetup] All background imports configured!");
    }

    private static void SetTextureImport(string path, TextureImporterType type,
        FilterMode filter, TextureWrapMode wrap)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) { Debug.LogWarning($"Texture not found: {path}"); return; }

        importer.textureType = type;
        importer.filterMode = filter;
        importer.wrapMode = wrap;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static void SetSpriteImport(string path, int ppu)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) { Debug.LogWarning($"Texture not found: {path}"); return; }

        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }
}
#endif