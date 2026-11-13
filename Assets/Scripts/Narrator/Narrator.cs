using System;
using UnityEngine;

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
    //la voz audioclip es opcional
    public static event Action<string, AudioClip> Said;
    
    public static event Action<NarratorLine[]> SequenceSaid;
    public static event Action<string, AudioClip> SaidIfIdle;

    public static void Say(string line, AudioClip voice = null) => Said?.Invoke(line, voice);
    // Para frases prescindibles, como las burlas: si el Sistema ya está hablando, se descartan.
    public static void SayIfIdle(string line, AudioClip voice = null) => SaidIfIdle?.Invoke(line, voice);
    public static void SaySequence(NarratorLine[] lines) => SequenceSaid?.Invoke(lines);
}
