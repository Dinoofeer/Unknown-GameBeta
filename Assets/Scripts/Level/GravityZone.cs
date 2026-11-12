using UnityEngine;

// Misión 2 (GDD): "zonas de gravedad alterada".
// Dentro de la zona la gravedad del jugador se multiplica. Como PlayerJump calcula el impulso
// a partir de gravityScale, el salto sigue alcanzando 2 o 4 celdas, pero sube y cae más lento.
[RequireComponent(typeof(Collider2D))]
public class GravityZone : MonoBehaviour
{
    [Tooltip("0,5 = gravedad a la mitad (flota) · 2 = cae más rápido.")]
    [SerializeField, Range(0.1f, 3f)] private float gravityMultiplier = 0.5f;

    private Rigidbody2D affected;
    private float originalGravity;

    private void Reset() => GetComponent<Collider2D>().isTrigger = true;

    private void OnEnable() => RespawnManager.PlayerRespawned += Restore;
    private void OnDisable()
    {
        RespawnManager.PlayerRespawned -= Restore;
        Restore();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D body = other.attachedRigidbody;
        if (body == null || affected != null || !body.CompareTag("Player"))
            return;

        affected = body;
        originalGravity = body.gravityScale;
        body.gravityScale = originalGravity * gravityMultiplier;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.attachedRigidbody != null && other.attachedRigidbody == affected)
            Restore();
    }

    private void Restore()
    {
        if (affected == null)
            return;
        affected.gravityScale = originalGravity;
        affected = null;
    }
}
