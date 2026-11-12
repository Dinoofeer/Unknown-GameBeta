using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(AudioSource))]
public class MenuAudioButton : MonoBehaviour
{
    public AudioClip clickSound;
    public AudioClip moveSound;   // al moverte con flechas / WASD
    private AudioSource audioSource;
    private UIDocument uiDocument;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.ignoreListenerPause = true;
        audioSource.playOnAwake = false;
        uiDocument = GetComponent<UIDocument>();

        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;

            // al moverte con flechas o WASD
            root.RegisterCallback<NavigationMoveEvent>(
                e => Play(moveSound), TrickleDown.TrickleDown);

            // al pulsar un botón 
            var buttons = root.Query<Button>().ToList();
            foreach (var button in buttons)
            {
                button.clicked += () => Play(clickSound);
            }
        }
    }

    private void Play(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}