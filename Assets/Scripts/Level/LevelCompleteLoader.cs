using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Al cruzar la puerta (GoalDoor.LevelCompleted), espera unos segundos y carga el siguiente nivel.
// Va en "Sistemas del nivel". En Level_01 pon "Level_02"; en Level_02 pon "Level_03" cuando exista.
public class LevelCompleteLoader : MonoBehaviour
{
    [Tooltip("Nombre exacto de la escena siguiente. Debe estar en File → Build Profiles → Scene List.")]
    [SerializeField] private string nextScene = "Level_02";
    [Tooltip("Segundos para que se lea la frase del Sistema antes de cambiar de escena.")]
    [SerializeField, Min(0f)] private float delay = 2.5f;

    private bool loading;

    private void OnEnable() => GoalDoor.LevelCompleted += OnLevelCompleted;
    private void OnDisable() => GoalDoor.LevelCompleted -= OnLevelCompleted;

    private void OnLevelCompleted()
    {
        if (!loading)
            StartCoroutine(LoadNext());
    }

    private IEnumerator LoadNext()
    {
        loading = true;
        yield return new WaitForSecondsRealtime(delay);

        if (string.IsNullOrEmpty(nextScene) || !Application.CanStreamedLevelBeLoaded(nextScene))
        {
            Debug.LogError($"{name}: la escena '{nextScene}' no existe o no está en la lista de Build Profiles.", this);
            loading = false;
            yield break;
        }
        SceneManager.LoadScene(nextScene);
    }
}
