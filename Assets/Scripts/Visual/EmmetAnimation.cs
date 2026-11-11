using UnityEngine;

// Displays Emmet's current state using the player's existing motion and respawn signals.
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer), typeof(PlayerDeath), typeof(PlayerGroundCheck))]
public sealed class EmmetAnimation : MonoBehaviour
{
    private enum Pose { Idle, Walk, Rise, Fall, Death, Respawn }

    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private Sprite[] riseFrames;
    [SerializeField] private Sprite[] fallFrames;
    [SerializeField] private Sprite[] deathFrames;
    [SerializeField] private Sprite[] respawnFrames;

    private const float MotionThreshold = 0.05f;
    private const float RespawnFps = 30f;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D body;
    private PlayerGroundCheck ground;
    private PlayerDeath death;
    private Pose pose;
    private float elapsed;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
        ground = GetComponent<PlayerGroundCheck>();
        death = GetComponent<PlayerDeath>();
    }

    private void OnEnable() => RespawnManager.PlayerRespawned += BeginRespawn;
    private void OnDisable() => RespawnManager.PlayerRespawned -= BeginRespawn;

    private void BeginRespawn()
    {
        pose = Pose.Respawn;
        elapsed = 0f;
    }

    private void LateUpdate()
    {
        if (death.IsDead)
        {
            Show(Pose.Death, deathFrames, 18f);
            return;
        }

        if (pose == Pose.Death)
            BeginRespawn();

        Vector2 velocity = body.linearVelocity;
        bool moving = Mathf.Abs(velocity.x) > MotionThreshold;
        bool rising = velocity.y > MotionThreshold;
        if (moving)
            spriteRenderer.flipX = velocity.x < 0f;

        if (pose == Pose.Respawn && elapsed * RespawnFps < respawnFrames.Length && !moving && !rising)
            Show(Pose.Respawn, respawnFrames, RespawnFps);
        else if (rising)
            Show(Pose.Rise, riseFrames, 15f);
        else if (!ground.IsGrounded)
            Show(Pose.Fall, fallFrames, 12f);
        else if (moving)
            Show(Pose.Walk, walkFrames, 14f, loop: true);
        else
            Show(Pose.Idle, idleFrames, 5f, loop: true);
    }

    private void Show(Pose next, Sprite[] frames, float fps, bool loop = false)
    {
        if (pose != next)
        {
            pose = next;
            elapsed = 0f;
        }

        int frame = Mathf.FloorToInt(elapsed * fps);
        spriteRenderer.enabled = next != Pose.Death || frame < frames.Length;
        spriteRenderer.sprite = frames[Mathf.Min(frame, frames.Length - 1)];
        elapsed += Time.deltaTime;
        if (loop)
            elapsed = Mathf.Repeat(elapsed, frames.Length / fps);
    }
}
