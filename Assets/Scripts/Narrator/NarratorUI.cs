using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Muestra lo que dice el Sistema letra a letra, como una terminal vieja, y lo oculta pasado un tiempo.
// Las frases no se cortan: la nueva espera en cola a que termine la actual.
public class NarratorUI : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private GameObject messageWindow;
    [SerializeField] private RectTransform cursor;
    [SerializeField, Min(1f)] private float charactersPerSecond = 40f;
    [SerializeField, Min(0f)] private float holdSeconds = 2.5f;
    [Tooltip("Segundos en pantalla cuando ya hay otra frase esperando, para que la cola no se retrase demasiado.")]
    [SerializeField, Min(0f)] private float holdSecondsWithQueue = 1.5f;

    private readonly Queue<IEnumerator> pending = new Queue<IEnumerator>();
    private Coroutine showRoutine;

    private void Awake()
    {
        SetMessage(string.Empty);
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

    private void Show(string line, AudioClip voice)
    {
        if (!string.IsNullOrEmpty(line))
            Enqueue(ShowRoutine(line, voice));
    }

    private void ShowIfIdle(string line, AudioClip voice)
    {
        if (!string.IsNullOrEmpty(line) && showRoutine == null)
            Enqueue(ShowRoutine(line, voice));
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

    private IEnumerator ShowRoutine(string line, AudioClip voice)
    {
        // La voz suena cuando la frase aparece, no cuando se pidió: si había cola, espera su turno.
        SfxPlayer.Play(voice);
        yield return TypeLine(line);

        float elapsed = 0f;
        while (elapsed < (pending.Count > 0 ? holdSecondsWithQueue : holdSeconds))
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        SetMessage(string.Empty);
        PlayNext();
    }

    private IEnumerator SequenceRoutine(NarratorLine[] lines)
    {
        foreach (NarratorLine line in lines)
        {
            SetMessage(string.Empty);
            if (!string.IsNullOrEmpty(line.text))
                yield return TypeLine(line.text);
            yield return new WaitForSeconds(line.seconds);
        }
        SetMessage(string.Empty);
        PlayNext();
    }

    private IEnumerator TypeLine(string line)
    {
        messageText.maxVisibleCharacters = 0;
        SetMessage(line);
        for (int i = 0; i <= line.Length; i++)
        {
            messageText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(1f / charactersPerSecond);
        }
    }

    private void SetMessage(string message)
    {
        messageText.text = message;
        if (messageWindow != null)
            messageWindow.SetActive(message.Length > 0);
    }

    private void LateUpdate()
    {
        if (cursor == null || !messageText.isActiveAndEnabled)
            return;

        TMP_TextInfo textInfo = messageText.textInfo;
        int last = Mathf.Min(messageText.maxVisibleCharacters, textInfo.characterCount) - 1;
        bool visible = last >= 0 && Mathf.Repeat(Time.time, 0.8f) < 0.48f;
        cursor.gameObject.SetActive(visible);
        if (!visible)
            return;

        TMP_CharacterInfo glyph = textInfo.characterInfo[last];
        float rightEdge = messageText.rectTransform.rect.xMax - cursor.rect.width;
        cursor.localPosition = new Vector3(Mathf.Min(glyph.xAdvance + 2f, rightEdge), glyph.baseLine - 2f, 0f);
    }
}
