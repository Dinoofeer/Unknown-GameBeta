using System.Collections;
using UnknownGame.GameFlow;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LevelExitTransition : MonoBehaviour
{
    [SerializeField] private string nextScene;
    [SerializeField] private RunStats stats;

    private void OnEnable() => GoalDoor.LevelCompleted += OnCompleted;
    private void OnDisable() => GoalDoor.LevelCompleted -= OnCompleted;

    private void OnCompleted() => StartCoroutine(Advance());

    private IEnumerator Advance()
    {
        yield return new WaitForSeconds(3f);

        if (!Application.CanStreamedLevelBeLoaded(nextScene))
        {
            Debug.LogError($"El siguiente nivel ({nextScene}) no está disponible en la compilación.", this);
            yield break;
        }

        var current = GameSession.Current;
        if (current != null)
        {
            if (current.levelScene != gameObject.scene.name)
                yield break;

            var progress = new RunProgressService(current, GameSession.Saves);
            bool reported = false;
            while (GameSession.Current == current)
            {
                if (progress.TryAdvance(nextScene, stats.Deaths, stats.ElapsedTime, out var next, out string error))
                {
                    GameSession.Begin(next, GameSession.Saves);
                    break;
                }

                if (!reported)
                {
                    Debug.LogWarning("No se pudo guardar el acceso al siguiente nivel. " + error, this);
                    Narrator.Say("No se pudo guardar el avance. Reintentando…");
                    reported = true;
                }
                yield return new WaitForSeconds(2f);
            }

            if (GameSession.Current?.levelScene != nextScene)
                yield break;
        }

        SceneManager.LoadSceneAsync(nextScene);
    }
}
