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
    [Tooltip("Segundos entre la muerte y la reaparición. GDD (Alcance y Requisitos Técnicos): menos de 0,2 s.")]
    [SerializeField, Range(0f, 0.19f)] private float respawnDelay = 0.15f;

    private Vector2 respawnPoint;
    private Checkpoint activeCheckpoint;
    private Coroutine pendingRespawn;

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
        CancelPendingRespawn();
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

    public void RestoreCheckpoint(Checkpoint checkpoint)
    {
        OnCheckpointReached(checkpoint);
        CancelPendingRespawn();
        player.Respawn(respawnPoint);
    }

    public bool RestartFromCheckpoint()
    {
        if (!isActiveAndEnabled || player == null)
            return false;

        CancelPendingRespawn();
        RespawnAtCurrentPoint();
        return true;
    }

    private void OnPlayerDied(Vector2 deathPosition)
    {
        CancelPendingRespawn();
        pendingRespawn = StartCoroutine(RespawnAfterDelay());
    }

    private void CancelPendingRespawn()
    {
        if (pendingRespawn == null)
            return;

        StopCoroutine(pendingRespawn);
        pendingRespawn = null;
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        // El punto se lee ahora y no al morir: si en el mismo frame se tocó un checkpoint,
        // ya cuenta (caso límite del GDD).
        pendingRespawn = null;
        RespawnAtCurrentPoint();
    }

    private void RespawnAtCurrentPoint()
    {
        player.Respawn(respawnPoint);
        PlayerRespawned?.Invoke();
    }
}
