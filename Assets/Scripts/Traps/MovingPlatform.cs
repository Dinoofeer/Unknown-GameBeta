using System.Collections.Generic;
using UnityEngine;

// Misión 2 (GDD): plataforma móvil que va y viene entre dos puntos y lleva al jugador encima.
[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    [Tooltip("Recorrido desde la posición inicial hasta el otro extremo, en celdas. (3, 0) = 3 celdas a la derecha.")]
    [SerializeField] private Vector2 travel = new Vector2(3f, 0f);
    [Tooltip("Celdas por segundo.")]
    [SerializeField, Min(0.1f)] private float speed = 2f;
    [Tooltip("Segundos de pausa en cada extremo.")]
    [SerializeField, Min(0f)] private float waitAtEnds = 0.3f;

    private readonly HashSet<Rigidbody2D> riders = new HashSet<Rigidbody2D>();
    private Rigidbody2D rb;
    private Vector2 start;
    private float progress;   // 0 = inicio, 1 = extremo
    private int direction = 1;
    private float waitTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        start = rb.position;
    }

    private void OnEnable() => RespawnManager.PlayerRespawned += ResetPlatform;
    private void OnDisable() => RespawnManager.PlayerRespawned -= ResetPlatform;

    private void ResetPlatform()
    {
        progress = 0f;
        direction = 1;
        waitTimer = 0f;
        riders.Clear();
        rb.position = start;
        transform.position = start;
    }

    private void FixedUpdate()
    {
        float length = travel.magnitude;
        if (length < 0.001f)
            return;

        if (waitTimer > 0f)
        {
            waitTimer -= Time.fixedDeltaTime;
            return;
        }

        Vector2 before = start + travel * progress;
        progress += direction * speed * Time.fixedDeltaTime / length;
        if (progress >= 1f || progress <= 0f)
        {
            progress = Mathf.Clamp01(progress);
            direction = -direction;
            waitTimer = waitAtEnds;
        }

        Vector2 target = start + travel * progress;
        rb.MovePosition(target);

        // Arrastra al jugador que está encima, sin tocar su velocidad (PlayerMovement la controla).
        Vector2 delta = target - before;
        foreach (Rigidbody2D rider in riders)
        {
            if (rider != null && rider.simulated)
                rider.position += delta;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) => TryAddRider(collision);
    private void OnCollisionStay2D(Collision2D collision) => TryAddRider(collision);

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.rigidbody != null)
            riders.Remove(collision.rigidbody);
    }

    private void TryAddRider(Collision2D collision)
    {
        Rigidbody2D other = collision.rigidbody;
        if (other == null || !(other.CompareTag("Player") || collision.collider.CompareTag("Player")))
            return;

        // Solo cuenta si está encima: la normal del contacto apunta hacia abajo, hacia la plataforma.
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y < -0.5f)
            {
                riders.Add(other);
                return;
            }
        }
    }
}
