using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public sealed class LeverVisual : MonoBehaviour
{
    [SerializeField] private Sprite[] transitionFrames;
    [SerializeField] private Sprite activatedSprite;
    [SerializeField, Min(0.01f)] private float frameDuration = 0.04f;

    private SpriteRenderer spriteRenderer;
    private bool activated;

    private void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        var body = other.attachedRigidbody;
        if (activated || body == null || !body.TryGetComponent(out PlayerDeath player) || player.IsDead)
            return;

        activated = true;
        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        var wait = new WaitForSeconds(frameDuration);
        foreach (var frame in transitionFrames)
        {
            spriteRenderer.sprite = frame;
            yield return wait;
        }

        spriteRenderer.sprite = activatedSprite;
    }
}
