using UnityEngine;

// Mecánica 09 (aprobada por Jesús): pared falsa. Parece sólida, pero se atraviesa.
// Se vuelve semitransparente mientras el jugador está dentro. No mata ni tiene temporizador.
[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class FakeWall : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float insideAlpha = 0.4f;
    [SerializeField, Min(0.01f)] private float fadeSeconds = 0.15f;

    private SpriteRenderer[] sprites;
    private bool playerInside;
    private float alpha = 1f;

    public bool PlayerInside => playerInside;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        sprites = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void OnEnable() => RespawnManager.PlayerRespawned += ResetWall;
    private void OnDisable() => RespawnManager.PlayerRespawned -= ResetWall;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            TrollTrap.Mark();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInside = false;
    }

    private void Update()
    {
        float target = playerInside ? insideAlpha : 1f;
        if (Mathf.Approximately(alpha, target))
            return;

        alpha = Mathf.MoveTowards(alpha, target, Time.deltaTime * (1f - insideAlpha) / fadeSeconds);
        ApplyAlpha();
    }

    private void ResetWall()
    {
        playerInside = false;
        alpha = 1f;
        ApplyAlpha();
    }

    private void ApplyAlpha()
    {
        foreach (SpriteRenderer sprite in sprites)
        {
            Color color = sprite.color;
            color.a = alpha;
            sprite.color = color;
        }
    }
}
