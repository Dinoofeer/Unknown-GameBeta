using UnityEngine;

public static class GamePauseState
{
    public static bool IsPaused { get; private set; }
    public static bool BlocksGameplayInput => IsPaused || Time.frameCount <= resumeFrame;

    private static int resumeFrame = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        IsPaused = false;
        resumeFrame = -1;
    }

    internal static void SetPaused(bool paused)
    {
        IsPaused = paused;
        if (!paused)
            resumeFrame = Time.frameCount;
    }
}
