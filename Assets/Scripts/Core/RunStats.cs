using UnityEngine;

// Cronómetro y contador de muertes de la partida (mecánicas secundarias del GDD).
// Mostrarlo es tarea de StatsHUD; LevelProgressPersistence conserva y restaura estos valores.
// No es static a propósito: el proyecto no recarga el dominio al entrar en Play Mode.
public class RunStats : MonoBehaviour
{
    public int Deaths { get; private set; }
    public float ElapsedTime { get; private set; }
    public bool IsRunning { get; private set; } = true;

    public void Restore(int deaths, float elapsedTime)
    {
        Deaths = deaths;
        ElapsedTime = elapsedTime;
        IsRunning = true;
    }

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
