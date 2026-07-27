using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SpriteUtils
{
    private const string PLACEHOLDER_NAME = "PLACEHOLDER";

    private static readonly Dictionary<string, Dictionary<string, Sprite>> spriteCache = new();
    public static readonly string SPRITEMAPS_PATH = "Textures";
    public static readonly string MATERIALS = "material_icons";

    /// <summary>
    /// Gets a sprite by name from a given sprite sheet, using caching to avoid reloading.
    /// If the sprite doesn't exist, returns a "placeholder" sprite instead (if present).
    /// </summary>
    public static Sprite GetSprite(string sheetName, string spriteName)
    {
        spriteName = spriteName.ToUpper();
        string fullSheetPath = Path.Combine(SPRITEMAPS_PATH, sheetName).Replace("\\", "/");

        if (!spriteCache.TryGetValue(fullSheetPath, out var spritesInSheet))
        {
            spritesInSheet = LoadSpriteSheet(fullSheetPath);
        }

        if (spritesInSheet.TryGetValue(spriteName, out Sprite foundSprite))
        {
            return foundSprite;
        }

        if (spritesInSheet.TryGetValue(PLACEHOLDER_NAME, out Sprite placeholderSprite))
        {
            Debug.LogWarning($"SpriteUtils: Sprite '{spriteName}' not found in '{fullSheetPath}', using placeholder instead.");
            return placeholderSprite;
        }

        Debug.LogWarning($"SpriteUtils: Neither sprite '{spriteName}' nor placeholder found in '{fullSheetPath}'.");
        return null;
    }
    

    public static Sprite GetRandomSpriteVariant(string sheetName, string prefix)
    {
        //Debug.Log($"GetRandomSpriteVariant {sheetName} {prefix}");
        string fullSheetPath = Path.Combine(SPRITEMAPS_PATH, sheetName).Replace("\\", "/");

        if (!spriteCache.TryGetValue(fullSheetPath, out var spritesInSheet))
        {
            spritesInSheet = LoadSpriteSheet(fullSheetPath);
        }

        List<Sprite> matchingSprites = new List<Sprite>();
        foreach (KeyValuePair<string, Sprite> kvp in spritesInSheet)
        {
            if (kvp.Key.ToUpper().StartsWith(prefix.ToUpper()))
            {
                matchingSprites.Add(kvp.Value);
            }
        }

        if (matchingSprites.Count > 0)
        {
            int randomIndex = Random.Range(0, matchingSprites.Count);
            return matchingSprites[randomIndex];
        }

        if (spritesInSheet.TryGetValue(PLACEHOLDER_NAME, out Sprite placeholderSprite))
        {
            //Debug.LogWarning($"SpriteUtils: No variants found for prefix '{prefix}' in '{fullSheetPath}', using placeholder instead.");
            return placeholderSprite;
        }

        Debug.LogWarning($"SpriteUtils: No sprites found for prefix '{prefix}' and no placeholder in '{fullSheetPath}'.");
        return null;
    }

    /// <summary>
    /// Loads all sprites from a given sheet and caches them.
    /// </summary>
    private static Dictionary<string, Sprite> LoadSpriteSheet(string fullSheetPath)
    {
        Dictionary<string, Sprite> spritesInSheet = new Dictionary<string, Sprite>();
        spriteCache[fullSheetPath] = spritesInSheet;

        Sprite[] loadedSprites = Resources.LoadAll<Sprite>(fullSheetPath);
        if (loadedSprites == null || loadedSprites.Length == 0)
        {
            Debug.LogWarning($"SpriteUtils: No sprites found in sheet '{fullSheetPath}'. Make sure it's in a Resources folder.");
            return spritesInSheet;
        }

        foreach (Sprite s in loadedSprites)
        {
            spritesInSheet[s.name.ToUpper()] = s;
        }

        return spritesInSheet;
    }
}