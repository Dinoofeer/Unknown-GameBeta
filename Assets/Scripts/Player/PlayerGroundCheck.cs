using UnityEngine;

// Detecta si el jugador está apoyado sobre una superficie sólida (contacto con normal hacia arriba).
// No usa capas: cualquier collider sólido cuenta como suelo (también los bloques invisibles ya revelados).
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerGroundCheck : MonoBehaviour
{
    [Tooltip("Inclinación máxima (grados) de una superficie para contar como suelo. Paredes y techos no cuentan.")]
    [SerializeField, Range(0f, 89f)] private float maxGroundAngle = 45f;

    private Rigidbody2D rb;
    private ContactFilter2D groundFilter;

    public bool IsGrounded => rb.IsTouching(groundFilter);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        groundFilter = new ContactFilter2D { useTriggers = false };
        groundFilter.SetNormalAngle(90f - maxGroundAngle, 90f + maxGroundAngle);
    }
}
