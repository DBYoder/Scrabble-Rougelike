// TileAssetPostprocessor.cs — Editor-only.
// Automatically imports every PNG under Assets/Resources/Tiles/ as a Sprite.
// Also provides "Crossword/Reimport Tile Sprites" to fix PNGs already on disk.
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class TileAssetPostprocessor : AssetPostprocessor
{
    private const string TilesRoot = "Assets/Resources/Tiles/";

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(TilesRoot, System.StringComparison.OrdinalIgnoreCase))
            return;

        var imp = (TextureImporter)assetImporter;
        if (imp.textureType == TextureImporterType.Sprite) return; // already correct

        imp.textureType          = TextureImporterType.Sprite;
        imp.spriteImportMode     = SpriteImportMode.Single;
        imp.alphaIsTransparency  = true;
        imp.mipmapEnabled        = false;
        imp.filterMode           = FilterMode.Bilinear;
        imp.textureCompression   = TextureImporterCompression.Uncompressed;
        Debug.Log($"[TileImport] Set Sprite type: {assetPath}");
    }

    // Run this once after the initial PNG copy to fix already-imported textures.
    [MenuItem("Crossword/Reimport Tile Sprites")]
    static void ReimportTileSprites()
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Tiles" });
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
        Debug.Log($"[TileImport] Reimported {count} tile sprites. Resources.Load<Sprite>() will now work.");
    }
}
#endif
