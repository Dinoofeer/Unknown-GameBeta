using System.Collections;
using UnityEngine;

// Trampa troll "la puerta que huye" (Nivel 2).
// Va en una zona invisible (BoxCollider2D trigger) delante de la puerta. Cuando Emmet entra,
// la puerta se desplaza a otro sitio. Al reaparecer, vuelve a su lugar original.
// La puerta sigue funcionando igual (GoalDoor): solo cambia de posición.
[RequireComponent(typeof(BoxCollider2D))]
public class FleeingDoor : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Objeto que se mueve: el padre de la puerta (\"Puerta del Nucleo\").")]
    [SerializeField] private Transform door;
    [Tooltip("Opcional: si se asigna, la puerta solo huye cuando Emmet YA lleva esta llave.")]
    [SerializeField] private LevelKey requiredKey;
    [Tooltip("Opcional: objeto que aparece cuando la puerta huye (por ejemplo, la cornisa). Empieza oculto.")]
    [SerializeField] private GameObject appearOnFlee;

    [Header("Huida")]
    [Tooltip("Hacia dónde se mueve, en casillas. (0, 4) = 4 casillas hacia arriba.")]
    [SerializeField] private Vector2 fleeOffset = new Vector2(0f, 4f);
    [Tooltip("Segundos que tarda en llegar a su nuevo lugar.")]
    [SerializeField, Min(0.01f)] private float fleeTime = 0.25f;
    [Tooltip("Segundos de temblor antes de huir: el aviso para el jugador.")]
    [SerializeField, Min(0f)] private float shakeTime = 0.15f;
    [SerializeField, Min(0f)] private float shakeAmplitude = 0.06f;

    [Header("Extras")]
    [Tooltip("Opcional: lo que dice el Sistema cuando la puerta huye. Vacío = nada.")]
    [SerializeField] private string fleeLine;
    [SerializeField] private AudioClip fleeClip;

    private Vector3 startPosition;
    private Coroutine fleeRoutine;
    private bool fled;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        if (door == null)
        {
            Debug.LogError($"{name}: falta asignar Door en FleeingDoor.", this);
            enabled = false;
            return;
        }
        startPosition = door.position;
        if (appearOnFlee != null)
            appearOnFlee.SetActive(false);
    }

    // Mecánica 04: al reaparecer, la puerta vuelve a su sitio para que la trampa se repita.
    private void OnEnable() => RespawnManager.PlayerRespawned += ResetDoor;
    private void OnDisable() => RespawnManager.PlayerRespawned -= ResetDoor;

    private void OnTriggerEnter2D(Collider2D other) => TryFlee(other);
    // Por si Emmet ya estaba dentro cuando consiguió la llave.
    private void OnTriggerStay2D(Collider2D other) => TryFlee(other);

    private void TryFlee(Collider2D other)
    {
        if (!enabled || fled || other.attachedRigidbody == null || !other.attachedRigidbody.CompareTag("Player"))
            return;
        if (requiredKey != null && !requiredKey.IsCollected)
            return;

        fled = true;
        fleeRoutine = StartCoroutine(Flee());
    }

    private IEnumerator Flee()
    {
        // Aviso: tiembla un instante.
        float elapsed = 0f;
        while (elapsed < shakeTime)
        {
            elapsed += Time.deltaTime;
            door.position = startPosition + (Vector3)(Random.insideUnitCircle * shakeAmplitude);
            yield return null;
        }

        if (appearOnFlee != null)
            appearOnFlee.SetActive(true);
        if (!string.IsNullOrEmpty(fleeLine))
            Narrator.Say(fleeLine, fleeClip);
        else
            SfxPlayer.Play(fleeClip);

        Vector3 target = startPosition + (Vector3)fleeOffset;
        elapsed = 0f;
        while (elapsed < fleeTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / fleeTime);
            door.position = Vector3.Lerp(startPosition, target, t);
            yield return null;
        }
        door.position = target;
        fleeRoutine = null;
    }

    private void ResetDoor()
    {
        if (door == null)
            return;
        if (fleeRoutine != null)
        {
            StopCoroutine(fleeRoutine);
            fleeRoutine = null;
        }
        door.position = startPosition;
        fled = false;
        if (appearOnFlee != null)
            appearOnFlee.SetActive(false);
    }

    // Dibuja en la vista Scene dónde terminará la puerta (cian) y la zona que la activa (naranja).
    private void OnDrawGizmosSelected()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.7f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.offset, box.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
        if (door != null)
        {
            Vector3 from = Application.isPlaying ? startPosition : door.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(from, from + (Vector3)fleeOffset);
            Gizmos.DrawWireCube(from + (Vector3)fleeOffset + Vector3.up, new Vector3(1f, 2f, 0f));
        }
    }
}
