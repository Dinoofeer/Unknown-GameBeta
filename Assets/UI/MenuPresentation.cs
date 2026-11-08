using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class MenuPresentation : MonoBehaviour
{
    [Tooltip("Solo previsualización visual. No crea ni lee partidas guardadas. Cambiar antes de pulsar Play.")]
    [SerializeField] private bool previewSavedGame;

    private Button[] buttons;

    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        root.AddToClassList("menu-document");
        Button newGame = root.Q<Button>("newGameButton");
        Button continueGame = root.Q<Button>("continueGameButton");
        Button quit = root.Q<Button>("quitButton");
        buttons = new[] { newGame, continueGame, quit };
        newGame.text = previewSavedGame ? "CREAR PARTIDA" : "JUGAR";
        continueGame.EnableInClassList("is-hidden", !previewSavedGame);
        foreach (Button button in buttons)
        {
            button.RegisterCallback<FocusInEvent>(OnFocusIn);
            button.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
        }
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
    }

    private void OnFocusIn(FocusInEvent evt) => Select((Button)evt.currentTarget);
    private void OnPointerEnter(PointerEnterEvent evt) => ((Button)evt.currentTarget).Focus();

    private void Select(Button selected)
    {
        foreach (Button button in buttons)
            button.EnableInClassList("menu-button--selected", button == selected);
    }
}
