using UnityEngine;

// Cronómetro y contador de muertes de la partida (mecánicas secundarias del GDD).
// Solo cuenta; mostrarlo es tarea de StatsHUD. Más adelante el guardado leerá estos valores.
// No es static a propósito: el proyecto no recarga el dominio al entrar en Play Mode.
public class RunStats : MonoBehaviour
{
    public int Deaths { get; private set; }
    public float ElapsedTime { get; private set; }
    public bool IsRunning { get; private set; } = true;

    private void OnEnable()
    {
        PlayerDeath.Died += OnPlayerDied;
        GoalDoor.LevelCompleted += OnLevelCompleted;
    }

    private void OnDisable()
    {
        PlayerDeath.Died -= OnPlayerDied;
        GoalDoor.LevelCompleted -= OnLevelCompleted;
    }

    private void Update()
    {
        if (IsRunning)
            ElapsedTime += Time.deltaTime;
    }

    private void OnPlayerDied(Vector2 deathPosition) => Deaths++;
    private void OnLevelCompleted() => IsRunning = false;
}
