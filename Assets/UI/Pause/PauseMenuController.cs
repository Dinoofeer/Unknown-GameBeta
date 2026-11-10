using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-200)]
[RequireComponent(typeof(PauseMenuView))]
public class PauseMenuController : MonoBehaviour
{
    public bool IsPaused { get; private set; }

    private PauseMenuView view;
    private float previousTimeScale;
    private bool previousAudioPause;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLock;

    private void Awake() => view = GetComponent<PauseMenuView>();

    private void OnEnable() => view.ResumeRequested += Resume;

    private void OnDisable()
    {
        view.ResumeRequested -= Resume;
        Resume();
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
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
        if (GamePauseState.IsPaused || Time.timeScale <= 0f)
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

    public void Resume()
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
