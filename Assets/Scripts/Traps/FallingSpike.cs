using System.Collections;
using UnityEngine;

// Mecánica 08 (aprobada por Jesús): pincho que cae del techo.
// Al entrar el jugador en la franja de activación tiembla y cae en vertical. Se activa una vez por vida
// y vuelve a su sitio cuando el jugador reaparece. Lo letal es LethalHazard, en el hijo que cae.
[RequireComponent(typeof(BoxCollider2D))]
public class FallingSpike : MonoBehaviour
{
    [Header("Activación")]
    [Tooltip("Distancia desde el pincho hasta el centro de la franja, hacia donde viene el jugador (-X).")]
    [SerializeField, Min(0f)] private float triggerOffsetX = 3.5f;
    [Tooltip("Tamaño de la franja. Se extiende hacia abajo desde el pincho.")]
    [SerializeField] private Vector2 triggerSize = new Vector2(1f, 6f);

    [Header("Caída")]
    [Tooltip("Segundos de temblor antes de caer: es el aviso al jugador.")]
    [SerializeField, Range(0.25f, 0.3f)] private float shakeDuration = 0.3f;
    [SerializeField, Min(0f)] private float shakeAmplitude = 0.05f;
    [SerializeField, Min(0f)] private float fallGravityScale = 3f;
    [Tooltip("Por seguridad: segundos de caída tras los que se desactiva si no ha tocado suelo.")]
    [SerializeField, Min(0f)] private float disableAfterFalling = 2f;
    [Header("Audio")]
    [SerializeField] private AudioClip alertClip;
    [SerializeField] private AudioClip impactClip; //audio

    [Header("Referencias")]
    [Tooltip("Hijo que cae, con su collider trigger y LethalHazard.")]
    [SerializeField] private Rigidbody2D spike;
    [Tooltip("Hijo con el sprite. Es lo que tiembla, para no mover el collider.")]
    [SerializeField] private Transform visual;

    private readonly Collider2D[] overlaps = new Collider2D[4];
    private Collider2D spikeCollider;
    private ContactFilter2D solidFilter;
    private Vector2 startPosition;
    private Vector3 visualStartLocalPosition;
    private Coroutine fallRoutine;

    public bool IsActivated => fallRoutine != null;

    private void Awake()
    {
        var strip = GetComponent<BoxCollider2D>();
        strip.isTrigger = true;
        ApplyStrip(strip);

        spikeCollider = spike.GetComponent<Collider2D>();
        solidFilter = new ContactFilter2D { useTriggers = false };
        startPosition = spike.position;
        visualStartLocalPosition = visual.localPosition;
        spike.bodyType = RigidbodyType2D.Kinematic;
    }

    private void OnValidate()
    {
        if (TryGetComponent(out BoxCollider2D strip))
            ApplyStrip(strip);
    }

    private void ApplyStrip(BoxCollider2D strip)
    {
        strip.size = triggerSize;
        strip.offset = new Vector2(-triggerOffsetX, -triggerSize.y * 0.5f);
    }

    private void OnEnable() => RespawnManager.PlayerRespawned += ResetSpike;
    private void OnDisable() => RespawnManager.PlayerRespawned -= ResetSpike;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (fallRoutine != null || !other.CompareTag("Player"))
            return;
        SfxPlayer.Play(alertClip);//audio
        fallRoutine = StartCoroutine(ShakeAndFall());
    }

    private IEnumerator ShakeAndFall()
    {
        float elapsed = 0f;
        while (elapsed < shakeDuration || Time.timeScale <= 0f)
        {
            if (Time.timeScale <= 0f)
            {
                yield return null;
                continue;
            }

            visual.localPosition = visualStartLocalPosition + (Vector3)(Random.insideUnitCircle * shakeAmplitude);
            elapsed += Time.deltaTime;
            yield return null;
        }
        visual.localPosition = visualStartLocalPosition;

        spike.bodyType = RigidbodyType2D.Dynamic;
        spike.gravityScale = fallGravityScale;

        float fallStart = Time.time;
        bool hitGround;
        do
        {
            yield return new WaitForFixedUpdate();
            hitGround = TouchesGround();
        }
        while (!hitGround && Time.time - fallStart < disableAfterFalling);

        if (hitGround)
            SfxPlayer.Play(impactClip); //audio

        // El suelo queda libre: no debe bloquear el camino.
        spike.gameObject.SetActive(false);
    }

    private bool TouchesGround()
    {
        int count = spikeCollider.Overlap(solidFilter, overlaps);
        for (int i = 0; i < count; i++)
        {
            if (!overlaps[i].CompareTag("Player"))
                return true;
        }
        return false;
    }

    private void ResetSpike()
    {
        if (fallRoutine != null)
        {
            StopCoroutine(fallRoutine);
            fallRoutine = null;
        }
        spike.gameObject.SetActive(true);
        spike.bodyType = RigidbodyType2D.Kinematic;
        spike.linearVelocity = Vector2.zero;
        spike.position = startPosition;
        spike.transform.position = startPosition;
        visual.localPosition = visualStartLocalPosition;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.6f);
        Vector3 center = transform.position + new Vector3(-triggerOffsetX, -triggerSize.y * 0.5f, 0f);
        Gizmos.DrawWireCube(center, triggerSize);
    }
}
