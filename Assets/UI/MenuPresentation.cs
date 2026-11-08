using System;
using UnknownGame.GameFlow;
using UnknownGame.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class MenuPresentation : MonoBehaviour
{
    [SerializeField] private string firstLevelScene = "Level_01";

    private Button[] buttons;
    private VisualElement root;
    private VisualElement menuButtons;
    private VisualElement confirmation;
    private Button confirmButton;
    private Button cancelButton;
    private Label statusLabel;
    private MenuGameFlow flow;
    private bool isLoading;
    private bool confirmationOpen;

    private void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        root.AddToClassList("menu-document");
        Button newGame = root.Q<Button>("newGameButton");
        Button continueGame = root.Q<Button>("continueGameButton");
        Button quit = root.Q<Button>("quitButton");
        buttons = new[] { newGame, continueGame, quit };
        menuButtons = root.Q("menuButtons");
        confirmation = root.Q("replaceConfirmation");
        confirmButton = root.Q<Button>("confirmNewGameButton");
        cancelButton = root.Q<Button>("cancelNewGameButton");
        statusLabel = root.Q<Label>("menuStatus");
        string menuScene = gameObject.scene.name;
        flow = new MenuGameFlow(new SaveGameService(), scene => scene != menuScene
            && Application.CanStreamedLevelBeLoaded(scene), firstLevelScene);
        isLoading = false;
        confirmationOpen = false;
        confirmation.AddToClassList("is-hidden");
        menuButtons.SetEnabled(true);
        RefreshSaveState();
        foreach (Button button in buttons)
        {
            button.RegisterCallback<FocusInEvent>(OnFocusIn);
            button.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
        }
        newGame.clicked += OnNewGame;
        continueGame.clicked += OnContinue;
        quit.clicked += OnQuit;
        confirmButton.clicked += OnConfirmNewGame;
        cancelButton.clicked += OnCancelNewGame;
        root.RegisterCallback<NavigationCancelEvent>(OnNavigationCancel);
        root.RegisterCallback<KeyDownEvent>(OnKeyDown);
        root.schedule.Execute(() => newGame.Focus());
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
        buttons[0].clicked -= OnNewGame;
        buttons[1].clicked -= OnContinue;
        buttons[2].clicked -= OnQuit;
        confirmButton.clicked -= OnConfirmNewGame;
        cancelButton.clicked -= OnCancelNewGame;
        root.UnregisterCallback<NavigationCancelEvent>(OnNavigationCancel);
        root.UnregisterCallback<KeyDownEvent>(OnKeyDown);
    }

    private void RefreshSaveState()
    {
        SaveLoadStatus status = flow.GetSaveStatus();
        buttons[0].text = status == SaveLoadStatus.Missing ? "JUGAR" : "CREAR PARTIDA";
        buttons[1].EnableInClassList("is-hidden", status != SaveLoadStatus.Loaded);
        ShowStatus(status == SaveLoadStatus.Loaded || status == SaveLoadStatus.Missing
            ? string.Empty : MenuGameFlow.GetStatusMessage(status));
    }

    private void OnNewGame()
    {
        if (isLoading || confirmationOpen)
            return;

        SaveLoadStatus status = flow.GetSaveStatus();
        if (status == SaveLoadStatus.IOError)
        {
            ShowStatus(MenuGameFlow.GetStatusMessage(status));
            return;
        }

        if (status != SaveLoadStatus.Missing)
        {
            confirmationOpen = true;
            menuButtons.SetEnabled(false);
            confirmation.RemoveFromClassList("is-hidden");
            cancelButton.Focus();
            return;
        }

        StartNewGame(false);
    }

    private void OnConfirmNewGame()
    {
        if (!confirmationOpen || isLoading)
            return;
        CloseConfirmation();
        StartNewGame(true);
    }

    private void StartNewGame(bool replaceExisting)
    {
        if (flow.TryStartNew(replaceExisting, out SaveGameData data, out string error))
            LoadLevel(data);
        else
        {
            RefreshSaveState();
            ShowStatus(error);
        }
    }

    private void OnContinue()
    {
        if (isLoading || confirmationOpen)
            return;
        if (flow.TryContinue(out SaveGameData data, out string error))
            LoadLevel(data);
        else
        {
            RefreshSaveState();
            ShowStatus(error);
            buttons[0].Focus();
        }
    }

    private void LoadLevel(SaveGameData data)
    {
        isLoading = true;
        menuButtons.SetEnabled(false);
        ShowStatus("Cargando partida...");
        try
        {
            GameSession.Begin(data, flow.Saves);
            if (SceneManager.LoadSceneAsync(data.levelScene, LoadSceneMode.Single) == null)
                throw new InvalidOperationException("Unity no pudo iniciar la carga del nivel.");
        }
        catch (Exception exception)
        {
            GameSession.Clear();
            isLoading = false;
            menuButtons.SetEnabled(true);
            ShowStatus("No se pudo abrir el nivel. Inténtalo de nuevo.");
            Debug.LogException(exception, this);
        }
    }

    private void OnCancelNewGame() => CloseConfirmation();

    private void CloseConfirmation()
    {
        confirmationOpen = false;
        confirmation.AddToClassList("is-hidden");
        menuButtons.SetEnabled(true);
        buttons[0].Focus();
    }

    private void OnNavigationCancel(NavigationCancelEvent evt)
    {
        if (!confirmationOpen)
            return;
        CloseConfirmation();
        evt.StopPropagation();
    }

    private void OnKeyDown(KeyDownEvent evt)
    {
        if (!confirmationOpen || evt.keyCode != KeyCode.Escape)
            return;
        CloseConfirmation();
        evt.StopPropagation();
    }

    private void ShowStatus(string message)
    {
        statusLabel.text = message;
        statusLabel.EnableInClassList("is-hidden", string.IsNullOrEmpty(message));
    }

    private void OnQuit()
    {
        if (isLoading || confirmationOpen)
            return;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnFocusIn(FocusInEvent evt) => Select((Button)evt.currentTarget);
    private void OnPointerEnter(PointerEnterEvent evt) => ((Button)evt.currentTarget).Focus();

    private void Select(Button selected)
    {
        foreach (Button button in buttons)
            button.EnableInClassList("menu-button--selected", button == selected);
    }
}
