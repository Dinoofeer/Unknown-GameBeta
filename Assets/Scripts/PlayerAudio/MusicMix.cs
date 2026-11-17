using UnityEngine;

// GDD (Audio y Música › Mezcla): música de fondo al 60-70 % con atenuación automática
// cuando el jugador muere o cuando Sistema.exe muestra un mensaje.
public class MusicMix : MonoBehaviour
{
    [SerializeField] private AudioSource music;
    [Tooltip("Ventana de mensajes de Sistema.exe: mientras está visible, la música se atenúa.")]
    [SerializeField] private GameObject narratorWindow;
    [Tooltip("Volumen base de la música. GDD: 60-70 %.")]
    [SerializeField, Range(0.6f, 0.7f)] private float baseVolume = 0.65f;
    [Tooltip("Fracción del volumen base mientras dura la atenuación.")]
    [SerializeField, Range(0f, 1f)] private float duckedFactor = 0.4f;
    [Tooltip("Segundos que dura la atenuación tras una muerte.")]
    [SerializeField, Min(0f)] private float deathDuckSeconds = 0.6f;
    [Tooltip("Velocidad de la transición de volumen (unidades por segundo).")]
    [SerializeField, Min(0.1f)] private float fadeSpeed = 3f;

    private float deathTimer;

    private void Start()
    {
        if (music != null)
            music.volume = baseVolume * GameSettings.Music;
    }

    private void OnEnable() => PlayerDeath.Died += OnDied;
    private void OnDisable() => PlayerDeath.Died -= OnDied;

    private void OnDied(Vector2 _) => deathTimer = deathDuckSeconds;

    private void Update()
    {
        if (music == null)
            return;

        if (deathTimer > 0f)
            deathTimer -= Time.unscaledDeltaTime;

        bool ducked = deathTimer > 0f || (narratorWindow != null && narratorWindow.activeInHierarchy);
        // Volumen de música de la Configuración (GDD, Datos y Persistencia).
        float target = baseVolume * GameSettings.Music * (ducked ? duckedFactor : 1f);
        music.volume = Mathf.MoveTowards(music.volume, target, fadeSpeed * Time.unscaledDeltaTime);
    }
}
