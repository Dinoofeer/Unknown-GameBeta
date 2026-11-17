using UnityEngine;

// Pasarela rota de la sala de servidores. Con el Parche de Estabilidad activo, se reconstruye
// (se vuelve sólida y cambia a verde azulado) cuando Emmet se acerca. GDD (Objetos): el parche
// "corrige temporalmente los errores del entorno".
[RequireComponent(typeof(Collider2D))]
public class RebuildingWalkway : MonoBehaviour
{
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private Transform player;
    [SerializeField, Min(0f)] private float rebuildDistance = 6f;
    [SerializeField] private Color brokenColor = new Color32(0x8F, 0x2A, 0x31, 0x55);
    [SerializeField] private Color rebuiltColor = new Color32(0x3A, 0x9B, 0x8D, 0xFF);

    private Collider2D solid;
    private bool rebuilt;

    private void Awake()
    {
        solid = GetComponent<Collider2D>();
        solid.enabled = false;
        if (visual != null)
            visual.color = brokenColor;
    }

    private void Update()
    {
        if (rebuilt || player == null || !StabilityPatch.Active)
            return;
        if (Mathf.Abs(player.position.x - transform.position.x) > rebuildDistance)
            return;

        rebuilt = true;
        solid.enabled = true;
        if (visual != null)
            visual.color = rebuiltColor;
    }
}
