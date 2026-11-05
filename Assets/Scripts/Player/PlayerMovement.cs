using UnityEngine;
using UnityEngine.InputSystem;

// Mecánica 01 (GDD): movimiento lateral con arranque y frenado en seco.
// Solo controla la velocidad X; la Y queda para la gravedad y el salto.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Entrada")]
    [Tooltip("Acción Player/Move de Settings/InputSystem_Actions.")]
    [SerializeField] private InputActionReference moveAction;

    [Header("Movimiento")]
    [Tooltip("Velocidad horizontal en bloques (unidades) por segundo.")]
    [SerializeField, Min(0f)] private float maxSpeed = 8f;

    private Rigidbody2D rb;
    private float direction; // -1, 0 o 1

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        if (moveAction == null)
        {
            Debug.LogError($"{name}: falta asignar Move Action en PlayerMovement.", this);
            enabled = false;
            return;
        }
        moveAction.action.Enable();
    }

    private void OnDisable()
    {
        // Al morir se desactiva: que no conserve la dirección al reaparecer.
        direction = 0f;
    }

    private void Update()
    {
        // La entrada se lee cada frame para no perder pulsaciones.
        // Se redondea a -1/0/1: si no, pulsar W o arriba a la vez
        // normalizaría el vector y bajaría la velocidad al ~70 %.
        float x = moveAction.action.ReadValue<Vector2>().x;
        direction = x > 0.5f ? 1f : (x < -0.5f ? -1f : 0f);
    }

    private void FixedUpdate()
    {
        // Asignar la velocidad directamente da arranque y frenado instantáneos.
        rb.linearVelocity = new Vector2(direction * maxSpeed, rb.linearVelocity.y);
    }
}
