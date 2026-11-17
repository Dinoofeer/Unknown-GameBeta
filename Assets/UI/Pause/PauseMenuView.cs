using System;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class PauseMenuView : MonoBehaviour
{
    public event Action ResumeRequested;
    public event Action RestartRequested;
    public event Action ExitConfirmed;

    public bool IsVisible { get; private set; }
    public bool IsExitConfirmationVisible { get; private set; }
    public bool IsSettingsVisible { get; private set; }

    private VisualElement pauseRoot;
    private VisualElement content;
    private VisualElement confirmation;
    private VisualElement settings;
    private Slider master;
    private Slider music;
    private Slider sfx;
    private Toggle fullScreen;
    private Button closeSettingsButton;
    private Button[] buttons;
    private Button cancelButton;
    private Button confirmButton;
    private Label exitStatus;
    private bool exitBusy;

    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        root.AddToClassList("menu-document");
        root.pickingMode = PickingMode.Ignore;
        pauseRoot = root.Q("pauseRoot");
        content = root.Q("pauseContent");
        confirmation = root.Q("exitConfirmation");
        settings = root.Q("settingsPanel");
        master = root.Q<Slider>("masterVolume");
        music = root.Q<Slider>("musicVolume");
        sfx = root.Q<Slider>("sfxVolume");
        fullScreen = root.Q<Toggle>("fullScreenToggle");
        closeSettingsButton = root.Q<Button>("closeSettingsButton");
        buttons = new[] { root.Q<Button>("resumeButton"), root.Q<Button>("restartButton"), root.Q<Button>("settingsButton"), root.Q<Button>("exitButton") };
        cancelButton = root.Q<Button>("cancelExitButton");
        confirmButton = root.Q<Button>("confirmExitButton");
        exitStatus = root.Q<Label>("exitStatus");

        foreach (Button button in buttons)
        {
            button.RegisterCallback<FocusInEvent>(OnFocusIn);
            button.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
        }
        buttons[0].clicked += OnResume;
        buttons[1].clicked += OnRestart;
        buttons[2].clicked += ShowSettings;
        buttons[3].clicked += ShowExitConfirmation;
        closeSettingsButton.clicked += CloseSettings;
        master.RegisterValueChangedCallback(OnMaster);
        music.RegisterValueChangedCallback(OnMusic);
        sfx.RegisterValueChangedCallback(OnSfx);
        fullScreen.RegisterValueChangedCallback(OnFullScreen);
        cancelButton.clicked += ShowPause;
        confirmButton.clicked += OnExitConfirmed;
        Hide();

    }

    private void OnDisable()
    {
        if (buttons == null)
            return;
        foreach (Button button in buttons)
        {
            button.UnregisterCallback<FocusInEvent>(OnFocusIn);
            button.UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
        }
        buttons[0].clicked -= OnResume;
        buttons[1].clicked -= OnRestart;
        buttons[2].clicked -= ShowSettings;
        buttons[3].clicked -= ShowExitConfirmation;
        closeSettingsButton.clicked -= CloseSettings;
        master.UnregisterValueChangedCallback(OnMaster);
        music.UnregisterValueChangedCallback(OnMusic);
        sfx.UnregisterValueChangedCallback(OnSfx);
        fullScreen.UnregisterValueChangedCallback(OnFullScreen);
        if (IsSettingsVisible)
            GameSettings.Save();
        IsSettingsVisible = false;
        cancelButton.clicked -= ShowPause;
        confirmButton.clicked -= OnExitConfirmed;
        IsVisible = false;
        IsExitConfirmationVisible = false;
    }

    public void ShowPause()
    {
        if (exitBusy)
            return;
        ShowExitStatus(string.Empty);
        IsVisible = true;
        IsExitConfirmationVisible = false;
        pauseRoot.RemoveFromClassList("is-hidden");
        confirmation.AddToClassList("is-hidden");
        content.SetEnabled(true);
        content.RemoveFromClassList("pause-content--covered");
        buttons[0].schedule.Execute(() => buttons[0].Focus());
    }

    public void ShowExitConfirmation()
    {
        if (exitBusy)
            return;
        ShowExitStatus(string.Empty);
        IsVisible = true;
        IsExitConfirmationVisible = true;
        pauseRoot.RemoveFromClassList("is-hidden");
        content.SetEnabled(false);
        content.AddToClassList("pause-content--covered");
        confirmation.RemoveFromClassList("is-hidden");
        cancelButton.schedule.Execute(() => cancelButton.Focus());
    }

    private void ShowSettings()
    {
        if (exitBusy)
            return;
        master.SetValueWithoutNotify(GameSettings.Master);
        music.SetValueWithoutNotify(GameSettings.Music);
        sfx.SetValueWithoutNotify(GameSettings.Sfx);
        fullScreen.SetValueWithoutNotify(GameSettings.FullScreen);
        IsSettingsVisible = true;
        content.SetEnabled(false);
        content.AddToClassList("pause-content--covered");
        settings.RemoveFromClassList("is-hidden");
        master.schedule.Execute(() => master.Focus());
    }

    public void CloseSettings()
    {
        if (!IsSettingsVisible)
            return;
        GameSettings.Save();
        IsSettingsVisible = false;
        settings.AddToClassList("is-hidden");
        content.SetEnabled(true);
        content.RemoveFromClassList("pause-content--covered");
        buttons[2].schedule.Execute(() => buttons[2].Focus());
    }

    private void OnMaster(ChangeEvent<float> evt) => GameSettings.Master = evt.newValue;
    private void OnMusic(ChangeEvent<float> evt) => GameSettings.Music = evt.newValue;
    private void OnSfx(ChangeEvent<float> evt) => GameSettings.Sfx = evt.newValue;
    private void OnFullScreen(ChangeEvent<bool> evt) => GameSettings.FullScreen = evt.newValue;

    public void Hide()
    {
        if (IsSettingsVisible)
            CloseSettings();
        SetExitBusy(false);
        ShowExitStatus(string.Empty);
        var focused = pauseRoot.panel?.focusController.focusedElement as VisualElement;
        if (focused != null && pauseRoot.Contains(focused))
            focused.Blur();
        IsVisible = false;
        IsExitConfirmationVisible = false;
        pauseRoot.AddToClassList("is-hidden");
        confirmation.AddToClassList("is-hidden");
        content.SetEnabled(true);
        content.RemoveFromClassList("pause-content--covered");
    }

    private void OnResume() => ResumeRequested?.Invoke();
    private void OnRestart() => RestartRequested?.Invoke();
    private void OnExitConfirmed()
    {
        if (!exitBusy)
            ExitConfirmed?.Invoke();
    }

    public void SetExitBusy(bool busy)
    {
        exitBusy = busy;
        cancelButton.SetEnabled(!busy);
        confirmButton.SetEnabled(!busy);
        if (busy)
            ShowExitStatus("Volviendo al menú principal...");
    }

    public void ShowExitError(string message)
    {
        SetExitBusy(false);
        ShowExitStatus(message);
        cancelButton.schedule.Execute(() => cancelButton.Focus());
    }

    private void ShowExitStatus(string message)
    {
        exitStatus.text = message;
        exitStatus.EnableInClassList("is-hidden", string.IsNullOrEmpty(message));
    }

    private void OnPointerEnter(PointerEnterEvent evt) => ((Button)evt.currentTarget).Focus();

    private void OnFocusIn(FocusInEvent evt)
    {
        foreach (Button button in buttons)
            button.EnableInClassList("menu-button--selected", button == evt.currentTarget);
    }
}
