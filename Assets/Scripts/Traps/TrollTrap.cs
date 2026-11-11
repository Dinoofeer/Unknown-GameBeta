using UnityEngine;

// Registra cuándo el jugador acaba de caer en una trampa troll (GDD, Diálogos y Narrativa):
// plataforma que cae, checkpoint falso o cualquier cosa que le hace creer que está a salvo.
// Los pinchos y las caídas al vacío visibles no cuentan. Lo usa el Sistema para su burla única.
public static class TrollTrap
{
    // Una muerte cuenta como troll si ocurre hasta estos segundos después de activar una trampa troll.
    public const float Window = 2f;

    private static float lastActivation = float.NegativeInfinity;

    public static void Mark() => lastActivation = Time.time;

    public static void Clear() => lastActivation = float.NegativeInfinity;

    public static bool WasRecent => Time.time - lastActivation <= Window;
}
