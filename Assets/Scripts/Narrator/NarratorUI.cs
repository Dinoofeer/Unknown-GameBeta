using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Muestra lo que dice el Sistema letra a letra, como una terminal vieja, y lo oculta pasado un tiempo.
// Las frases no se cortan: la nueva espera en cola a que termine la actual.
public class NarratorUI : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField, Min(1f)] private float charactersPerSecond = 40f;
    [SerializeField, Min(0f)] private float holdSeconds = 2.5f;
    [Tooltip("Segundos en pantalla cuando ya hay otra frase esperando, para que la cola no se retrase demasiado.")]
    [SerializeField, Min(0f)] private float holdSecondsWithQueue = 1.5f;

    private readonly Queue<IEnumerator> pending = new Queue<IEnumerator>();
    private Coroutine showRoutine;

    private void Awake()
    {
        messageText.text = string.Empty;
    }

    private void OnEnable()
    {
        Narrator.Said += Show;
        Narrator.SaidIfIdle += ShowIfIdle;
        Narrator.SequenceSaid += ShowSequence;
    }

    private void OnDisable()
    {
        Narrator.Said -= Show;
        Narrator.SaidIfIdle -= ShowIfIdle;
        Narrator.SequenceSaid -= ShowSequence;
        pending.Clear();
        showRoutine = null;
    }

    private void Show(string line)
    {
        if (!string.IsNullOrEmpty(line))
            Enqueue(ShowRoutine(line));
    }

    private void ShowIfIdle(string line)
    {
        if (!string.IsNullOrEmpty(line) && showRoutine == null)
            Enqueue(ShowRoutine(line));
    }

    private void ShowSequence(NarratorLine[] lines)
    {
        Enqueue(SequenceRoutine(lines));
    }

    private void Enqueue(IEnumerator routine)
    {
        if (showRoutine == null)
            showRoutine = StartCoroutine(routine);
        else
            pending.Enqueue(routine);
    }

    private void PlayNext()
    {
        showRoutine = pending.Count > 0 ? StartCoroutine(pending.Dequeue()) : null;
    }

    private IEnumerator ShowRoutine(string line)
    {
        yield return TypeLine(line);

        float elapsed = 0f;
        while (elapsed < (pending.Count > 0 ? holdSecondsWithQueue : holdSeconds))
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        messageText.text = string.Empty;
        PlayNext();
    }

    private IEnumerator SequenceRoutine(NarratorLine[] lines)
    {
        foreach (NarratorLine line in lines)
        {
            messageText.text = string.Empty;
            if (!string.IsNullOrEmpty(line.text))
                yield return TypeLine(line.text);
            yield return new WaitForSeconds(line.seconds);
        }
        messageText.text = string.Empty;
        PlayNext();
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
