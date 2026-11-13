using System;
using UnityEngine;

// Una frase de una secuencia del Sistema. Con el texto vacío es solo una pausa.
[Serializable]
public struct NarratorLine
{
    public string text;
    [UnityEngine.Tooltip("Segundos que sigue en pantalla después de escribirse (o duración de la pausa).")]
    public float seconds;
    [UnityEngine.Tooltip("GDD (Interfaz Visual › Estados): ventana en estado Advertencia (borde e icono [!] en rojo oscuro). " +
                         "Solo para frases cuya función en la tabla de Diálogos es \"Advertencia\".")]
    public bool warning;
}

// Canal por el que cualquier objeto hace hablar al Sistema sin conocer la interfaz.
public static class Narrator
{
    public static event Action<string, AudioClip> Said;
    public static event Action<NarratorLine[]> SequenceSaid;
    public static event Action<string, AudioClip> SaidIfIdle;
    // GDD (Diálogos › Sala de servidores): el Sistema guarda silencio. La ventana se abre sin texto,
    // solo con el cursor, durante los segundos indicados.
    public static event Action<float> SilenceShown;
    public static void ShowSilence(float seconds) => SilenceShown?.Invoke(seconds);

    public static void Say(string line, AudioClip voice = null) => Said?.Invoke(line, voice);
    // Para frases prescindibles, como las burlas: si el Sistema ya está hablando, se descartan.
    public static void SayIfIdle(string line, AudioClip voice = null) => SaidIfIdle?.Invoke(line, voice);
    public static void SaySequence(NarratorLine[] lines) => SequenceSaid?.Invoke(lines);
}
