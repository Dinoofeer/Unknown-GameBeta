using UnityEngine;
using UnityEngine.Rendering.Universal;

// Solo visual: hace parpadear o latir una luz 2D. No afecta a colisiones ni reglas.
[RequireComponent(typeof(Light2D))]
public class LightPulse : MonoBehaviour
{
    private enum Mode { Pulse, Flicker }

    [SerializeField] private Mode mode = Mode.Pulse;
    [Tooltip("Pulsos por segundo; en parpadeo, cambios por segundo.")]
    [SerializeField, Min(0.01f)] private float speed = 1f;
    [Tooltip("Intensidad mínima respecto a la original.")]
    [SerializeField, Range(0f, 1f)] private float minimum = 0.5f;

    private Light2D target;
    private float baseIntensity;
    private float seed;

    private void Awake()
    {
        target = GetComponent<Light2D>();
        baseIntensity = target.intensity;
        seed = Mathf.Abs(transform.position.x) * 0.37f;
    }

    private void Update()
    {
        float amount;
        if (mode == Mode.Pulse)
        {
            amount = (Mathf.Sin(Time.time * speed * Mathf.PI * 2f + seed) + 1f) * 0.5f;
        }
        else
        {
            // Escalones de ruido: más parecido a un fluorescente roto que una onda suave.
            float noise = Mathf.PerlinNoise(Mathf.Floor(Time.time * speed) * 0.73f, seed);
            amount = noise < 0.35f ? 0f : 1f;
        }
        target.intensity = baseIntensity * Mathf.Lerp(minimum, 1f, amount);
    }
}
