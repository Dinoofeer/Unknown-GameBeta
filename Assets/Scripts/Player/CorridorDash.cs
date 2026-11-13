using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class CorridorDash : MonoBehaviour
{
    [SerializeField] private BoxCollider2D corridor;
    [SerializeField] private InputActionReference dashAction;
    [SerializeField, Range(0.1f, 1f)] private float walkMultiplier = 0.5f;
    [SerializeField, Min(0.1f)] private float dashSpeed = 20f;
    [SerializeField, Min(0.02f)] private float dashDuration = 0.2f;
    [SerializeField, Min(0f)] private float recoveryTime = 0.15f;

    private Rigidbody2D body;
    private InputAction action;
    private bool dashRequested;
    private float facing = 1f;
    private float dashDirection;
    private float dashRemaining;
    private float cooldown;

    private bool InsideCorridor => body.simulated && corridor.enabled && corridor.OverlapPoint(body.position);

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        action = dashAction != null ? dashAction.action : null;
        if (corridor == null || action == null)
        {
            Debug.LogError("El dash necesita una zona y una acción de entrada.", this);
            enabled = false;
        }
    }

    private void OnEnable() => action.Enable();

    private void OnDisable()
    {
        action?.Disable();
        ResetDash();
    }

    private void Update()
    {
        if (GamePauseState.BlocksGameplayInput || !InsideCorridor)
            dashRequested = false;
        else if (action.WasPressedThisFrame())
            dashRequested = true;
    }

    public float ResolveSpeed(float direction, float normalSpeed)
    {
        if (direction != 0f)
            facing = direction;

        if (!InsideCorridor)
        {
            ResetDash();
            return direction * normalSpeed;
        }

        float step = Time.fixedDeltaTime;
        cooldown = Mathf.Max(0f, cooldown - step);
        if (dashRequested && cooldown <= 0f && !GamePauseState.BlocksGameplayInput)
        {
            dashDirection = facing;
            dashRemaining = dashDuration;
            cooldown = dashDuration + recoveryTime;
        }
        dashRequested = false;

        if (dashRemaining <= 0f)
            return direction * normalSpeed * walkMultiplier;

        float dashStep = Mathf.Min(dashRemaining, step);
        dashRemaining -= dashStep;
        return dashDirection * dashSpeed * (dashStep / step);
    }

    public void ResetDash()
    {
        dashRequested = false;
        dashRemaining = 0f;
        cooldown = 0f;
    }
}
