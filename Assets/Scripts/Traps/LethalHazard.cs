using UnityEngine;

// Mata al jugador al tocarlo: pinchos, zona de caída al vacío, checkpoint falso...
// Funciona con colliders sólidos y con triggers.
public class LethalHazard : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other) => TryKill(other);
    private void OnCollisionEnter2D(Collision2D collision) => TryKill(collision.collider);

    private static void TryKill(Collider2D other)
    {
        Rigidbody2D rb = other.attachedRigidbody;
        if (rb != null && rb.TryGetComponent(out PlayerDeath player))
            player.Die();
    }
}
