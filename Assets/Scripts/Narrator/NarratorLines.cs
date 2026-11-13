using UnityEngine;

// Lista de frases del Sistema guardada como asset en Data/Narrator (datos fuera del código, como pide el GDD).
[CreateAssetMenu(menuName = "Unknown Game/Narrator Lines", fileName = "NarratorLines")]
public class NarratorLines : ScriptableObject
{
    [TextArea(1, 3)] public string[] lines;
    [Tooltip("Opcional: la voz de cada frase, en el MISMO orden que Lines (la voz 0 es de la frase 0).")]
    public AudioClip[] voices;

    public string GetRandom() => GetRandom(out _);

    // Elige una frase al azar y devuelve también su voz (null si no tiene).
    public string GetRandom( out AudioClip voice)
    {
        voice = null;
        if (lines == null || lines.Length == 0)
            return null;
        int index = Random.Range(0, lines.Length);
        if (voices != null && index < voices.Length)
            voice = voices[index];
        return lines[index];
    }
}
