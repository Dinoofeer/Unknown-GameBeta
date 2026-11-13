using UnityEngine;

// Error del entorno de la sala de servidores (no es una trampa del juego): datos corruptos.
// Sin el Parche de Estabilidad mata al tocarla, como cualquier peligro; con el parche se atraviesa.
// Parpadea como señal corrupta (GDD, Entornos: "estética de glitch").
[RequireComponent(typeof(Collider2D))]
public class CorruptionZone : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] visuals;
    [SerializeField] private Color corruptColor = new Color32(0x8F, 0x2A, 0x31, 0xB0);
    [SerializeField] private Color stabilizedColor = new Color32(0x3A, 0x9B, 0x8D, 0x40);

    private void Reset() => GetComponent<Collider2D>().isTrigger = true;

    private void OnTriggerEnter2D(Collider2D other) => TryKill(other);
    private void OnTriggerStay2D(Collider2D other) => TryKill(other);

    private void TryKill(Collider2D other)
    {
        if (StabilityPatch.Active || !other.CompareTag("Player"))
            return;
        if (other.TryGetComponent(out PlayerDeath death) && !death.IsDead)
            death.Die();
    }

    [Tooltip("Opcional: colisionador sólido que bloquea el paso hasta que el parche está aplicado.")]
    [SerializeField] private Collider2D blocker;

    private void Update()
    {
        if (blocker != null && blocker.enabled == StabilityPatch.Active)
            blocker.enabled = !StabilityPatch.Active;
        if (visuals == null)
            return;
        bool glitch = Random.value < 0.25f;
        foreach (SpriteRenderer sr in visuals)
        {
            if (sr == null)
                continue;
            Color c = StabilityPatch.Active ? stabilizedColor : corruptColor;
            if (!StabilityPatch.Active && glitch)
                c.a *= Random.Range(0.3f, 1f);
            sr.color = c;
        }
    }
}
