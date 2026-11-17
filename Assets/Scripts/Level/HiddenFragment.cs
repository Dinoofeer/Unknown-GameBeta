using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public sealed class HiddenFragment : MonoBehaviour
{
    [SerializeField] private AudioClip pickupClip;
    [SerializeField] private UnityEvent onCollected;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActiveAndEnabled || !other.CompareTag("Player"))
            return;

        gameObject.SetActive(false);
        SfxPlayer.Play(pickupClip);
        onCollected?.Invoke();
    }
}
