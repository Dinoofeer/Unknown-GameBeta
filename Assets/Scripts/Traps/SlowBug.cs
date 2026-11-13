using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class SlowBug : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float distance = 3f;
    [SerializeField, Min(0.1f)] private float speed = 1.25f;

    private Rigidbody2D body;
    private Vector2 start;
    private float traveled;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        start = body.position;
    }

    private void FixedUpdate()
    {
        traveled += speed * Time.fixedDeltaTime;
        body.MovePosition(start + Vector2.right * Mathf.PingPong(traveled, distance));
    }
}
