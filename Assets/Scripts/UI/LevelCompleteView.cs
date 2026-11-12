using TMPro;
using UnityEngine;

// GDD (Interfaz Visual): al completar el nivel aparece de golpe la ventana «Nivel completado»,
// con borde verde azulado, el tiempo, las muertes y un comentario mordaz de Sistema.exe.
// El GDD no define botones ni transición, así que la ventana solo informa; ESC (pausa) sigue disponible.
public class LevelCompleteView : MonoBehaviour
{
    [SerializeField] private GameObject window;
    [SerializeField] private RunStats stats;
    [SerializeField] private GoalDoor door;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text deathsText;
    [SerializeField] private TMP_Text commentText;
    [Tooltip("Scripts de control del jugador que se detienen al completar el nivel.")]
    [SerializeField] private Behaviour[] playerControl;

    private void Awake() => window.SetActive(false);

    private void OnEnable() => GoalDoor.LevelCompleted += Show;
    private void OnDisable() => GoalDoor.LevelCompleted -= Show;

    private void Show()
    {
        foreach (Behaviour control in playerControl)
        {
            if (control == null)
                continue;
            control.enabled = false;
            // Sin control no hay freno: se anula la velocidad horizontal para que no siga deslizándose.
            if (control.TryGetComponent(out Rigidbody2D body))
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        }

        float time = stats != null ? stats.ElapsedTime : 0f;
        timeText.text = $"Tiempo: {(int)(time / 60f):00}:{(int)(time % 60f):00}";
        deathsText.text = $"Muertes: {(stats != null ? stats.Deaths : 0)}";
        commentText.text = door != null ? door.CompletedLine : string.Empty;

        // Sin transición: aparece de golpe, como pide el GDD.
        window.SetActive(true);
    }
}
