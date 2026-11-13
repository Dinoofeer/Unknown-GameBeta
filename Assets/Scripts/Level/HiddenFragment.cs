using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class HiddenFragment : MonoBehaviour
{
    [SerializeField] private AudioClip pickupClip;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        gameObject.SetActive(false);
        SfxPlayer.Play(pickupClip);
        // Sin frase de Sistema.exe: el GDD no define ninguna para el coleccionable oculto.
    }
}
