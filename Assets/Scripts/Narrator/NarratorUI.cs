using System.Collections;
using TMPro;
using UnityEngine;

// Muestra lo que dice el Sistema letra a letra, como una terminal vieja, y lo oculta pasado un tiempo.
public class NarratorUI : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField, Min(1f)] private float charactersPerSecond = 40f;
    [SerializeField, Min(0f)] private float holdSeconds = 2.5f;

    private Coroutine showRoutine;
    private bool playingSequence;

    private void Awake()
    {
        messageText.text = string.Empty;
    }

    private void OnEnable()
    {
        Narrator.Said += Show;
        Narrator.SequenceSaid += ShowSequence;
    }

    private void OnDisable()
    {
        Narrator.Said -= Show;
        Narrator.SequenceSaid -= ShowSequence;
    }

    private void Show(string line)
    {
        // Una frase suelta (por ejemplo, una burla al morir) no corta una secuencia: se descarta.
        if (string.IsNullOrEmpty(line) || playingSequence)
            return;

        Restart(ShowRoutine(line));
    }

    private void ShowSequence(NarratorLine[] lines)
    {
        Restart(SequenceRoutine(lines));
    }

    private void Restart(IEnumerator routine)
    {
        if (showRoutine != null)
            StopCoroutine(showRoutine);
        showRoutine = StartCoroutine(routine);
    }

    private IEnumerator ShowRoutine(string line)
    {
        yield return TypeLine(line);
        yield return new WaitForSeconds(holdSeconds);
        messageText.text = string.Empty;
        showRoutine = null;
    }

    private IEnumerator SequenceRoutine(NarratorLine[] lines)
    {
        playingSequence = true;
        foreach (NarratorLine line in lines)
        {
            messageText.text = string.Empty;
            if (!string.IsNullOrEmpty(line.text))
                yield return TypeLine(line.text);
            yield return new WaitForSeconds(line.seconds);
        }
        messageText.text = string.Empty;
        playingSequence = false;
        showRoutine = null;
    }

    private IEnumerator TypeLine(string line)
    {
        messageText.text = line;
        for (int i = 0; i <= line.Length; i++)
        {
            messageText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(1f / charactersPerSecond);
        }
    }
}
