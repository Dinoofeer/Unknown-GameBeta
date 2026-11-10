using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

// Herramienta de editor: genera el arte del Nivel 1 (tiles, fondos, llave y partículas) y lo coloca en la escena.
// Los dibujos usan semillas fijas, así que regenerarlos da siempre lo mismo. No cambia colisiones ni reglas.
public static class LevelArtGenerator
{
    private const string Env = "Assets/Sprites/Environment";
    private const string Tiles = "Assets/Sprites/Environment/Tiles";
    private const string Fx = "Assets/Materials/VFX";
    private const float DamagedFromX = 150f;

    private static readonly Color MetalBase = new Color(0.33f, 0.35f, 0.40f);
    private static readonly Color RustBase = new Color(0.31f, 0.27f, 0.28f);

    [MenuItem("Unknown Game/Arte/1. Generar sprites")]
    public static void GenerateSprites()
    {
        Directory.CreateDirectory(Fx);
        SaveTile("Tile_Metal", DrawTile(MetalBase, false, 0, false));
        SaveTile("Tile_Metal_2", DrawTile(MetalBase, false, 1, false));
        SaveTile("Tile_Metal_3", DrawTile(MetalBase, false, 2, false));
        SaveTile("Tile_Metal_Top", DrawTile(MetalBase, true, 0, false));
        SaveTile("Tile_Rust_1", DrawTile(RustBase, false, 0, true));
        SaveTile("Tile_Rust_2", DrawTile(RustBase, false, 1, true));
        SaveTile("Tile_Rust_3", DrawTile(RustBase, false, 2, true));
        SaveTile("Tile_Rust_Top", DrawTile(RustBase, true, 0, true));
        SaveTile("Tile_Platform", DrawBrick());

        SaveSprite("BG_Racks_Clean.png", DrawRacks(false), 128, 32, true);
        SaveSprite("BG_Racks_Broken.png", DrawRacks(true), 128, 32, true);
        SaveSprite("BG_Cables_Clean.png", DrawCables(false), 128, 32, true);
        SaveSprite("BG_Cables_Broken.png", DrawCables(true), 128, 32, true);
        SaveSprite("Llave.png", DrawKey(), 32, 32, false);
        SaveSprite("Particula.png", Solid(4), 4, 32, false);
        AssetDatabase.Refresh();

        foreach (string name in new[] { "Tile_Metal_2", "Tile_Metal_3", "Tile_Metal_Top", "Tile_Rust_1", "Tile_Rust_2", "Tile_Rust_3", "Tile_Rust_Top" })
            EnsureTileAsset(name);
    }

    [MenuItem("Unknown Game/Arte/2. Aplicar a Level_01")]
    public static void ApplyToScene()
    {
        PaintTilemap();
        DisguiseTraps();
        BuildParallax();
        BuildAmbientAndDebris();
        BuildLightsAndKey();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    // ---------- Escena ----------

    private static void PaintTilemap()
    {
        Tilemap solid = FindMap("Solid");
        Tile[] clean = { Load("Tile_Metal"), Load("Tile_Metal"), Load("Tile_Metal_2"), Load("Tile_Metal_3") };
        Tile[] rust = { Load("Tile_Rust_1"), Load("Tile_Rust_1"), Load("Tile_Rust_2"), Load("Tile_Rust_3") };
        Tile cleanTop = Load("Tile_Metal_Top");
        Tile rustTop = Load("Tile_Rust_Top");

        BoundsInt bounds = solid.cellBounds;
        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            if (!solid.HasTile(cell))
                continue;
            bool damaged = solid.GetCellCenterWorld(cell).x >= DamagedFromX;
            bool top = !solid.HasTile(cell + Vector3Int.up) && !IsUnderDisguisedFloor(solid.GetCellCenterWorld(cell));
            // Hash fijo: la distribución de variantes no cambia entre regeneraciones.
            int hash = Mathf.Abs(cell.x * 73856093 ^ cell.y * 19349663) % 4;
            Tile tile = top ? (damaged ? rustTop : cleanTop) : (damaged ? rust : clean)[hash];
            solid.SetTile(cell, tile);
        }
    }

    // El fondo de un foso tapado no debe llevar el brillo de superficie: asomaría bajo el piso falso y lo delataría.
    private static bool IsUnderDisguisedFloor(Vector3 cellCenter)
    {
        foreach (FallingPlatform platform in Object.FindObjectsByType<FallingPlatform>(FindObjectsInactive.Include))
        {
            Transform visual = platform.transform.Find("Visual");
            SpriteRenderer renderer = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
            if (renderer == null || renderer.size.y <= 1.01f)
                continue;

            Bounds bounds = renderer.bounds;
            if (cellCenter.x > bounds.min.x && cellCenter.x < bounds.max.x && cellCenter.y < bounds.min.y && cellCenter.y > bounds.min.y - 0.5f)
                return true;
        }
        return false;
    }

    // Los pisos y paredes falsos usan el mismo tile que el suelo real de su zona, también en el borde superior.
    private static void DisguiseTraps()
    {
        foreach (FallingPlatform platform in Object.FindObjectsByType<FallingPlatform>(FindObjectsInactive.Include))
        {
            Transform visual = platform.transform.Find("Visual");
            SpriteRenderer renderer = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
            if (renderer == null || renderer.sprite == null || !renderer.sprite.name.StartsWith("Tile_") || renderer.sprite.name == "Tile_Platform")
                continue;

            bool damaged = platform.transform.position.x >= DamagedFromX;
            renderer.sprite = Load(damaged ? "Tile_Rust_1" : "Tile_Metal").sprite;

            Transform edge = visual.Find("Borde");
            if (edge == null)
            {
                edge = new GameObject("Borde").AddComponent<SpriteRenderer>().transform;
                edge.SetParent(visual, false);
            }
            var edgeRenderer = edge.GetComponent<SpriteRenderer>();
            edgeRenderer.sprite = Load(damaged ? "Tile_Rust_Top" : "Tile_Metal_Top").sprite;
            edgeRenderer.drawMode = SpriteDrawMode.Tiled;
            edgeRenderer.size = new Vector2(renderer.size.x, 0.5f);
            edgeRenderer.sharedMaterial = renderer.sharedMaterial;
            edgeRenderer.sortingOrder = renderer.sortingOrder + 1;
            edge.localPosition = new Vector3(0f, renderer.size.y * 0.5f - 0.25f, 0f);
        }

        foreach (FakeWall wall in Object.FindObjectsByType<FakeWall>(FindObjectsInactive.Include))
        {
            bool damaged = wall.transform.position.x >= DamagedFromX;
            wall.GetComponent<SpriteRenderer>().sprite = Load(damaged ? "Tile_Rust_1" : "Tile_Metal").sprite;
        }
    }

    private static void BuildParallax()
    {
        GameObject old = GameObject.Find("Parallax");
        if (old != null)
            Object.DestroyImmediate(old);

        var root = new GameObject("Parallax");
        Material material = GameObject.Find("Background").GetComponent<SpriteRenderer>().sharedMaterial;
        BuildLayer(root.transform, "Racks", "BG_Racks", 0.85f, -9, new Color(0.75f, 0.8f, 0.9f), material);
        BuildLayer(root.transform, "Cables", "BG_Cables", 0.55f, -8, Color.white, material);
    }

    private static void BuildLayer(Transform parent, string name, string sprite, float factor, int order, Color tint, Material material)
    {
        var layer = new GameObject(name);
        layer.transform.SetParent(parent, false);
        layer.transform.position = new Vector3(165f, 6f, 0f);

        SpriteRenderer clean = BuildSheet(layer.transform, "Limpio", sprite + "_Clean", order, tint, material, 1f);
        SpriteRenderer broken = BuildSheet(layer.transform, "Roto", sprite + "_Broken", order, tint, material, 0f);

        var parallax = layer.AddComponent<ParallaxLayer>();
        var so = new SerializedObject(parallax);
        so.FindProperty("factor").floatValue = factor;
        so.FindProperty("cleanLayer").objectReferenceValue = clean;
        so.FindProperty("brokenLayer").objectReferenceValue = broken;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static SpriteRenderer BuildSheet(Transform parent, string name, string sprite, int order, Color tint, Material material, float alpha)
    {
        var sheet = new GameObject(name).AddComponent<SpriteRenderer>();
        sheet.transform.SetParent(parent, false);
        sheet.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Env}/{sprite}.png");
        sheet.drawMode = SpriteDrawMode.Tiled;
        sheet.size = new Vector2(420f, 44f);
        sheet.sortingOrder = order;
        sheet.sharedMaterial = material;
        tint.a = alpha;
        sheet.color = tint;
        return sheet;
    }

    private static void BuildAmbientAndDebris()
    {
        Material material = ParticleMaterial();
        Transform camera = Camera.main.transform;

        Transform oldAmbient = camera.Find("ParticulasAmbiente");
        if (oldAmbient != null)
            Object.DestroyImmediate(oldAmbient.gameObject);
        var ambient = new GameObject("ParticulasAmbiente");
        ambient.transform.SetParent(camera, false);
        ambient.transform.localPosition = new Vector3(0f, 0f, 10f);

        ParticleSystem dust = BuildAmbient(ambient.transform, "Polvo", material, new Color(0.75f, 0.9f, 1f, 0.28f), 7f, 7f, 0.06f, new Vector2(0.05f, 0.2f));
        ParticleSystem sparks = BuildAmbient(ambient.transform, "Chispas", material, new Color(1f, 0.45f, 0.2f, 0.75f), 14f, 1.6f, 0.05f, new Vector2(0.4f, 1.6f));
        var zone = ambient.AddComponent<AmbientParticlesZone>();
        var so = new SerializedObject(zone);
        so.FindProperty("cleanZone").objectReferenceValue = dust;
        so.FindProperty("brokenZone").objectReferenceValue = sparks;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject debrisPrefab = BuildDebrisPrefab(material);
        foreach (FallingPlatform platform in Object.FindObjectsByType<FallingPlatform>(FindObjectsInactive.Include))
        {
            Transform previous = platform.transform.Find("PlatformDebris");
            if (previous != null)
                Object.DestroyImmediate(previous.gameObject);
            var debris = (GameObject)PrefabUtility.InstantiatePrefab(debrisPrefab, platform.transform);
            debris.name = "PlatformDebris";
            debris.transform.localPosition = Vector3.zero;
        }

        ImproveDeathParticles(material);
    }

    private static ParticleSystem BuildAmbient(Transform parent, string name, Material material, Color color, float rate, float life, float size, Vector2 speed)
    {
        var system = new GameObject(name).AddComponent<ParticleSystem>();
        system.transform.SetParent(parent, false);
        ParticleSystem.MainModule main = system.main;
        main.loop = true;
        main.startLifetime = life;
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
        main.startSize = size;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 120;
        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = rate;
        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(30f, 18f, 1f);
        var renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingOrder = 8;
        return system;
    }

    private static GameObject BuildDebrisPrefab(Material material)
    {
        const string path = "Assets/Prefabs/Effects/PlatformDebris.prefab";
        var go = new GameObject("PlatformDebris");
        var system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = system.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.2f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
        main.startColor = new Color(0.62f, 0.62f, 0.66f, 1f);
        main.gravityModifier = 2.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 22) });
        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(2f, 0.5f, 1f);
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingOrder = 9;
        go.AddComponent<PlatformFallDebris>();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static void ImproveDeathParticles(Material material)
    {
        const string path = "Assets/Prefabs/Effects/DeathParticles.prefab";
        GameObject contents = PrefabUtility.LoadPrefabContents(path);
        var system = contents.GetComponent<ParticleSystem>();
        if (system != null)
        {
            ParticleSystem.MainModule main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 10f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.2f, 0.85f, 1f), Color.white);
            main.gravityModifier = 1.5f;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 42) });
            var renderer = contents.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            PrefabUtility.SaveAsPrefabAsset(contents, path);
        }
        PrefabUtility.UnloadPrefabContents(contents);
    }

    private static void BuildLightsAndKey()
    {
        foreach (Light2D light in Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include))
        {
            if (light.lightType == Light2D.LightType.Global)
                continue;
            bool red = light.name.StartsWith("Red");
            bool portal = light.name.Contains("Portal");
            if (!portal && !(red && light.transform.position.x >= DamagedFromX))
                continue;
            LightPulse pulse = light.GetComponent<LightPulse>();
            if (pulse == null)
                pulse = light.gameObject.AddComponent<LightPulse>();
            var so = new SerializedObject(pulse);
            so.FindProperty("mode").enumValueIndex = portal ? 0 : 1;
            so.FindProperty("speed").floatValue = portal ? 0.7f : 9f;
            so.FindProperty("minimum").floatValue = portal ? 0.55f : 0.2f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        var key = Object.FindAnyObjectByType<LevelKey>();
        if (key == null)
            return;

        // El dibujo va en un hijo: la escala del objeto con collider no se toca.
        key.GetComponent<SpriteRenderer>().enabled = false;
        Transform art = key.transform.Find("LlaveArte");
        if (art == null)
        {
            art = new GameObject("LlaveArte").AddComponent<SpriteRenderer>().transform;
            art.SetParent(key.transform, false);
        }
        Vector3 world = key.transform.lossyScale;
        art.localScale = new Vector3(1.1f / Mathf.Max(world.x, 0.01f), 1.1f / Mathf.Max(world.y, 0.01f), 1f);
        var artRenderer = art.GetComponent<SpriteRenderer>();
        artRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Env}/Llave.png");
        artRenderer.sortingOrder = key.GetComponent<SpriteRenderer>().sortingOrder;
        artRenderer.sharedMaterial = key.GetComponent<SpriteRenderer>().sharedMaterial;

        Transform glow = key.transform.Find("LlaveLuz");
        if (glow == null)
        {
            var light = new GameObject("LlaveLuz").AddComponent<Light2D>();
            light.transform.SetParent(key.transform, false);
            light.lightType = Light2D.LightType.Point;
            light.pointLightOuterRadius = 2.2f;
            light.pointLightInnerRadius = 0.2f;
            light.color = new Color(1f, 0.82f, 0.3f);
            light.intensity = 1.1f;
            var pulse = light.gameObject.AddComponent<LightPulse>();
            var so = new SerializedObject(pulse);
            so.FindProperty("speed").floatValue = 1.2f;
            so.FindProperty("minimum").floatValue = 0.5f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static Material ParticleMaterial()
    {
        string path = $"{Fx}/Particula.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Env}/Particula.png");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Tilemap FindMap(string name)
    {
        foreach (Tilemap map in Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include))
        {
            if (map.name == name)
                return map;
        }
        return null;
    }

    private static Tile Load(string name) => AssetDatabase.LoadAssetAtPath<Tile>($"{Tiles}/{name}.asset");

    private static void EnsureTileAsset(string name)
    {
        string path = $"{Tiles}/{name}.asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }
        tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Env}/{name}.png");
        tile.colliderType = Tile.ColliderType.Grid;
        EditorUtility.SetDirty(tile);
    }

    // ---------- Dibujos ----------

    private static Color[] DrawTile(Color baseColor, bool top, int variant, bool damaged)
    {
        const int s = 16;
        var px = new Color[s * s];
        var random = new System.Random(101 + variant * 7 + (damaged ? 50 : 0) + (top ? 500 : 0));
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float shade = 1f + (float)(random.NextDouble() - 0.5) * 0.05f;
                if (y >= s - 1 || x == 0) shade += 0.22f;
                if (y == 0 || x == s - 1) shade -= 0.32f;
                px[y * s + x] = Tint(baseColor, shade);
            }
        }

        foreach (Vector2Int rivet in new[] { new Vector2Int(2, 2), new Vector2Int(13, 2), new Vector2Int(2, 13), new Vector2Int(13, 13) })
        {
            Put(px, s, rivet.x, rivet.y, Tint(baseColor, 1.55f));
            Put(px, s, rivet.x + 1, rivet.y - 1, Tint(baseColor, 0.6f));
        }

        if (variant == 1)
        {
            for (int x = 1; x < s - 1; x++)
                Put(px, s, x, 8, Tint(baseColor, 0.72f));
        }
        else if (variant == 2)
        {
            for (int i = 0; i < 6; i++)
                Put(px, s, 5 + i, 10 - i, Tint(baseColor, 1.25f));
        }

        if (top)
        {
            Color glow = damaged ? new Color(0.72f, 0.42f, 0.32f) : new Color(0.62f, 0.86f, 0.95f);
            for (int x = 0; x < s; x++)
            {
                Put(px, s, x, s - 1, glow);
                Put(px, s, x, s - 2, Color.Lerp(glow, baseColor, 0.6f));
                Put(px, s, x, s - 3, Tint(baseColor, 0.65f));
            }
        }

        if (damaged)
        {
            for (int i = 0; i < 12; i++)
                Put(px, s, random.Next(s), random.Next(s - (top ? 3 : 0)), new Color(0.58f, 0.3f, 0.16f));
            if (variant == 1)
            {
                int[,] crack = { { 3, 15 }, { 4, 12 }, { 7, 10 }, { 6, 7 }, { 10, 5 }, { 9, 1 } };
                for (int i = 0; i < crack.GetLength(0); i++)
                    Put(px, s, crack[i, 0], crack[i, 1], new Color(0.08f, 0.06f, 0.07f));
            }
            if (variant == 2)
            {
                for (int x = 3; x < 12; x++)
                    Put(px, s, x, 4, x % 2 == 0 ? new Color(0.9f, 0.15f, 0.3f) : new Color(0.2f, 0.85f, 0.9f));
            }
        }
        return px;
    }

    private static Color[] DrawBrick()
    {
        const int s = 16;
        var px = new Color[s * s];
        var random = new System.Random(7);
        Color brick = new Color(0.52f, 0.25f, 0.2f);
        Color mortar = new Color(0.2f, 0.12f, 0.11f);
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                bool rowLine = y % 8 == 0;
                bool columnLine = (y / 8) % 2 == 0 ? x % 8 == 0 : (x + 4) % 8 == 0;
                float shade = 1f + (float)(random.NextDouble() - 0.5) * 0.14f;
                if (y % 8 == 7) shade += 0.25f;
                px[y * s + x] = rowLine || columnLine ? mortar : Tint(brick, shade);
            }
        }
        return px;
    }

    private static Color[] DrawRacks(bool broken)
    {
        const int s = 128;
        var px = new Color[s * s];
        var random = new System.Random(broken ? 23 : 11);
        Color rack = broken ? new Color(0.12f, 0.07f, 0.08f, 1f) : new Color(0.07f, 0.1f, 0.14f, 1f);
        Color led = broken ? new Color(0.95f, 0.18f, 0.15f, 1f) : new Color(0.25f, 0.85f, 0.95f, 1f);
        for (int column = 0; column < 4; column++)
        {
            int left = column * 32 + 2;
            bool missing = broken && column == 2;
            for (int y = 8; y < s - 8; y++)
            {
                for (int x = left; x < left + 28; x++)
                {
                    if (missing && y > 70)
                        continue;
                    float shade = (y % 12 == 0) ? 0.55f : 1f;
                    px[y * s + x] = Tint(rack, shade);
                }
                if (y % 12 == 6 && !(missing && y > 70))
                {
                    for (int l = 0; l < 5; l++)
                    {
                        if (random.NextDouble() < (broken ? 0.3 : 0.6))
                            Put(px, s, left + 3 + l * 4, y, led);
                    }
                }
            }
        }
        return px;
    }

    private static Color[] DrawCables(bool broken)
    {
        const int s = 128;
        var px = new Color[s * s];
        Color cable = broken ? new Color(0.22f, 0.08f, 0.09f, 1f) : new Color(0.1f, 0.16f, 0.2f, 1f);
        for (int wire = 0; wire < 3; wire++)
        {
            int height = 100 - wire * 16;
            float sag = 8f + wire * 5f;
            for (int x = 0; x < s; x++)
            {
                if (broken && wire == 1 && x > 48 && x < 84)
                    continue;
                float t = (float)x / s;
                int y = height - Mathf.RoundToInt(Mathf.Sin(t * Mathf.PI) * sag);
                Put(px, s, x, y, cable);
                Put(px, s, x, y + 1, Tint(cable, 1.4f));
            }
        }
        Color panel = broken ? new Color(0.17f, 0.08f, 0.09f, 1f) : new Color(0.1f, 0.13f, 0.17f, 1f);
        for (int y = 10; y < 34; y++)
        {
            for (int x = 20; x < 56; x++)
            {
                bool edge = y == 10 || y == 33 || x == 20 || x == 55;
                Put(px, s, x, y, edge ? Tint(panel, 1.6f) : panel);
            }
        }
        if (broken)
        {
            for (int i = 0; i < 26; i++)
                Put(px, s, 24 + i, 16 + i % 13, new Color(0.9f, 0.2f, 0.2f, 1f));
        }
        return px;
    }

    private static Color[] DrawKey()
    {
        const int s = 32;
        var px = new Color[s * s];
        Color gold = new Color(1f, 0.82f, 0.25f);
        Color dark = new Color(0.55f, 0.36f, 0.08f);
        Color shine = new Color(1f, 0.96f, 0.68f);
        Vector2 center = new Vector2(9.5f, 16f);
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance < 7.6f && distance > 3f)
                    Put(px, s, x, y, distance > 6.4f || distance < 4f ? dark : gold);
            }
        }
        for (int x = 16; x < 29; x++)
        {
            for (int y = 14; y < 19; y++)
                Put(px, s, x, y, y == 14 || y == 18 ? dark : gold);
            Put(px, s, x, 17, shine);
        }
        foreach (int tooth in new[] { 20, 25 })
        {
            for (int x = tooth; x < tooth + 3; x++)
            {
                for (int y = 8; y < 14; y++)
                    Put(px, s, x, y, y == 8 || x == tooth + 2 ? dark : gold);
            }
        }
        Put(px, s, 6, 20, shine);
        Put(px, s, 7, 21, shine);
        return px;
    }

    private static Color[] Solid(int s)
    {
        var px = new Color[s * s];
        for (int i = 0; i < px.Length; i++)
            px[i] = Color.white;
        return px;
    }

    private static Color Tint(Color color, float shade) => new Color(
        Mathf.Clamp01(color.r * shade), Mathf.Clamp01(color.g * shade), Mathf.Clamp01(color.b * shade), color.a);

    private static void Put(Color[] pixels, int size, int x, int y, Color color)
    {
        if (x >= 0 && y >= 0 && x < size && y < size)
            pixels[y * size + x] = color;
    }

    // ---------- Guardado ----------

    private static void SaveTile(string name, Color[] pixels) => SaveSprite($"{name}.png", pixels, 16, 32, false);

    private static void SaveSprite(string fileName, Color[] pixels, int size, float ppu, bool repeat)
    {
        string path = $"{Env}/{fileName}";
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.SetPixels(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }
}
