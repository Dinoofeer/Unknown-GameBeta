using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class NarratorLineExporter
{
    [MenuItem("Tools/Exportar frases del narrador")]
    private static void Export()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("Sal del modo Play antes de exportar.");
            return;
        }

        var found = new List<string>();

        // Burlas y otras listas (assets NarratorLines)
        foreach (string guid in AssetDatabase.FindAssets("t:NarratorLines"))
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!assetPath.StartsWith("Assets/"))
                continue;

            var asset = AssetDatabase.LoadAssetAtPath<NarratorLines>(assetPath);
            if (asset != null && asset.lines != null)
                foreach (string line in asset.lines)
                    Add(found, line);
        }

        // Prefabs (solo los de Assets)
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!prefabPath.StartsWith("Assets/"))
                continue;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
                CollectFrom(prefab.GetComponentsInChildren<MonoBehaviour>(true), found);
        }

        // Escenas (solo las de Assets; se abren un momento y se cierran)
        foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith("Assets/") || path.Contains("/Archive/") || path.Contains("/Backups/"))
                continue;

            Scene scene = SceneManager.GetSceneByPath(path);
            bool openedHere = false;
            if (!scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                openedHere = true;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
                CollectFrom(root.GetComponentsInChildren<MonoBehaviour>(true), found);

            if (openedHere)
                EditorSceneManager.CloseScene(scene, true);
        }

        var sb = new StringBuilder();
        for (int i = 0; i < found.Count; i++)
            sb.AppendLine($"{i + 1:00}. {found[i]}");

        string file = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "NarratorLines_export.txt");
        File.WriteAllText(file, sb.ToString());
        Debug.Log($"Frases encontradas: {found.Count}. Guardadas en {file}\n{sb}");
        EditorUtility.RevealInFinder(file);
    }

    private static void CollectFrom(MonoBehaviour[] behaviours, List<string> found)
    {
        foreach (MonoBehaviour b in behaviours)
        {
            if (b == null)
                continue;

            if (b is NarratorTrigger)
            {
                var so = new SerializedObject(b);
                Add(found, so.FindProperty("line")?.stringValue);
                SerializedProperty seq = so.FindProperty("sequence");
                if (seq != null)
                    for (int i = 0; i < seq.arraySize; i++)
                        Add(found, seq.GetArrayElementAtIndex(i).FindPropertyRelative("text")?.stringValue);
            }
            else if (b is GoalDoor)
            {
                var so = new SerializedObject(b);
                Add(found, so.FindProperty("completedLine")?.stringValue);
            }
        }
    }

    private static void Add(List<string> found, string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;
        line = line.Trim();
        if (!found.Contains(line))
            found.Add(line);
    }
}