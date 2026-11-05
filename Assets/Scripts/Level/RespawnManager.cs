using System;
using System.Collections;
using UnityEngine;

// Mecánicas 04 y 05 (GDD): guarda el punto de reaparición y devuelve al jugador tras morir.
// Uno por escena de nivel.
public class RespawnManager : MonoBehaviour
{
    // Avisa a las trampas de que vuelvan a su estado inicial (bloques invisibles, plataformas que caen...).
    public static event Action PlayerRespawned;

    [SerializeField] private PlayerDeath player;
    [Tooltip("Punto de reaparición inicial. Si está vacío, se usa la posición inicial del jugador.")]
    [SerializeField] private Transform levelStart;
    [Tooltip("Segundos entre la muerte y la reaparición. GDD: menos de 1 s.")]
    [SerializeField, Range(0f, 1f)] private float respawnDelay = 0.5f;

    private Vector2 respawnPoint;
    private Checkpoint activeCheckpoint;

    private void Awake()
    {
        if (player == null)
        {
            Debug.LogError($"{name}: falta asignar Player en RespawnManager.", this);
            enabled = false;
            return;
        }
        respawnPoint = levelStart != null ? levelStart.position : player.transform.position;
    }

    private void OnEnable()
    {
        PlayerDeath.Died += OnPlayerDied;
        Checkpoint.Reached += OnCheckpointReached;
    }

    private void OnDisable()
    {
        PlayerDeath.Died -= OnPlayerDied;
        Checkpoint.Reached -= OnCheckpointReached;
    }

    private void OnCheckpointReached(Checkpoint checkpoint)
    {
        // GDD: solo un checkpoint activo; el nuevo sobrescribe al anterior.
        if (activeCheckpoint != null)
            activeCheckpoint.SetActiveVisual(false);

        activeCheckpoint = checkpoint;
        respawnPoint = checkpoint.SpawnPosition;
        checkpoint.SetActiveVisual(true);
    }

    private void OnPlayerDied(Vector2 deathPosition)
    {
        StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        // El punto se lee ahora y no al morir: si en el mismo frame se tocó un checkpoint,
        // ya cuenta (caso límite del GDD).
        PlayerRespawned?.Invoke();
        player.Respawn(respawnPoint);
    }
}
