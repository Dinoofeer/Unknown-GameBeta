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
        Narrator.Say("¿Cómo encontraste ese fragmento? Estaba fuera del recorrido.");
    }
}
