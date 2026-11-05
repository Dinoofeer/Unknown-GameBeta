using UnityEngine;
using UnityEngine.Events;

// Mecánica 06 (GDD): llave de nivel. Al tocarla, el jugador la lleva (flota detrás de él).
// Si muere y reaparece sin haber activado un checkpoint nuevo desde que la cogió,
// la pierde y vuelve a su pedestal.
[RequireComponent(typeof(Collider2D))]
public class LevelKey : MonoBehaviour
{
    [Tooltip("Posición respecto al jugador mientras la lleva.")]
    [SerializeField] private Vector2 followOffset = new Vector2(-0.8f, 0.8f);
    [Tooltip("Rapidez con la que sigue al jugador (más alto = más pegada).")]
    [SerializeField, Min(0f)] private float followSharpness = 10f;
    [Tooltip("Diseño troll: lo que se activa al recoger la llave (enemigos, cambios del escenario...).")]
    [SerializeField] private UnityEvent onCollected;

    private Collider2D col;
    private Vector3 startPosition;
    private Transform carrier;
    private bool secured; // se activó un checkpoint después de cogerla

    public bool IsCollected => carrier != null;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        startPosition = transform.position;
    }

    private void OnEnable()
    {
        RespawnManager.PlayerRespawned += OnPlayerRespawned;
        Checkpoint.Reached += OnCheckpointReached;
    }

    private void OnDisable()
    {
        RespawnManager.PlayerRespawned -= OnPlayerRespawned;
        Checkpoint.Reached -= OnCheckpointReached;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsCollected || !other.CompareTag("Player"))
            return;

        carrier = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform;
        secured = false;
        col.enabled = false;
        onCollected?.Invoke();
    }

    private void LateUpdate()
    {
        if (!IsCollected)
            return;

        // Suavizado independiente de los FPS.
        Vector3 target = carrier.position + (Vector3)followOffset;
        float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, target, t);
    }

    private void OnCheckpointReached(Checkpoint checkpoint)
    {
        if (IsCollected)
            secured = true;
    }

    private void OnPlayerRespawned()
    {
        // GDD: si reaparece en un checkpoint anterior a donde la cogió, la llave se pierde.
        // También cubre coger la llave y morir en el mismo instante.
        if (IsCollected && !secured)
            ReturnToPedestal();
    }

    // La puerta la gasta al abrirse.
    public void Consume()
    {
        carrier = null;
        gameObject.SetActive(false);
    }

    private void ReturnToPedestal()
    {
        carrier = null;
        secured = false;
        transform.position = startPosition;
        col.enabled = true;
    }
}
