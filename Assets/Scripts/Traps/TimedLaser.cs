using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class TimedLaser : MonoBehaviour
{
    [SerializeField] private PlayerDeath player;
    [SerializeField] private SpriteRenderer beam;
    [SerializeField] private SpriteRenderer core;
    [SerializeField, Min(0.02f)] private float activeTime = 1.2f;
    [SerializeField, Min(0.02f)] private float offTime = 0.4f;
    [SerializeField, Min(0f)] private float phaseOffset;
    [SerializeField, Min(0f)] private float openingWarning = 0.2f;
    [SerializeField, Min(0f)] private float closingWarning = 0.1f;

    private BoxCollider2D area;
    private Collider2D playerCollider;
    private int phaseStep;
    private int activeSteps;
    private int cycleSteps;
    private int offsetSteps;
    private int openingWarningSteps;
    private int closingWarningSteps;

    public bool IsActive => phaseStep < activeSteps;

    private void Awake()
    {
        area = GetComponent<BoxCollider2D>();
        if (player == null || !player.TryGetComponent(out playerCollider) || beam == null || core == null)
        {
            Debug.LogError("El láser necesita referencias al jugador y a sus dos renderizadores.", this);
            enabled = false;
            return;
        }

        // Los pasos enteros mantienen alineadas las ventanas de todos los láseres.
        float step = Time.fixedDeltaTime;
        activeSteps = Mathf.Max(1, Mathf.RoundToInt(activeTime / step));
        int offSteps = Mathf.Max(1, Mathf.RoundToInt(offTime / step));
        cycleSteps = activeSteps + offSteps;
        offsetSteps = Mathf.RoundToInt(phaseOffset / step) % cycleSteps;
        openingWarningSteps = Mathf.RoundToInt(openingWarning / step);
        closingWarningSteps = Mathf.RoundToInt(closingWarning / step);
    }

    private void OnEnable()
    {
        ResetPhase();
        RespawnManager.PlayerRespawned += ResetPhase;
    }

    private void OnDisable()
    {
        RespawnManager.PlayerRespawned -= ResetPhase;
        if (beam != null) beam.enabled = false;
        if (core != null) core.enabled = false;
    }

    private void FixedUpdate()
    {
        phaseStep = (phaseStep + 1) % cycleSteps;
        UpdateVisuals();

        // Comprueba también a quien ya estaba dentro cuando volvió a encenderse.
        if (IsActive && !player.IsDead && area.Distance(playerCollider).isOverlapped)
            player.Die();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isActiveAndEnabled && IsActive && other == playerCollider)
            player.Die();
    }

    private void ResetPhase()
    {
        phaseStep = offsetSteps;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        bool active = IsActive;
        bool opening = active && activeSteps - phaseStep <= openingWarningSteps;
        bool closing = !active && cycleSteps - phaseStep <= closingWarningSteps;
        beam.enabled = active;
        beam.color = opening ? new Color(0.14f, 0.52f, 0.9f, 0.49f) : new Color(0.14f, 0.58f, 0.95f, 0.72f);
        core.enabled = active || closing;
        core.color = closing ? new Color(1f, 0.72f, 0.34f, 0.85f)
            : opening ? new Color(0.53f, 0.82f, 1f) : new Color(0.83f, 0.96f, 1f);
    }
}
