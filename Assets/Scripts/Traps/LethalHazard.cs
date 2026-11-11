using UnityEngine;

// Mata al jugador al tocarlo: pinchos, zona de caída al vacío, checkpoint falso...
// Funciona con colliders sólidos y con triggers.
public class LethalHazard : MonoBehaviour
{
    [Tooltip("Actívalo en trampas que engañan al jugador (checkpoint falso, pinchos ocultos). Los pinchos visibles no lo llevan.")]
    [SerializeField] private bool trollTrap = false;

    private void OnTriggerEnter2D(Collider2D other) => TryKill(other);
    private void OnCollisionEnter2D(Collision2D collision) => TryKill(collision.collider);

    private void TryKill(Collider2D other)
    {
        Rigidbody2D rb = other.attachedRigidbody;
        if (rb != null && rb.TryGetComponent(out PlayerDeath player))
        {
            if (trollTrap && !player.IsDead)
                TrollTrap.Mark();
            player.Die();
        }
    }
}
