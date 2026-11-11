using System.Collections;
using UnityEngine;

// Mecánica 07 (GDD): plataforma inestable. Al tocarla (encima, de lado o por debajo) espera un instante,
// puede temblar y cae. Vuelve intacta a su sitio cuando el jugador reaparece.
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class FallingPlatform : MonoBehaviour
{
    [Tooltip("Segundos desde el contacto hasta la caída. GDD: entre 0,2 y 0,5.")]
    [SerializeField, Range(0.2f, 0.5f)] private float fallDelay = 0.35f;
    [Tooltip("Tiembla mientras corre el tiempo. Desactívalo para la versión más troll.")]
    [SerializeField] private bool shakeBeforeFalling = true;
    [SerializeField, Min(0f)] private float shakeAmplitude = 0.05f;
    [Tooltip("Hijo con el sprite. Es lo que tiembla, para no mover el collider.")]
    [SerializeField] private Transform visual;
    [SerializeField, Min(0f)] private float fallGravityScale = 3f;
    [Tooltip("Segundos de caída tras los que deja de simularse (ya está fuera de la pantalla).")]
    [SerializeField, Min(0f)] private float disableAfterFalling = 2f;
    [Header("Audio")]
    [SerializeField] private AudioClip shakeClip;
    [SerializeField] private AudioClip breakClip;

    private Rigidbody2D rb;
    private Collider2D col;
    private Vector2 startPosition;
    private Vector3 visualStartLocalPosition;
    private Coroutine fallRoutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        startPosition = rb.position;
        if (visual != null)
            visualStartLocalPosition = visual.localPosition;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void OnEnable() => RespawnManager.PlayerRespawned += ResetPlatform;
    private void OnDisable() => RespawnManager.PlayerRespawned -= ResetPlatform;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // GDD: cualquier contacto del jugador activa el temporizador, también tocar el borde de lado.
        if (fallRoutine != null || !collision.collider.CompareTag("Player"))
            return;
            SfxPlayer.Play(shakeClip);
        fallRoutine = StartCoroutine(FallAfterDelay());
    }

    private IEnumerator FallAfterDelay()
    {
        float elapsed = 0f;
        while (elapsed < fallDelay || Time.timeScale <= 0f)
        {
            if (Time.timeScale <= 0f)
            {
                yield return null;
                continue;
            }

            if (shakeBeforeFalling && visual != null)
                visual.localPosition = visualStartLocalPosition + (Vector3)(Random.insideUnitCircle * shakeAmplitude);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (visual != null)
            visual.localPosition = visualStartLocalPosition;

        // Sin collider: el jugador pierde el apoyo al instante y la plataforma cae atravesándolo todo.
        SfxPlayer.Play(breakClip);
        col.enabled = false;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = fallGravityScale;

        yield return new WaitForSeconds(disableAfterFalling);
        rb.simulated = false;
    }

    private void ResetPlatform()
    {
        if (fallRoutine != null)
        {
            StopCoroutine(fallRoutine);
            fallRoutine = null;
        }
        rb.simulated = true;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.position = startPosition;
        transform.position = startPosition;
        col.enabled = true;
        if (visual != null)
            visual.localPosition = visualStartLocalPosition;
    }
}
