using UnityEngine;

public static class SfxPlayer
{
    private static AudioSource source2D;

    public static void Play(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        Vector3 pos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        // Volumen de efectos de la Configuración (GDD, Datos y Persistencia).
        AudioSource.PlayClipAtPoint(clip, pos, volume * GameSettings.Sfx);
    }

    // Sonido 2D (sin posición ni efecto Doppler) en una fuente que sobrevive a la reaparición y a los cambios
    // de escena. GDD (Audio y Música › Audio espacial): estéreo uniforme 2D, sin audio posicional.
    // Se usa para la muerte: con la reaparición en 0,15 s la cámara salta al checkpoint mientras el sonido
    // aún suena, y un sonido 3D se oía acelerado (Doppler) y cortado por la distancia.
    public static void Play2D(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        if (source2D == null)
        {
            var go = new GameObject("SfxPlayer2D");
            Object.DontDestroyOnLoad(go);
            source2D = go.AddComponent<AudioSource>();
            source2D.playOnAwake = false;
            source2D.spatialBlend = 0f;
            source2D.dopplerLevel = 0f;
            source2D.pitch = 1f;
        }
        // Tope en 1, como hacía PlayClipAtPoint (AudioSource.volume no pasa de 1): más amplificaría y saturaría.
        source2D.PlayOneShot(clip, Mathf.Min(volume, 1f) * GameSettings.Sfx);
    }
}
