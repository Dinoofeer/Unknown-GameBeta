using System.Collections;
using TMPro;
using UnityEngine;

// GDD (Objetos y Coleccionables › Parche de Estabilidad): "corrige temporalmente los errores del entorno";
// al usarse, "efecto de código binario verde"; representación: icono brillante "sobre un nodo de datos".
// [Aprobado por Jesús] Solo existe en la sala de servidores y es su primer momento:
// 1. Al acercarse, la pantalla del nodo escribe el LEEME.TXT que dejó el autor (la Partida 0).
// 2. Al tocarlo, Emmet se detiene ~1,5 s mientras se aplica (barra de progreso y binario verde que sube).
// 3. Queda el aura: inmunidad a la corrupción de la sala (errores del entorno, no trampas).
// 4. En el cierre, el aura es lo último que se apaga antes del portal (FadeOut).
[RequireComponent(typeof(Collider2D))]
public class StabilityPatch : MonoBehaviour
{
    public static bool Active { get; private set; }

    [SerializeField] private GameObject icon;
    [Tooltip("Pantalla del nodo donde se escribe el LEEME.TXT y luego el estado.")]
    [SerializeField] private TMP_Text screenText;
    [SerializeField, TextArea(4, 10)] private string readme =
        "LEEME.TXT  -  PARCHE_ESTABILIDAD.EXE\nAUTOR: #%&#@?\n\nEL JUEGO SE ESTÁ ROMPIENDO.\nESTO CORRIGE LOS ERRORES DEL ENTORNO.\nLO HICE PARA SALIR.\nSI LEES ESTO, YO NO LO LOGRÉ.\nÚSALO TÚ.";
    [SerializeField, TextArea(2, 4)] private string appliedText = "PARCHE_ESTABILIDAD.EXE\nAUTOR: #%&#@?\n\nESTADO: APLICADO\nUSUARIO: NOVATO";
    [SerializeField, Min(1f)] private float charactersPerSecond = 32f;
    [SerializeField, Min(0f)] private float readDistance = 5f;
    [SerializeField, Min(0.1f)] private float applySeconds = 1.5f;
    [SerializeField] private AudioClip pickupClip;
    [SerializeField] private Color binaryColor = new Color32(0x6F, 0xE8, 0xC8, 0xFF);
    [SerializeField, Min(1)] private int glyphCount = 10;
    [SerializeField, Min(0.1f)] private float auraRadius = 0.75f;
    [SerializeField] private Transform playerRef;

    private Transform player;
    private TextMeshPro[] glyphs;
    private float flipTimer;
    private bool reading;
    private bool applying;
    private float applyProgress;
    private float auraAlpha = 1f;

    private void Awake()
    {
        Active = false;
        // El parche aparece cuando el LEEME.TXT termina de escribirse: primero se lee, luego se toma.
        GetComponent<Collider2D>().enabled = false;
        if (icon != null)
            icon.SetActive(false);
        if (screenText != null)
        {
            screenText.text = readme;
            screenText.maxVisibleCharacters = 0;
        }
    }

    private void Update()
    {
        if (reading || Active || playerRef == null || screenText == null)
            return;
        if (Mathf.Abs(playerRef.position.x - transform.position.x) <= readDistance)
        {
            reading = true;
            StartCoroutine(TypeReadme());
        }
    }

    private IEnumerator TypeReadme()
    {
        screenText.text = readme;
        for (int i = 0; i <= readme.Length && !applying && !Active; i++)
        {
            screenText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(1f / charactersPerSecond);
        }
        if (icon != null)
            icon.SetActive(true);
        GetComponent<Collider2D>().enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (Active || applying || !other.CompareTag("Player"))
            return;
        StartCoroutine(Apply(other.transform));
    }

    private IEnumerator Apply(Transform who)
    {
        applying = true;
        player = who;
        if (icon != null)
            icon.SetActive(false);
        SfxPlayer.Play(pickupClip);

        // Emmet se detiene mientras se aplica.
        var controls = new Behaviour[] { who.GetComponent<PlayerMovement>(), who.GetComponent<PlayerJump>() };
        foreach (Behaviour c in controls)
            if (c != null)
                c.enabled = false;
        if (who.TryGetComponent(out Rigidbody2D body))
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);

        CreateAura();
        float t = 0f;
        while (t < applySeconds)
        {
            t += Time.deltaTime;
            applyProgress = Mathf.Clamp01(t / applySeconds);
            if (screenText != null)
            {
                int pct = Mathf.RoundToInt(applyProgress * 100f);
                int bars = Mathf.RoundToInt(applyProgress * 12f);
                screenText.text = "APLICANDO PARCHE...\n\n[" + new string('#', bars) + new string('.', 12 - bars) + "] " + pct + "%";
                screenText.maxVisibleCharacters = 99999;
            }
            yield return null;
        }

        Active = true;
        applying = false;
        if (screenText != null)
            screenText.text = appliedText;
        foreach (Behaviour c in controls)
            if (c != null)
                c.enabled = true;
    }

    // Cierre de la sala: el aura se apaga la última, justo antes del portal.
    public void FadeOut(float seconds) => StartCoroutine(FadeRoutine(seconds));

    private IEnumerator FadeRoutine(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            auraAlpha = 1f - Mathf.Clamp01(t / seconds);
            yield return null;
        }
        auraAlpha = 0f;
    }

    private void CreateAura()
    {
        glyphs = new TextMeshPro[glyphCount];
        TMP_FontAsset font = screenText != null ? screenText.font : null;
        for (int i = 0; i < glyphCount; i++)
        {
            var go = new GameObject("Binario_" + i);
            go.transform.SetParent(transform, false);
            var tmp = go.AddComponent<TextMeshPro>();
            if (font != null)
                tmp.font = font;
            tmp.fontSize = 1.6f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = binaryColor;
            tmp.sortingOrder = 20;
            tmp.text = Random.value < 0.5f ? "0" : "1";
            glyphs[i] = tmp;
        }
    }

    private void LateUpdate()
    {
        if (player == null || glyphs == null)
            return;

        flipTimer -= Time.deltaTime;
        bool flip = flipTimer <= 0f;
        if (flip)
            flipTimer = 0.12f;

        Vector3 center = player.position + new Vector3(0f, 0.1f, 0f);
        for (int i = 0; i < glyphs.Length; i++)
        {
            Vector3 pos;
            if (applying)
            {
                // Mientras se aplica: columnas de binario que suben desde los pies.
                float column = (i - (glyphs.Length - 1) * 0.5f) / glyphs.Length * 2f * auraRadius;
                float rise = Mathf.Repeat(Time.time * 2.2f + i * 0.37f, 1.8f) - 0.9f;
                pos = center + new Vector3(column, rise, 0f);
            }
            else
            {
                float angle = Time.time * 1.6f + i * Mathf.PI * 2f / glyphs.Length;
                pos = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * 0.85f, 0f) * auraRadius;
            }
            glyphs[i].transform.position = pos;
            if (flip)
                glyphs[i].text = Random.value < 0.5f ? "0" : "1";
            Color c = binaryColor;
            c.a = (0.55f + 0.45f * Mathf.PerlinNoise(Time.time * 3f, i)) * auraAlpha;
            glyphs[i].color = c;
        }
    }
}
