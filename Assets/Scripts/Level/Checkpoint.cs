using System;
using UnityEngine;

// Mecánica 05 (GDD): punto de control. Al tocarlo pasa a ser el punto de reaparición.
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    public static event Action<Checkpoint> Reached;

    [Tooltip("Identificador estable para guardar progreso. Único dentro del nivel.")]
    [SerializeField] private string persistentId;

    [Tooltip("Dónde reaparece el jugador, relativo al checkpoint.")]
    [SerializeField] private Vector2 spawnOffset = Vector2.zero;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color activeColor = Color.green;

    private bool used;

    public Vector2 SpawnPosition => (Vector2)transform.position + spawnOffset;
    public string PersistentId => persistentId;

    public void RestoreState(bool reached, bool active)
    {
        used = reached;
        SetActiveVisual(active);
    }

    private void Awake()
    {
        SetActiveVisual(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Cada checkpoint se activa una sola vez: volver atrás no retrocede el progreso.
        if (used || !other.CompareTag("Player"))
            return;

        used = true;
        Reached?.Invoke(this);
    }

    public void SetActiveVisual(bool active)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = active ? activeColor : inactiveColor;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(SpawnPosition, 0.25f);
    }
}
