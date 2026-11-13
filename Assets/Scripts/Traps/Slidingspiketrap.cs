using System.Collections;
using UnityEngine;

// Trampa troll (Nivel 2): unos pinchos fijos que,
// se deslizan por el suelo hacia. Hay que saltarlos. Al reaparecer vuelven a su sitio.

// Va en una zona invisible (BoxCollider2D trigger) que cubre la plataforma.
[RequireComponent(typeof(BoxCollider2D))]
public class SlidingSpikeTrap : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Los pinchos que se mueven")]
    [SerializeField] private Transform spikes;

    [Header("Movimiento")]
    [Tooltip("Casillas que recorren los pinchos.")]
    [SerializeField, Min(0.1f)] private float distance = 5f;
    [Tooltip("Casillas por segundo.")]
    [SerializeField, Min(0.1f)] private float speed = 4f;
    [Tooltip("Segundos que tiemblan antes de arrancar: el aviso.")]
    [SerializeField, Min(0f)] private float warningTime = 0.25f;
    [SerializeField, Min(0f)] private float shakeAmplitude = 0.05f;

    [Header("Audio")]
    [SerializeField] private AudioClip startClip;

    private Vector3 startPosition;
    private Coroutine slideRoutine;
    private bool triggered;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        if (spikes == null)
        {
            Debug.LogError($"{name}: falta asignar Spikes en SlidingSpikeTrap.", this);
            enabled = false;
            return;
        }
        startPosition = spikes.position;
    }

    // Mecánica 04: al reaparecer, los pinchos vuelven a su sitio.
    private void OnEnable() => RespawnManager.PlayerRespawned += ResetTrap;
    private void OnDisable() => RespawnManager.PlayerRespawned -= ResetTrap;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!enabled || triggered || other.attachedRigidbody == null || !other.attachedRigidbody.CompareTag("Player"))
            return;

        triggered = true;
        // Se mueven hacia el lado donde está Emmet.
        float direction = Mathf.Sign(other.attachedRigidbody.position.x - startPosition.x);
        slideRoutine = StartCoroutine(Slide(direction));
    }

    private IEnumerator Slide(float direction)
    {
        float elapsed = 0f;
        while (elapsed < warningTime)
        {
            elapsed += Time.deltaTime;
            spikes.position = startPosition + (Vector3)(Random.insideUnitCircle * shakeAmplitude);
            yield return null;
        }

        SfxPlayer.Play(startClip);
        Vector3 target = startPosition + Vector3.right * direction * distance;
        spikes.position = startPosition;
        while ((spikes.position - target).sqrMagnitude > 0.0001f)
        {
            spikes.position = Vector3.MoveTowards(spikes.position, target, speed * Time.deltaTime);
            yield return null;
        }
        slideRoutine = null;
    }

    private void ResetTrap()
    {
        if (spikes == null)
            return;
        if (slideRoutine != null)
        {
            StopCoroutine(slideRoutine);
            slideRoutine = null;
        }
        spikes.position = startPosition;
        triggered = false;
    }

    // En la vista Scene: zona que la activa (naranja) y recorrido de los pinchos (rojo, hacia ambos lados).
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
        if (spikes != null)
        {
            Vector3 from = Application.isPlaying ? startPosition : spikes.position;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(from + Vector3.left * distance, from + Vector3.right * distance);
        }
    }
}