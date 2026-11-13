using UnityEngine;

// Zona que hace hablar al Sistema cuando el jugador entra.
[RequireComponent(typeof(Collider2D))]
public class NarratorTrigger : MonoBehaviour
{
    [TextArea(1, 3)]
    [SerializeField] private string line;
    [Tooltip("Si está activo, vuelve a hablar cada vez que el jugador entra.")]
    [SerializeField] private bool repeat = false;
    [Tooltip("Opcional: frases encadenadas. Si hay alguna, se dicen en lugar de la frase simple.")]
    [SerializeField] private NarratorLine[] sequence;
    [Tooltip("La secuencia solo suena entera la primera vez; después se usa la frase simple, que puede estar vacía.")]
    [SerializeField] private bool fullOnlyFirstTime = false;

    [Tooltip("Opcional: la voz del Sistema para esta frase. Suena cuando el jugador entra.")]
    [SerializeField] private AudioClip voiceClip;

    private bool said;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((said && !repeat) || !other.CompareTag("Player"))
            return;

        bool firstTime = !said;
        said = true;
        if (sequence != null && sequence.Length > 0 && (firstTime || !fullOnlyFirstTime))
            Narrator.SaySequence(sequence);
        else
            Narrator.Say(line, voiceClip);
    }
}
