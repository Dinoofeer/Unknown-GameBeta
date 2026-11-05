using UnityEngine;

// Mecánica 03 (GDD): bloque invisible troll.
// Oculto y sin colisión hasta que el jugador lo golpea desde abajo; entonces aparece,
// se vuelve sólido y corta la subida del jugador. Sigue visible el resto del intento.
public class InvisibleBlock : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Collider sólido del bloque. Se desactiva al empezar.")]
    [SerializeField] private BoxCollider2D solidCollider;
    [Tooltip("Trigger fino en la parte inferior del bloque que detecta el golpe desde abajo.")]
    [SerializeField] private BoxCollider2D detector;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Reaparición (Mecánica 04)")]
    [Tooltip("Si está activo, el bloque sigue visible después de que el jugador muera.")]
    [SerializeField] private bool stayRevealedAfterDeath = false;

    private bool revealed;

    private void Awake()
    {
        if (solidCollider == null || detector == null || spriteRenderer == null)
        {
            Debug.LogError($"{name}: faltan referencias en InvisibleBlock.", this);
            enabled = false;
            return;
        }
        Hide();
    }

    private void OnEnable() => RespawnManager.PlayerRespawned += ResetAfterDeath;
    private void OnDisable() => RespawnManager.PlayerRespawned -= ResetAfterDeath;

    // Stay además de Enter: si el jugador entra al área de lado y luego sube, también se detecta.
    private void OnTriggerEnter2D(Collider2D other) => TryReveal(other);
    private void OnTriggerStay2D(Collider2D other) => TryReveal(other);

    private void TryReveal(Collider2D other)
    {
        if (revealed || !other.CompareTag("Player"))
            return;

        Rigidbody2D playerRb = other.attachedRigidbody;
        // GDD: solo se activa al golpearlo desde abajo: el jugador sube
        // y su cabeza no ha pasado la parte superior del detector.
        if (playerRb == null || playerRb.linearVelocity.y <= 0f)
            return;
        if (other.bounds.max.y > detector.bounds.max.y)
            return;

        Reveal(other, playerRb);
    }

    private void Reveal(Collider2D playerCollider, Rigidbody2D playerRb)
    {
        revealed = true;
        spriteRenderer.enabled = true;
        solidCollider.enabled = true;
        detector.enabled = false;

        // Si en este paso de física la cabeza ya entró en el bloque, se baja justo hasta su borde
        // para que Unity no lo expulse hacia un lado.
        float blockBottom = transform.position.y
            + (solidCollider.offset.y - solidCollider.size.y * 0.5f) * transform.lossyScale.y;
        float overlap = playerCollider.bounds.max.y - blockBottom;
        if (overlap > 0f)
            playerRb.position += Vector2.down * overlap;

        // GDD: detiene de inmediato la inercia ascendente (el doble salto sigue disponible).
        playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 0f);
    }

    // Se llama al reaparecer el jugador (RespawnManager.PlayerRespawned).
    public void ResetAfterDeath()
    {
        if (!stayRevealedAfterDeath)
            Hide();
    }

    private void Hide()
    {
        revealed = false;
        spriteRenderer.enabled = false;
        solidCollider.enabled = false;
        detector.enabled = true;
    }

    // Solo en el editor: dibuja el contorno para que el diseñador vea dónde está el bloque.
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.8f, 0.8f);
        Gizmos.DrawWireCube(transform.position, transform.lossyScale);
    }
}
