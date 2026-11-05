using System.IO;
using UnityEditor;
using UnityEngine;

// Herramienta de editor: genera los sprites provisionales de la etapa 3 (32 px = 1 unidad).
// Los dibujos son fijos, así que regenerarlos da siempre los mismos sprites.
public static class PlaceholderSpriteGenerator
{
    private const string Folder = "Assets/Sprites/Environment";
    private const int Size = 32;

    [MenuItem("Unknown Game/Regenerar sprites provisionales")]
    public static void Generate()
    {
        Save("Grieta_Overlay.png", DrawCrack());
        Save("Candado.png", DrawLock());
        Save("Roca_Mortal.png", DrawDeadlyRock());
        AssetDatabase.Refresh();
    }

    // Grieta clara que se pone encima de un tile para delatar la pared falsa.
    private static Color[] DrawCrack()
    {
        Color[] pixels = Blank();
        Color light = new Color(0.85f, 0.8f, 0.68f, 1f);
        int[,] path = { { 8, 30 }, { 11, 25 }, { 9, 21 }, { 14, 17 }, { 12, 12 }, { 17, 8 }, { 15, 2 } };
        for (int i = 0; i < path.GetLength(0) - 1; i++)
            Line(pixels, path[i, 0], path[i, 1], path[i + 1, 0], path[i + 1, 1], light);
        Line(pixels, 14, 17, 22, 21, light);
        Line(pixels, 22, 21, 27, 19, light);
        Line(pixels, 12, 12, 6, 9, light);
        return pixels;
    }

    private static Color[] DrawLock()
    {
        Color[] pixels = Blank();
        Color body = new Color(0.86f, 0.72f, 0.3f, 1f);
        Color dark = new Color(0.35f, 0.26f, 0.08f, 1f);
        for (int y = 4; y < 17; y++)
            for (int x = 7; x < 25; x++)
                Set(pixels, x, y, y == 4 || y == 16 || x == 7 || x == 24 ? dark : body);
        for (int y = 17; y < 26; y++)
        {
            Set(pixels, 10, y, dark); Set(pixels, 11, y, body); Set(pixels, 12, y, dark);
            Set(pixels, 19, y, dark); Set(pixels, 20, y, body); Set(pixels, 21, y, dark);
        }
        for (int x = 10; x < 22; x++)
        {
            Set(pixels, x, 26, dark); Set(pixels, x, 27, body); Set(pixels, x, 28, dark);
        }
        for (int y = 8; y < 13; y++)
        {
            Set(pixels, 15, y, dark); Set(pixels, 16, y, dark);
        }
        return pixels;
    }

    // Roca con espinas: peligro con otro dibujo, sin comportamiento propio.
    private static Color[] DrawDeadlyRock()
    {
        Color[] pixels = Blank();
        Color rock = new Color(0.46f, 0.37f, 0.36f, 1f);
        Color dark = new Color(0.22f, 0.17f, 0.18f, 1f);
        Color thorn = new Color(0.78f, 0.2f, 0.22f, 1f);
        Vector2 center = new Vector2(15.5f, 15.5f);
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance < 8f)
                    Set(pixels, x, y, rock);
                else if (distance < 10f)
                    Set(pixels, x, y, dark);
            }
        }
        int[,] tips = { { 15, 1 }, { 15, 30 }, { 1, 15 }, { 30, 15 }, { 5, 5 }, { 26, 5 }, { 5, 26 }, { 26, 26 } };
        for (int i = 0; i < tips.GetLength(0); i++)
            Line(pixels, tips[i, 0], tips[i, 1], 15, 15, thorn, 7);
        return pixels;
    }

    private static Color[] Blank() => new Color[Size * Size];

    private static void Set(Color[] pixels, int x, int y, Color color)
    {
        if (x >= 0 && y >= 0 && x < Size && y < Size)
            pixels[y * Size + x] = color;
    }

    private static void Line(Color[] pixels, int x0, int y0, int x1, int y1, Color color, int maxSteps = 64)
    {
        int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
        for (int i = 0; i <= steps && i <= maxSteps; i++)
        {
            float t = steps == 0 ? 0f : (float)i / steps;
            Set(pixels, Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)), Mathf.RoundToInt(Mathf.Lerp(y0, y1, t)), color);
        }
    }

    private static void Save(string fileName, Color[] pixels)
    {
        string path = $"{Folder}/{fileName}";
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        texture.SetPixels(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = Size;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }
}
