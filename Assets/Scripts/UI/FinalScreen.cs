using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// GDD (Premisa y Trama › Finales, final único "Salir"): el juego termina con un párrafo en pantalla
// que indica que el jugador logró salir. El texto está en la escena (componente TMP) y es el del GDD.
// El GDD no define créditos ni transición: corte directo al menú principal.
public class FinalScreen : MonoBehaviour
{
    [SerializeField] private string mainMenuScene = "Menu-Principal";
    [Tooltip("Segundos antes de aceptar una tecla para volver al menú (evita saltarlo sin querer).")]
    [SerializeField, Min(0f)] private float minSeconds = 3f;
    [Tooltip("Segundos tras los que se vuelve al menú aunque no se pulse nada.")]
    [SerializeField, Min(1f)] private float maxSeconds = 15f;
    [SerializeField] private FinalScoreSubmission scoreSubmission;

    private float elapsed;
    private bool leaving;
    private bool checkedScoreSubmission;

    private void Update()
    {
        if (leaving || (scoreSubmission != null && scoreSubmission.gameObject.activeSelf))
            return;

        elapsed += Time.unscaledDeltaTime;
        if (elapsed >= minSeconds && !checkedScoreSubmission)
        {
            checkedScoreSubmission = true;
            if (scoreSubmission != null && scoreSubmission.TryShow(ReturnToMenu))
                return;
        }

        bool keyPressed = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        if ((elapsed >= minSeconds && keyPressed) || elapsed >= maxSeconds)
            ReturnToMenu();
    }

    private void ReturnToMenu()
    {
        if (leaving)
            return;

        leaving = true;
        SceneManager.LoadSceneAsync(mainMenuScene);
    }
}
