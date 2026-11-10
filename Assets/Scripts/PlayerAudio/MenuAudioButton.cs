using UnityEngine;
using UnityEngine.UIElements;

public class MenuAudioButton : MonoBehaviour
{
    public AudioClip clickSound;
    private AudioSource audioSource;
    private UIDocument uiDocument;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        uiDocument = GetComponent<UIDocument>();

        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;
            var buttons = root.Query<Button>().ToList();

            foreach (var button in buttons)
            {
                button.clicked += () => {
                    if (audioSource != null && clickSound != null)
                    {
                        audioSource.PlayOneShot(clickSound);
                    }
                };
            }
        }
    }
}