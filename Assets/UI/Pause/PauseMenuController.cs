using System;
using UnknownGame.GameFlow;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-200)]
[RequireComponent(typeof(PauseMenuView))]
public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private RespawnManager respawnManager;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private LevelProgressPersistence progressPersistence;
    [SerializeField] private string mainMenuScene = "Menu-Principal";

    public bool IsPaused { get; private set; }

    private PauseMenuView view;
    private float previousTimeScale;
    private bool previousAudioPause;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLock;
    private bool isLeaving;

    private void Awake() => view = GetComponent<PauseMenuView>();

    private void OnEnable()
    {
        view.ResumeRequested += Resume;
        view.RestartRequested += Restart;
        view.ExitConfirmed += ReturnToMenu;
    }

    private void OnDisable()
    {
        view.ResumeRequested -= Resume;
        view.RestartRequested -= Restart;
        view.ExitConfirmed -= ReturnToMenu;
        RestorePauseState();
        if (isLeaving)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void Update()
    {
        if (isLeaving || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        if (!IsPaused)
            Pause();
        else if (view.IsExitConfirmationVisible)
            view.ShowPause();
        else
            Resume();
    }

    public void Pause()
    {
        if (isLeaving || GamePauseState.IsPaused || Time.timeScale <= 0f)
            return;

        previousTimeScale = Time.timeScale;
        previousAudioPause = AudioListener.pause;
        previousCursorVisible = Cursor.visible;
        previousCursorLock = Cursor.lockState;
        IsPaused = true;
        GamePauseState.SetPaused(true);
        Time.timeScale = 0f;
        AudioListener.pause = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        view.ShowPause();
    }

    private void Restart()
    {
        if (isLeaving || !IsPaused || view.IsExitConfirmationVisible)
            return;

        if (respawnManager == null || !respawnManager.RestartFromCheckpoint())
        {
            Debug.LogError("Pausa: no se pudo reiniciar. Revisa el Respawn Manager del nivel.", this);
            return;
        }

        if (cameraFollow != null)
            cameraFollow.SnapToTarget();
        Resume();
    }

    public void Resume()
    {
        if (!isLeaving)
            RestorePauseState();
    }

    private void ReturnToMenu()
    {
        if (isLeaving || !IsPaused || !view.IsExitConfirmationVisible)
            return;

        if (string.IsNullOrWhiteSpace(mainMenuScene) || mainMenuScene == gameObject.scene.name
            || !Application.CanStreamedLevelBeLoaded(mainMenuScene))
        {
            view.ShowExitError("No se pudo abrir el menú principal. Puedes cancelar y seguir jugando.");
            return;
        }

        isLeaving = true;
        view.SetExitBusy(true);
        if (GameSession.Current != null)
        {
            string error = "Falta la referencia al guardado del nivel.";
            if (progressPersistence == null || !progressPersistence.TrySaveBeforeLeaving(out error))
            {
                isLeaving = false;
                view.ShowExitError("No se pudo guardar la partida. Revisa el espacio disponible y los permisos, y vuelve a intentarlo. También puedes cancelar y seguir jugando.");
                Debug.LogWarning("Pausa: no se salió del nivel porque no se pudo guardar. " + error, this);
                return;
            }
        }

        try
        {
            if (SceneManager.LoadSceneAsync(mainMenuScene, LoadSceneMode.Single) == null)
                throw new InvalidOperationException("Unity no pudo iniciar la carga del menú principal.");
            GameSession.Clear();
        }
        catch (Exception exception)
        {
            isLeaving = false;
            view.ShowExitError("No se pudo abrir el menú principal. Puedes volver a intentarlo o cancelar y seguir jugando.");
            Debug.LogWarning("Pausa: no se pudo volver al menú. " + exception.Message, this);
        }
    }

    private void RestorePauseState()
    {
        if (!IsPaused)
            return;

        view.Hide();
        Time.timeScale = previousTimeScale;
        AudioListener.pause = previousAudioPause;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
        IsPaused = false;
        GamePauseState.SetPaused(false);
    }
}
