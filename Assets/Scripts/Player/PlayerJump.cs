using UnityEngine;
using UnityEngine.InputSystem;

// Mecánica 02 (GDD): salto y doble salto con dos alturas fijas.
// Pulsación corta = salto leve; mantenida más que holdThreshold = salto máximo.
// Las alturas se miden desde el punto donde empieza cada salto.
[RequireComponent(typeof(Rigidbody2D), typeof(PlayerGroundCheck))]
public class PlayerJump : MonoBehaviour
{
    [Header("Entrada")]
    [Tooltip("Acción Player/Jump de Settings/InputSystem_Actions.")]
    [SerializeField] private InputActionReference jumpAction;

    [Header("Alturas (bloques)")]
    [SerializeField, Min(0f)] private float shortJumpHeight = 2f;
    [SerializeField, Min(0f)] private float maxJumpHeight = 4f;

    [Header("Pulsación")]
    [Tooltip("Segundos que hay que mantener el botón para que el salto pase de leve a máximo.")]
    [SerializeField, Min(0f)] private float holdThreshold = 0.12f;

    [Header("Coyote time")]
    [Tooltip("Segundos tras perder el suelo sin saltar en los que el salto aún cuenta como salto desde el suelo y no gasta el doble salto.")]
    [SerializeField, Min(0f)] private float coyoteTime = 0.1f;

    [Header("Doble salto")]
    [Tooltip("Impulso mínimo que debe notarse. Si aún se sube tan rápido que el doble salto ganaría menos, la pulsación espera a que se note en vez de gastarse.")]
    [SerializeField, Min(0f)] private float doubleJumpMinBoost = 3f;

    private Rigidbody2D rb;
    private PlayerGroundCheck groundCheck;

    private bool jumpRequested;   // pulsación leída en Update, aplicada en FixedUpdate
    private bool hasDoubleJump;
    private bool canUpgrade;      // el salto actual aún puede pasar de leve a máximo
    private bool coyoteAvailable; // salió del suelo sin saltar: aún puede usar el salto de suelo
    private float lastGroundedTime;
    private float jumpStartY;
    private float jumpStartTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        groundCheck = GetComponent<PlayerGroundCheck>();
    }

    private void OnEnable()
    {
        if (jumpAction == null)
        {
            Debug.LogError($"{name}: falta asignar Jump Action en PlayerJump.", this);
            enabled = false;
            return;
        }
        jumpAction.action.Enable();
    }

    private void OnDisable()
    {
        // Al morir se desactiva: que no complete un salto pendiente al reaparecer.
        jumpRequested = false;
        canUpgrade = false;
        coyoteAvailable = false;
    }

    private void Update()
    {
        if (jumpAction.action.WasPressedThisFrame())
            jumpRequested = true;
    }

    private void FixedUpdate()
    {
        // vy <= 0 evita contar como suelo el instante justo después de despegar.
        bool onGround = groundCheck.IsGrounded && rb.linearVelocity.y <= 0.01f;

        // GDD: el doble salto se recarga solo al volver a tocar suelo.
        if (onGround)
        {
            hasDoubleJump = true;
            coyoteAvailable = true;
            lastGroundedTime = Time.fixedTime;
        }

        if (jumpRequested)
        {
            jumpRequested = false;
            bool inCoyoteTime = coyoteAvailable && Time.fixedTime - lastGroundedTime <= coyoteTime;
            if (onGround || inCoyoteTime)
            {
                coyoteAvailable = false;
                StartJump();
            }
            else if (hasDoubleJump)
            {
                if (SpeedForHeight(shortJumpHeight) - rb.linearVelocity.y < doubleJumpMinBoost)
                {
                    jumpRequested = true; // se vuelve a intentar en el siguiente paso de física
                }
                else
                {
                    hasDoubleJump = false;
                    StartJump();
                }
            }
        }

        if (canUpgrade)
            TryUpgradeToMaxJump();
    }

    private void StartJump()
    {
        jumpStartY = rb.position.y;
        jumpStartTime = Time.fixedTime;
        canUpgrade = true;
        SetVerticalSpeed(SpeedForHeight(shortJumpHeight));
    }

    private void TryUpgradeToMaxJump()
    {
        // Soltó antes del umbral, o un techo / bloque invisible cortó la subida: se queda en leve.
        if (!jumpAction.action.IsPressed() || rb.linearVelocity.y <= 0f)
        {
            canUpgrade = false;
            return;
        }

        if (Time.fixedTime - jumpStartTime < holdThreshold)
            return;

        canUpgrade = false;
        // Recalcula la velocidad para completar exactamente maxJumpHeight desde el despegue.
        float remaining = maxJumpHeight - (rb.position.y - jumpStartY);
        if (remaining > 0f)
            SetVerticalSpeed(SpeedForHeight(remaining));
    }

    // v = raíz(2 * g * h) es exacta en tiempo continuo. Unity integra la física en pasos de
    // Time.fixedDeltaTime y eso pierde unos v·dt/2 de altura; sumar g·dt/2 lo compensa.
    private float SpeedForHeight(float height)
    {
        float gravity = -Physics2D.gravity.y * rb.gravityScale;
        return Mathf.Sqrt(2f * gravity * height) + gravity * Time.fixedDeltaTime * 0.5f;
    }

    private void SetVerticalSpeed(float speed)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, speed);
    }
}
