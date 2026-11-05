using UnityEngine;

// Lista de frases del Sistema guardada como asset en Data/Narrator (datos fuera del código, como pide el GDD).
[CreateAssetMenu(menuName = "Unknown Game/Narrator Lines", fileName = "NarratorLines")]
public class NarratorLines : ScriptableObject
{
    [TextArea(1, 3)] public string[] lines;

    public string GetRandom()
    {
        if (lines == null || lines.Length == 0)
            return null;
        return lines[Random.Range(0, lines.Length)];
    }
}
