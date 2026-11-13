using System.Collections;
using UnityEngine;

// Trampa troll (Nivel 2): una plataforma que parece segura. Cuando Emmet aterriza encima,
// unos pinchos escondidos salen de la plataforma y lo matan. Al reaparecer se vuelven a esconder.
// Va en un hijo de la plataforma: su BoxCollider2D (trigger) es la franja que detecta a Emmet encima.
[RequireComponent(typeof(BoxCollider2D))]
public class HiddenSpikeTrap : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Objeto que agrupa los pinchos (uno o varios prefabs Spikes como hijos).")]
    [SerializeField] private Transform spikes;

    [Header("Tiempos")]
    [Tooltip("Segundos entre que Emmet aterriza y salen los pinchos. 0,1 = casi imposible de esquivar.")]
    [SerializeField, Min(0f)] private float delay = 0.1f;
    [Tooltip("Segundos que tardan los pinchos en salir.")]
    [SerializeField, Min(0.01f)] private float riseTime = 0.06f;
    [Tooltip("Cuánto bajan los pinchos para esconderse dentro de la plataforma, en unidades.")]
    [SerializeField, Min(0f)] private float hiddenDepth = 0.5f;

    [Header("Dibujo")]
    [Tooltip("Orden de dibujo de los pinchos. Menor que el de la plataforma = salen desde detrás de ella.")]
    [SerializeField] private int spikeSortingOrder = -1;

    [Header("Audio")]
    [SerializeField] private AudioClip popClip;

    private SpriteRenderer[] spikeRenderers;
    private Collider2D[] spikeColliders;
    private Vector3 shownPosition;
    private Vector3 hiddenPosition;
    private Coroutine popRoutine;
    private PlayerDeath playerOnTop;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        if (spikes == null)
        {
            Debug.LogError($"{name}: falta asignar Spikes en HiddenSpikeTrap.", this);
            enabled = false;
            return;
        }

        spikeRenderers = spikes.GetComponentsInChildren<SpriteRenderer>(true);
        spikeColliders = spikes.GetComponentsInChildren<Collider2D>(true);
        foreach (SpriteRenderer r in spikeRenderers)
            r.sortingOrder = spikeSortingOrder;

        shownPosition = spikes.localPosition;
        hiddenPosition = shownPosition + Vector3.down * hiddenDepth;
        Hide();
    }

    // Mecánica 04: al reaparecer, la trampa vuelve a su estado inicial.
    private void OnEnable() => RespawnManager.PlayerRespawned += ResetTrap;
    private void OnDisable() => RespawnManager.PlayerRespawned -= ResetTrap;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!enabled || !TryGetPlayer(other, out PlayerDeath player))
            return;

        playerOnTop = player;
        if (popRoutine == null)
            popRoutine = StartCoroutine(Pop());
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (TryGetPlayer(other, out PlayerDeath player) && player == playerOnTop)
            playerOnTop = null;
    }

    private IEnumerator Pop()
    {
        yield return new WaitForSeconds(delay);   // con la pausa (timeScale 0) espera

        SfxPlayer.Play(popClip);
        SetVisible(true);

        float elapsed = 0f;
        while (elapsed < riseTime)
        {
            elapsed += Time.deltaTime;
            spikes.localPosition = Vector3.Lerp(hiddenPosition, shownPosition, elapsed / riseTime);
            yield return null;
        }
        spikes.localPosition = shownPosition;
        SetCollidersEnabled(true);

        // Si sigue encima cuando salen, muere aunque no toque un pincho concreto.
        if (playerOnTop != null && !playerOnTop.IsDead)
            playerOnTop.Die();
    }

    private void ResetTrap()
    {
        if (popRoutine != null)
        {
            StopCoroutine(popRoutine);
            popRoutine = null;
        }
        playerOnTop = null;
        Hide();
    }

    private void Hide()
    {
        spikes.localPosition = hiddenPosition;
        SetVisible(false);
        SetCollidersEnabled(false);
    }

    private void SetVisible(bool visible)
    {
        foreach (SpriteRenderer r in spikeRenderers)
            r.enabled = visible;
    }

    private void SetCollidersEnabled(bool on)
    {
        foreach (Collider2D c in spikeColliders)
            c.enabled = on;
    }

    private static bool TryGetPlayer(Collider2D other, out PlayerDeath player)
    {
        player = null;
        return other.attachedRigidbody != null && other.attachedRigidbody.TryGetComponent(out player);
    }
}
