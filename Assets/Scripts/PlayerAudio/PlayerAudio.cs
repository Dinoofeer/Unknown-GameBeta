using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerAudio : MonoBehaviour
{
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip doubleJumpClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField, Range(0f, 10f)] private float jumpVolume = 8f;
    [SerializeField, Range(0f, 10f)] private float deathVolume = 1f;

    private AudioSource source;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
    }

    public void PlayJump()
    {
        if (jumpClip != null) source.PlayOneShot(jumpClip);

        Vector3 pos = Camera.main != null ? Camera.main.transform.position : transform.position;
        AudioSource.PlayClipAtPoint(jumpClip, pos, jumpVolume);
    }

    public void PlayDoubleJump()
    {
        if (doubleJumpClip != null) source.PlayOneShot(doubleJumpClip);
        
    }

    public void PlayDeath()
    {
        if (deathClip == null) return;

        Vector3 pos = Camera.main != null ? Camera.main.transform.position : transform.position;
        AudioSource.PlayClipAtPoint(deathClip, pos, deathVolume);
    }
}