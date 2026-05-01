// TileAssetPostprocessor.cs — Editor-only.
// Automatically imports every PNG under Assets/Resources/Tiles/ and Assets/Resources/UI/ as a Sprite.
// Also provides menu items to reimport already-imported textures.
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class TileAssetPostprocessor : AssetPostprocessor
{
    private const string TilesRoot = "Assets/Resources/Tiles/";
    private const string UIRoot    = "Assets/Resources/UI/";

    void OnPreprocessTexture()
    {
        bool isTile = assetPath.StartsWith(TilesRoot, System.StringComparison.OrdinalIgnoreCase);
        bool isUI   = assetPath.StartsWith(UIRoot,    System.StringComparison.OrdinalIgnoreCase);
        if (!isTile && !isUI) return;

        var imp = (TextureImporter)assetImporter;
        if (imp.textureType == TextureImporterType.Sprite) return;

        imp.textureType         = TextureImporterType.Sprite;
        imp.spriteImportMode    = SpriteImportMode.Single;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled       = false;
        imp.filterMode          = FilterMode.Bilinear;
        imp.textureCompression  = TextureImporterCompression.Uncompressed;
        Debug.Log($"[Import] Set Sprite type: {assetPath}");
    }

    [MenuItem("Crossword/Reimport Tile Sprites")]
    static void ReimportTileSprites() => ReimportFolder("Assets/Resources/Tiles", "[TileImport]",
        () => TileSpriteLoader.ClearCache());

    [MenuItem("Crossword/Reimport UI Sprites")]
    static void ReimportUISprites() => ReimportFolder("Assets/Resources/UI", "[UIImport]",
        () => UIAssetLoader.ClearCache());

    static void ReimportFolder(string folder, string logTag, System.Action onDone)
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        int count = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var imp  = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;

            bool changed = false;
            if (imp.textureType != TextureImporterType.Sprite)
            { imp.textureType = TextureImporterType.Sprite; changed = true; }
            if (imp.spriteImportMode != SpriteImportMode.Single)
            { imp.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (!imp.alphaIsTransparency)
            { imp.alphaIsTransparency = true; changed = true; }
            if (imp.mipmapEnabled)
            { imp.mipmapEnabled = false; changed = true; }
            if (imp.textureCompression != TextureImporterCompression.Uncompressed)
            { imp.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }

            if (changed)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                count++;
            }
        }
        AssetDatabase.Refresh();
        onDone?.Invoke();
        Debug.Log($"{logTag} Reimported {count} sprites. Resources.Load<Sprite>() will now work.");
    }
}
#endif
