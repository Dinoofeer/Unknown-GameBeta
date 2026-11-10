using UnityEngine;

public static class SfxPlayer
{
    public static void Play(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        Vector3 pos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        AudioSource.PlayClipAtPoint(clip, pos, volume);
    }
}