using UnityEngine;

// Nivel 2 · Zonas de glitch (GDD): "zonas con velocidad alterada".
// Mientras el jugador está dentro, su velocidad horizontal se multiplica.
// Necesita la propiedad SpeedMultiplier de PlayerMovement.
[RequireComponent(typeof(Collider2D))]
public class SpeedZone : MonoBehaviour
{
    [Tooltip("0,5 = mitad de velocidad · 1,6 = más rápido.")]
    [SerializeField, Range(0.1f, 3f)] private float speedMultiplier = 0.5f;

    private void Reset() => GetComponent<Collider2D>().isTrigger = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (TryGetMovement(other, out PlayerMovement movement))
            movement.SpeedMultiplier = speedMultiplier;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (TryGetMovement(other, out PlayerMovement movement))
            movement.SpeedMultiplier = 1f;
    }

    private static bool TryGetMovement(Collider2D other, out PlayerMovement movement)
    {
        movement = null;
        return other.attachedRigidbody != null && other.attachedRigidbody.TryGetComponent(out movement);
    }
}
