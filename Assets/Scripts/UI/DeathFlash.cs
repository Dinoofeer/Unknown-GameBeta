using UnityEngine;
using UnityEngine.UI;

// GDD (Interfaz Visual › Estados de la ventana, "Muerte"): destello rojo breve con parpadeo tipo CRT.
// Imagen a pantalla completa en el HUD que se enciende al morir y parpadea unos fotogramas.
public class DeathFlash : MonoBehaviour
{
    [SerializeField] private Image overlay;
    [Tooltip("Rojo vivo de la paleta del GDD.")]
    [SerializeField] private Color flashColor = new Color32(0xE1, 0x34, 0x48, 0x73);
    [Tooltip("Duración total del destello, en segundos. Breve: la reaparición es casi inmediata.")]
    [SerializeField, Min(0.01f)] private float duration = 0.15f;
    [Tooltip("Cambios de encendido/apagado por segundo (parpadeo CRT).")]
    [SerializeField, Min(1f)] private float flickerRate = 40f;

    private float timer;

    private void Awake() => overlay.enabled = false;

    private void OnEnable() => PlayerDeath.Died += OnDied;
    private void OnDisable() => PlayerDeath.Died -= OnDied;

    private void OnDied(Vector2 _)
    {
        timer = duration;
        overlay.color = flashColor;
        overlay.enabled = true;
    }

    private void Update()
    {
        if (timer <= 0f)
            return;

        // unscaled: el destello se ve aunque el tiempo se pare o se ralentice al morir.
        timer -= Time.unscaledDeltaTime;
        if (timer <= 0f)
        {
            overlay.enabled = false;
            return;
        }

        // Parpadeo: alterna entre el rojo completo y uno más tenue, y se desvanece al final.
        bool on = Mathf.FloorToInt(timer * flickerRate) % 2 == 0;
        float fade = timer / duration;
        Color c = flashColor;
        c.a = flashColor.a * fade * (on ? 1f : 0.4f);
        overlay.color = c;
    }
}
