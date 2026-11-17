using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

// Sala de servidores (GDD: Premisa y Trama › Acto 3; Diálogos › Sala de servidores). [Aprobado por Jesús]
// - Las pantallas se encienden cuando Emmet se acerca: la sala cuenta la historia por etapas.
// - La Partida 1 muestra las muertes y el tiempo reales; la Partida 0 sigue contando sus 26 años.
// - En la evidencia final, la ventana del Sistema se abre sin texto (silencio).
// - Al cruzar el portal, la Partida 1 pasa a COMPLETADO y la sala se apaga sola de atrás hacia delante
//   (no es el Sistema: según Mundo, regla 5, no puede producir efectos en pantalla). Solo queda el portal.
public class ServerRoomDirector : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private RunStats stats;
    [Tooltip("Pantallas que se encienden al acercarse Emmet (cada una con su posición X).")]
    [SerializeField] private GameObject[] screens;
    [SerializeField, Min(0f)] private float screenWakeDistance = 6f;

    [Header("Textos dinámicos")]
    [SerializeField] private TMP_Text partida1Text;
    [SerializeField] private TMP_Text partida0Text;
    [SerializeField] private TMP_Text finalPartida1Text;
    [Tooltip("Línea pequeña del cierre: el parche también termina.")]
    [SerializeField] private TMP_Text patchUsedText;
    [SerializeField] private string patchUsedLine = "PARCHE_ESTABILIDAD: USADO 1 VEZ";
    [SerializeField] private StabilityPatch patch;
    [SerializeField, Min(0.05f)] private float auraFadeSeconds = 0.4f;

    [Tooltip("Nombre borrado de la Partida 0 (GDD, Pistas: nombre vacío o corrupto).")]
    [SerializeField] private string corruptName = "######";
    [SerializeField] private string sep = " - ";

    [Header("Silencio del Sistema")]
    [SerializeField] private float silenceAtX = 45f;
    [SerializeField, Min(0f)] private float silenceSeconds = 7f;

    [Header("Apagón")]
    [Tooltip("Lo que no se apaga: el portal (y Emmet).")]
    [SerializeField] private Transform[] keepLit;
    [SerializeField, Min(0f)] private float blackoutDelay = 0.6f;
    [SerializeField, Min(0.1f)] private float blackoutDuration = 1.5f;

    [Header("Ambiente")]
    [SerializeField] private AudioSource ambient;
    [SerializeField, Range(0f, 1f)] private float ambientVolume = 1f;
    private float silenceUntil = -1f;

    private readonly HashSet<GameObject> awake = new HashSet<GameObject>();
    private bool silenceDone;
    private bool completed;

    private void OnEnable() => GoalDoor.LevelCompleted += OnCompleted;
    private void OnDisable() => GoalDoor.LevelCompleted -= OnCompleted;

    private void Start()
    {
        foreach (GameObject screen in screens)
            SetBrightness(screen, 0.12f);
    }

    private void Update()
    {
        if (player == null)
            return;

        float x = player.position.x;
        foreach (GameObject screen in screens)
        {
            if (screen == null || awake.Contains(screen))
                continue;
            if (x >= screen.transform.position.x - screenWakeDistance)
            {
                awake.Add(screen);
                SetBrightness(screen, 1f);
            }
        }

        if (!silenceDone && x >= silenceAtX)
        {
            silenceDone = true;
            Narrator.ShowSilence(silenceSeconds);
            silenceUntil = Time.time + silenceSeconds;
        }

        // Ambiente de la sala (audio de Jesús): volumen de Música de la Configuración,
        // baja al 30 % durante el silencio del Sistema y se apaga con el apagón.
        if (ambient != null)
        {
            float target = completed ? 0f : ambientVolume * GameSettings.Music * (Time.time < silenceUntil ? 0.3f : 1f);
            ambient.volume = Mathf.MoveTowards(ambient.volume, target, Time.deltaTime * (completed ? 0.6f : 1.5f));
        }

        if (partida1Text != null && stats != null && !completed)
        {
            float t = stats.ElapsedTime;
            partida1Text.text = "REGISTRO DE PARTIDAS\nPARTIDA 1" + sep + "NOVATO\nMUERTES: " + stats.Deaths +
                                $"\nTIEMPO: {(int)(t / 60f):00}:{(int)(t % 60f):00}\nESTADO: EN CURSO";
        }

        if (partida0Text != null)
        {
            int s = (int)Time.timeSinceLevelLoad;
            partida0Text.text = "PARTIDA 0" + sep + corruptName + "\nMUERTES: 99 999+\n" +
                                $"TIEMPO: 26 AÑOS {s / 3600:00}:{s / 60 % 60:00}:{s % 60:00}\nÚLTIMO PUNTO: NIVEL 3";
        }
    }

    private void OnCompleted()
    {
        if (completed)
            return;
        completed = true;
        if (finalPartida1Text != null)
            finalPartida1Text.text = "PARTIDA 1" + sep + "NOVATO\nCOMPLETADO";
        if (patchUsedText != null)
            patchUsedText.text = patchUsedLine;
        if (player != null)
        {
            foreach (var control in new Behaviour[] { player.GetComponent<PlayerMovement>(), player.GetComponent<PlayerJump>() })
                if (control != null)
                    control.enabled = false;
            if (player.TryGetComponent(out Rigidbody2D body))
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        }
        StartCoroutine(Blackout());
    }

    private IEnumerator Blackout()
    {
        yield return new WaitForSeconds(blackoutDelay);

        // Todo lo visible de la escena, de atrás (entrada) hacia delante (portal).
        var items = new List<(float x, Object target)>();
        foreach (SpriteRenderer sr in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude))
            if (!IsKeptLit(sr.transform))
                items.Add((sr.bounds.center.x, sr));
        foreach (TMP_Text tx in FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude))
            if (!IsKeptLit(tx.transform) && !(tx is TextMeshProUGUI) && tx != patchUsedText)
                items.Add((tx.transform.position.x, tx));
        foreach (Tilemap tm in FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude))
            items.Add((float.MinValue, tm));
        items.Sort((a, b) => a.x.CompareTo(b.x));

        float start = Time.time;
        int next = 0;
        while (next < items.Count)
        {
            float progress = (Time.time - start) / blackoutDuration;
            int target = Mathf.Min(items.Count, Mathf.CeilToInt(progress * items.Count));
            for (; next < target; next++)
                Darken(items[next].target);
            yield return null;
        }

        // El aura del parche es la última luz antes del portal.
        if (patch != null)
            patch.FadeOut(auraFadeSeconds);
        if (patchUsedText != null)
        {
            float t = 0f;
            while (t < auraFadeSeconds)
            {
                t += Time.deltaTime;
                patchUsedText.alpha = 1f - Mathf.Clamp01(t / auraFadeSeconds);
                yield return null;
            }
        }
    }

    private bool IsKeptLit(Transform t)
    {
        if (player != null && t.IsChildOf(player))
            return true;
        if (t.GetComponentInParent<StabilityPatch>() != null && t.GetComponent<TextMeshPro>() != null && t.name.StartsWith("Binario"))
            return true;
        foreach (Transform keep in keepLit)
            if (keep != null && t.IsChildOf(keep))
                return true;
        return false;
    }

    private static void Darken(Object target)
    {
        Color off = new Color(0.04f, 0.04f, 0.05f, 1f);
        switch (target)
        {
            case SpriteRenderer sr: sr.color = new Color(off.r, off.g, off.b, sr.color.a); break;
            case TMP_Text tx: tx.color = new Color(off.r, off.g, off.b, 0f); break;
            case Tilemap tm: tm.color = off; break;
        }
    }

    private static void SetBrightness(GameObject screen, float k)
    {
        if (screen == null)
            return;
        foreach (SpriteRenderer sr in screen.GetComponentsInChildren<SpriteRenderer>(true))
        {
            Color c = sr.color;
            c.a = Mathf.Clamp01(k < 1f ? 0.25f : 1f);
            sr.color = c;
        }
        foreach (TMP_Text tx in screen.GetComponentsInChildren<TMP_Text>(true))
            tx.alpha = k;
    }
}
