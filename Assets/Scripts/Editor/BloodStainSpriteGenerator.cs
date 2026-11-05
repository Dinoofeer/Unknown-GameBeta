using System.IO;
using UnityEditor;
using UnityEngine;

// Herramienta de editor: genera las variantes de mancha en pixel art (32 px = 1 unidad).
// Se dibujan en escala de grises: el color lo pone el SpriteRenderer (rojo = jugador, gris = antiguas).
// Las semillas son fijas, así que regenerarlas da siempre los mismos sprites.
public static class BloodStainSpriteGenerator
{
    private const string Folder = "Assets/Sprites/Effects";
    private const int Size = 32;
    private const int Variants = 4;
    private const float EdgeShade = 0.55f;
    private const float DarkShade = 0.68f;
    private const float BodyShade = 0.82f;
    private const float HighlightShade = 1f;

    [MenuItem("Unknown Game/Regenerar sprites de manchas")]
    public static void Generate()
    {
        Directory.CreateDirectory(Folder);
        for (int i = 1; i <= Variants; i++)
        {
            string path = $"{Folder}/Stain_{i:00}.png";
            File.WriteAllBytes(path, DrawStain(new System.Random(1000 + i * 37)));
            AssetDatabase.ImportAsset(path);
            ApplyImportSettings(path);
        }
        AssetDatabase.Refresh();
    }

    private static byte[] DrawStain(System.Random random)
    {
        var mask = new bool[Size, Size];
        float centerX = 15.5f + Range(random, -1f, 1f);
        float centerY = 15.5f + Range(random, -1f, 1f);

        // Mancha central irregular: un círculo con varios lóbulos pegados.
        Stamp(mask, centerX, centerY, Range(random, 4.5f, 5.5f));
        int lobes = random.Next(3, 6);
        for (int i = 0; i < lobes; i++)
        {
            float angle = Range(random, 0f, Mathf.PI * 2f);
            float distance = Range(random, 3.5f, 5.5f);
            Stamp(mask, centerX + Mathf.Cos(angle) * distance, centerY + Mathf.Sin(angle) * distance, Range(random, 1.5f, 2.8f));
        }

        // Regueros que salen del centro y se afinan, con una gota al final.
        int arms = random.Next(3, 6);
        for (int i = 0; i < arms; i++)
        {
            float angle = Range(random, 0f, Mathf.PI * 2f);
            float length = Range(random, 8f, 12f);
            for (float distance = 4f; distance < length; distance += 0.5f)
            {
                float radius = Mathf.Lerp(1.4f, 0.6f, (distance - 4f) / (length - 4f));
                Stamp(mask, centerX + Mathf.Cos(angle) * distance, centerY + Mathf.Sin(angle) * distance, radius);
            }
            Stamp(mask, centerX + Mathf.Cos(angle) * length, centerY + Mathf.Sin(angle) * length, Range(random, 1f, 1.5f));
        }

        // Gotas sueltas alrededor.
        var droplets = new bool[Size, Size];
        int dropletCount = random.Next(6, 11);
        for (int i = 0; i < dropletCount; i++)
        {
            float angle = Range(random, 0f, Mathf.PI * 2f);
            float distance = Range(random, 9f, 14f);
            Stamp(droplets, centerX + Mathf.Cos(angle) * distance, centerY + Mathf.Sin(angle) * distance, Range(random, 0.5f, 1.1f));
        }

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                bool inMask = mask[x, y];
                float shade = inMask ? ShadeOf(mask, x, y, centerX, centerY) : DarkShade;
                bool visible = inMask || droplets[x, y];
                texture.SetPixel(x, y, visible ? new Color(shade, shade, shade, 1f) : Color.clear);
            }
        }
        texture.Apply();
        byte[] png = texture.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(texture);
        return png;
    }

    private static float ShadeOf(bool[,] mask, int x, int y, float centerX, float centerY)
    {
        if (!Filled(mask, x - 1, y) || !Filled(mask, x + 1, y) || !Filled(mask, x, y - 1) || !Filled(mask, x, y + 1))
            return EdgeShade;

        // Luz arriba a la izquierda: brillo pequeño y sombra en la parte contraria.
        float highlightX = x - (centerX - 2f);
        float highlightY = y - (centerY + 2f);
        if (highlightX * highlightX + highlightY * highlightY <= 2.6f)
            return HighlightShade;
        if (!Filled(mask, x + 2, y) || !Filled(mask, x, y - 2))
            return DarkShade;
        return BodyShade;
    }

    private static bool Filled(bool[,] mask, int x, int y)
    {
        return x >= 0 && y >= 0 && x < Size && y < Size && mask[x, y];
    }

    private static void Stamp(bool[,] mask, float centerX, float centerY, float radius)
    {
        for (int y = 1; y < Size - 1; y++)
        {
            for (int x = 1; x < Size - 1; x++)
            {
                float dx = x - centerX;
                float dy = y - centerY;
                if (dx * dx + dy * dy <= radius * radius)
                    mask[x, y] = true;
            }
        }
    }

    private static float Range(System.Random random, float min, float max)
    {
        return min + (float)random.NextDouble() * (max - min);
    }

    private static void ApplyImportSettings(string path)
    {
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
