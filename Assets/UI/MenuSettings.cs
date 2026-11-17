using UnityEngine;
using UnityEngine.UIElements;

// GDD (UI y Menús: Menú principal › Configuración; Datos y Persistencia › configuracion):
// volumen general, de música y de efectos, y pantalla completa. Se guarda al cerrar el panel.
// Controles y Accesibilidad no tienen opciones definidas en el GDD (tablas vacías).
[RequireComponent(typeof(UIDocument))]
public class MenuSettings : MonoBehaviour
{
    [Tooltip("Música del menú; su volumen sigue al de Música de la Configuración.")]
    [SerializeField] private AudioSource menuMusic;

    private VisualElement menuButtons;
    private VisualElement panel;
    private Button openButton;
    private Button closeButton;
    private Slider master;
    private Slider music;
    private Slider sfx;
    private Toggle fullScreen;
    private float menuMusicBaseVolume = 1f;

    public bool IsOpen { get; private set; }

    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        menuButtons = root.Q("menuButtons");
        panel = root.Q("settingsPanel");
        openButton = root.Q<Button>("settingsButton");
        closeButton = root.Q<Button>("closeSettingsButton");
        master = root.Q<Slider>("masterVolume");
        music = root.Q<Slider>("musicVolume");
        sfx = root.Q<Slider>("sfxVolume");
        fullScreen = root.Q<Toggle>("fullScreenToggle");
        if (panel == null || openButton == null || closeButton == null || master == null || music == null || sfx == null || fullScreen == null)
        {
            Debug.LogError($"{name}: faltan elementos de la Configuración en el menú.", this);
            enabled = false;
            return;
        }

        if (menuMusic != null)
            menuMusicBaseVolume = menuMusic.volume;
        ApplyMenuMusic();

        openButton.clicked += Open;
        closeButton.clicked += Close;
        master.RegisterValueChangedCallback(OnMaster);
        music.RegisterValueChangedCallback(OnMusic);
        sfx.RegisterValueChangedCallback(OnSfx);
        fullScreen.RegisterValueChangedCallback(OnFullScreen);
        panel.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        panel.RegisterCallback<NavigationCancelEvent>(OnCancel);
        panel.AddToClassList("is-hidden");
        IsOpen = false;
    }

    private void OnDisable()
    {
        if (openButton == null)
            return;
        openButton.clicked -= Open;
        closeButton.clicked -= Close;
        master.UnregisterValueChangedCallback(OnMaster);
        music.UnregisterValueChangedCallback(OnMusic);
        sfx.UnregisterValueChangedCallback(OnSfx);
        fullScreen.UnregisterValueChangedCallback(OnFullScreen);
        panel.UnregisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        panel.UnregisterCallback<NavigationCancelEvent>(OnCancel);
    }

    public void Open()
    {
        if (IsOpen)
            return;
        master.SetValueWithoutNotify(GameSettings.Master);
        music.SetValueWithoutNotify(GameSettings.Music);
        sfx.SetValueWithoutNotify(GameSettings.Sfx);
        fullScreen.SetValueWithoutNotify(GameSettings.FullScreen);
        IsOpen = true;
        menuButtons.SetEnabled(false);
        panel.RemoveFromClassList("is-hidden");
        panel.schedule.Execute(() => master.Focus());
    }

    public void Close()
    {
        if (!IsOpen)
            return;
        GameSettings.Save();
        IsOpen = false;
        panel.AddToClassList("is-hidden");
        menuButtons.SetEnabled(true);
        openButton.Focus();
    }

    private void OnMaster(ChangeEvent<float> evt) => GameSettings.Master = evt.newValue;
    private void OnMusic(ChangeEvent<float> evt) { GameSettings.Music = evt.newValue; ApplyMenuMusic(); }
    private void OnSfx(ChangeEvent<float> evt) => GameSettings.Sfx = evt.newValue;
    private void OnFullScreen(ChangeEvent<bool> evt) => GameSettings.FullScreen = evt.newValue;

    private void ApplyMenuMusic()
    {
        if (menuMusic != null)
            menuMusic.volume = menuMusicBaseVolume * GameSettings.Music;
    }

    private void OnKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode != KeyCode.Escape)
            return;
        Close();
        evt.StopPropagation();
    }

    private void OnCancel(NavigationCancelEvent evt)
    {
        Close();
        evt.StopPropagation();
    }
}
