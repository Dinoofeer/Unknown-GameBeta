using UnityEngine;

// El Sistema se burla al morir: siempre la primera vez y después cada N muertes.
public class NarratorDeathTaunts : MonoBehaviour
{
    [SerializeField] private NarratorLines taunts;
    [SerializeField, Min(1)] private int everyNDeaths = 3;

    private int deaths;

    private void OnEnable() => PlayerDeath.Died += OnPlayerDied;
    private void OnDisable() => PlayerDeath.Died -= OnPlayerDied;

    private void OnPlayerDied(Vector2 deathPosition)
    {
        deaths++;
        if (taunts != null && (deaths == 1 || deaths % everyNDeaths == 0))
            Narrator.SayIfIdle(taunts.GetRandom());
    }
}
