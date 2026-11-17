using UnityEngine;

// Nivel 2 · Zona A (GDD): plataforma temporizada que aparece y desaparece sola,
// aunque el jugador no la pise. Es distinta de FallingPlatform, que cae al pisarla.
[RequireComponent(typeof(Collider2D))]
public class TimedPlatform : MonoBehaviour
{
    [Header("Ciclo")]
    [Tooltip("Segundos que la plataforma está visible y sólida.")]
    [SerializeField, Min(0.1f)] private float visibleTime = 1.5f;

    [Tooltip("Segundos que está oculta y sin colisión.")]
    [SerializeField, Min(0.1f)] private float hiddenTime = 1f;

    [Tooltip("Desfase para que varias plataformas no desaparezcan a la vez.")]
    [SerializeField, Min(0f)] private float phaseOffset;

    [Header("Aviso")]
    [Tooltip("Segundos de parpadeo antes de desaparecer. 0 = sin aviso (más troll).")]
    [SerializeField, Min(0f)] private float warningTime = 0.4f;
    
    [Tooltip("Transparencia mientras está oculta (0 = invisible).")]
    [SerializeField, Range(0f, 1f)] private float hiddenAlpha = 0f;

    [Header("Referencias")]
    [Tooltip("Sprite de la plataforma. Si está vacío, se busca en los hijos.")]
    [SerializeField] private SpriteRenderer visual;

    private Collider2D solid;
    private Color baseColor = Color.white;
    private float timer;

    private void Awake()
    {
        solid = GetComponent<Collider2D>();
        if (visual == null)
            visual = GetComponentInChildren<SpriteRenderer>();
        if (visual != null)
            baseColor = visual.color;
        ResetPhase();
    }

    // Mecánica 04: al reaparecer, todas las trampas vuelven a su estado inicial.
    private void OnEnable() => RespawnManager.PlayerRespawned += ResetPhase;
    private void OnDisable() => RespawnManager.PlayerRespawned -= ResetPhase;

    private void ResetPhase()
    {
        timer = phaseOffset;
        Apply();
    }

    private void Update()
    {
        timer += Time.deltaTime;   // con la pausa (timeScale 0) el ciclo se detiene
        Apply();
    }

    private void Apply()
    {
        float t = Mathf.Repeat(timer, visibleTime + hiddenTime);
        bool visible = t < visibleTime;
        solid.enabled = visible;

        if (visual == null)
            return;

        float alpha = visible ? 1f : hiddenAlpha;
        bool warning = visible && warningTime > 0f && t > visibleTime - warningTime;
        if (warning)
            alpha = Mathf.Repeat(t * 10f, 1f) > 0.5f ? 1f : 0.35f;

        visual.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * alpha);
    }
}
