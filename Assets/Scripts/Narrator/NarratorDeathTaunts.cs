using System.Collections;
using UnityEngine;

// Burlas del Sistema al morir, con las tres reglas del GDD (Diálogos y Narrativa):
// 1. Primera muerte del juego: frase única.
// 2. Primera muerte en una trampa troll: otra frase única que no se repite. Si la primera muerte
//    del juego ya fue en una trampa troll, esta frase se guarda para la siguiente muerte troll.
// 3. Resto de muertes: una burla al azar cada N muertes (3.ª, 6.ª, 9.ª...).
public class NarratorDeathTaunts : MonoBehaviour
{
    [SerializeField] private NarratorLines taunts;
    [SerializeField, Min(1)] private int everyNDeaths = 3;
    [Tooltip("Muertes de la partida guardada, para no repetir la frase de la primera muerte al continuar.")]
    [SerializeField] private RunStats stats;
    [TextArea(1, 3)]
    [SerializeField] private string firstDeathLine;
    [TextArea(1, 3)]
    [SerializeField] private string firstTrollDeathLine;

    private int deaths;
    private bool trollLineSaid;

    private void OnEnable() => PlayerDeath.Died += OnPlayerDied;
    private void OnDisable() => PlayerDeath.Died -= OnPlayerDied;

    private IEnumerator Start()
    {
        // Se espera un fotograma para leer las muertes ya restauradas desde el guardado.
        yield return null;
        if (stats != null)
            deaths = Mathf.Max(deaths, stats.Deaths);
    }

    private void OnPlayerDied(Vector2 deathPosition)
    {
        deaths++;
        bool troll = TrollTrap.WasRecent;
        TrollTrap.Clear();

        if (deaths == 1 && !string.IsNullOrEmpty(firstDeathLine))
            Narrator.Say(firstDeathLine);
        else if (troll && !trollLineSaid && !string.IsNullOrEmpty(firstTrollDeathLine))
        {
            trollLineSaid = true;
            Narrator.Say(firstTrollDeathLine);
        }
        else if (taunts != null && deaths % everyNDeaths == 0)
        {
            string line = taunts.GetRandom(out AudioClip voice);
            Narrator.SayIfIdle(line, voice);
        }
    }
}
