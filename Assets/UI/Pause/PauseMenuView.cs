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

    private VisualElement pauseRoot;
    private VisualElement content;
    private VisualElement confirmation;
    private Button[] buttons;
    private Button cancelButton;
    private Button confirmButton;

    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        root.AddToClassList("menu-document");
        root.pickingMode = PickingMode.Ignore;
        pauseRoot = root.Q("pauseRoot");
        content = root.Q("pauseContent");
        confirmation = root.Q("exitConfirmation");
        buttons = new[] { root.Q<Button>("resumeButton"), root.Q<Button>("restartButton"), root.Q<Button>("exitButton") };
        cancelButton = root.Q<Button>("cancelExitButton");
        confirmButton = root.Q<Button>("confirmExitButton");

        foreach (Button button in buttons)
        {
            button.RegisterCallback<FocusInEvent>(OnFocusIn);
            button.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
        }
        buttons[0].clicked += OnResume;
        buttons[1].clicked += OnRestart;
        buttons[2].clicked += ShowExitConfirmation;
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
        buttons[2].clicked -= ShowExitConfirmation;
        cancelButton.clicked -= ShowPause;
        confirmButton.clicked -= OnExitConfirmed;
        IsVisible = false;
        IsExitConfirmationVisible = false;
    }

    public void ShowPause()
    {
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
        IsVisible = true;
        IsExitConfirmationVisible = true;
        pauseRoot.RemoveFromClassList("is-hidden");
        content.SetEnabled(false);
        content.AddToClassList("pause-content--covered");
        confirmation.RemoveFromClassList("is-hidden");
        cancelButton.schedule.Execute(() => cancelButton.Focus());
    }

    public void Hide()
    {
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
    private void OnExitConfirmed() => ExitConfirmed?.Invoke();
    private void OnPointerEnter(PointerEnterEvent evt) => ((Button)evt.currentTarget).Focus();

    private void OnFocusIn(FocusInEvent evt)
    {
        foreach (Button button in buttons)
            button.EnableInClassList("menu-button--selected", button == evt.currentTarget);
    }
}
