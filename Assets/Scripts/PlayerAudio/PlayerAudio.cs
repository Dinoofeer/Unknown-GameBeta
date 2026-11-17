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
        // Volumen de efectos de la Configuración (GDD, Datos y Persistencia).
        if (jumpClip != null) source.PlayOneShot(jumpClip, GameSettings.Sfx);

        Vector3 pos = Camera.main != null ? Camera.main.transform.position : transform.position;
        AudioSource.PlayClipAtPoint(jumpClip, pos, jumpVolume * GameSettings.Sfx);
    }

    public void PlayDoubleJump()
    {
        if (doubleJumpClip != null) source.PlayOneShot(doubleJumpClip, GameSettings.Sfx);

    }

    public void PlayDeath()
    {
        if (deathClip == null) return;

        // 2D y en una fuente persistente: la cámara salta al checkpoint a los 0,15 s y no debe acelerarlo ni cortarlo.
        SfxPlayer.Play2D(deathClip, deathVolume);
    }
}