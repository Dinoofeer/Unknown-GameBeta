using System;

// Una frase de una secuencia del Sistema. Con el texto vacío es solo una pausa.
[Serializable]
public struct NarratorLine
{
    public string text;
    [UnityEngine.Tooltip("Segundos que sigue en pantalla después de escribirse (o duración de la pausa).")]
    public float seconds;
}

// Canal por el que cualquier objeto hace hablar al Sistema sin conocer la interfaz.
public static class Narrator
{
    public static event Action<string> Said;
    public static event Action<NarratorLine[]> SequenceSaid;

    public static void Say(string line) => Said?.Invoke(line);
    public static void SaySequence(NarratorLine[] lines) => SequenceSaid?.Invoke(lines);
}
