using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class LungingPlant : MonoBehaviour
{
    [SerializeField] private Rigidbody2D plantBody;
    [SerializeField] private SpriteRenderer visual;
    [SerializeField, Min(0f)] private float warningTime = 0.32f;
    [SerializeField, Min(0.1f)] private float lungeSpeed = 3.25f;
    [SerializeField, Min(0.1f)] private float lungeDistance = 2.55f;
    [SerializeField] private Color warningColor = new(1f, 0.74f, 0.42f);
    [SerializeField] private Color spentColor = new(0.55f, 0.63f, 0.72f);

    private enum State { Idle, Warning, Lunging, Spent }

    private Collider2D hitbox;
    private Vector2 startPosition;
    private Vector2 targetPosition;
    private Color idleColor;
    private float warningRemaining;
    private State state;

    private void Awake()
    {
        if (plantBody == null || visual == null || !plantBody.TryGetComponent(out hitbox))
        {
            Debug.LogError("La planta necesita un cuerpo con collider y un SpriteRenderer.", this);
            enabled = false;
            return;
        }

        startPosition = plantBody.position;
        idleColor = visual.color;
    }

    private void OnEnable()
    {
        ResetPlant();
        RespawnManager.PlayerRespawned += ResetPlant;
    }

    private void OnDisable()
    {
        RespawnManager.PlayerRespawned -= ResetPlant;
        if (hitbox != null)
            hitbox.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActiveAndEnabled || state != State.Idle || !other.CompareTag("Player"))
            return;

        float direction = other.bounds.center.x < startPosition.x ? -1f : 1f;
        targetPosition = startPosition + Vector2.right * (direction * lungeDistance);
        warningRemaining = warningTime;
        visual.color = warningColor;
        state = State.Warning;
    }

    private void FixedUpdate()
    {
        if (state == State.Warning)
        {
            warningRemaining -= Time.fixedDeltaTime;
            if (warningRemaining > 0f)
                return;

            hitbox.enabled = true;
            state = State.Lunging;
        }

        if (state != State.Lunging)
            return;

        if (plantBody.position == targetPosition)
        {
            hitbox.enabled = false;
            visual.color = spentColor;
            state = State.Spent;
            return;
        }

        Vector2 next = Vector2.MoveTowards(plantBody.position, targetPosition, lungeSpeed * Time.fixedDeltaTime);
        plantBody.MovePosition(next);
    }

    private void ResetPlant()
    {
        hitbox.enabled = false;
        plantBody.linearVelocity = Vector2.zero;
        plantBody.position = startPosition;
        plantBody.transform.position = startPosition;
        visual.color = idleColor;
        state = State.Idle;
    }
}
