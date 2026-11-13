using UnityEngine;

public static class SfxPlayer
{
    private static AudioSource source;
    public static void Play(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        if (source == null)
        {
            GameObject go = new GameObject("SfxPlayer");
            Object.DontDestroyOnLoad(go);
            source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D
        }
        source.PlayOneShot(clip, volume);

        // Vector3 pos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        // AudioSource.PlayClipAtPoint(clip, pos, volume);
    }
}