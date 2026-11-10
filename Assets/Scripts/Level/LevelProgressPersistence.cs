using System.Collections.Generic;
using UnknownGame.GameFlow;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class LevelProgressPersistence : MonoBehaviour
{
    [SerializeField] private RespawnManager respawnManager;
    [SerializeField] private RunStats stats;
    [SerializeField] private PlayerDeath player;
    [SerializeField] private LevelKey key;
    [SerializeField] private CameraFollow cameraFollow;
    [Tooltip("Checkpoints reales, ordenados del primero al último.")]
    [SerializeField] private Checkpoint[] checkpoints;

    private RunProgressService progress;
    private string activeCheckpointId;
    private bool ready;

    private void OnEnable() => Checkpoint.Reached += OnCheckpointReached;
    private void OnDisable() => Checkpoint.Reached -= OnCheckpointReached;

    private void Start()
    {
        var data = GameSession.Current;
        if (data == null || data.levelScene != gameObject.scene.name)
            return;

        if (respawnManager == null || stats == null || player == null || checkpoints == null)
        {
            Debug.LogError("Guardado: faltan referencias de los sistemas del nivel.", this);
            return;
        }

        var ids = new HashSet<string>();
        int restoredIndex = -1;
        for (int i = 0; i < checkpoints.Length; i++)
        {
            Checkpoint checkpoint = checkpoints[i];
            if (checkpoint == null || string.IsNullOrEmpty(checkpoint.PersistentId) || !ids.Add(checkpoint.PersistentId))
            {
                Debug.LogError("Guardado: hay checkpoints sin identificador o con identificadores repetidos.", this);
                return;
            }
            if (checkpoint.PersistentId == data.checkpointId)
                restoredIndex = i;
        }

        if (!string.IsNullOrEmpty(data.checkpointId) && restoredIndex < 0)
        {
            Debug.LogWarning("Guardado: el checkpoint de esta partida ya no existe. Se inicia al principio y se conserva el archivo sin actualizarlo.", this);
            return;
        }

        progress = new RunProgressService(data, GameSession.Saves);
        activeCheckpointId = data.checkpointId;
        stats.Restore(data.deaths, data.elapsedSeconds);
        if (restoredIndex >= 0)
        {
            for (int i = 0; i < checkpoints.Length; i++)
                checkpoints[i].RestoreState(i <= restoredIndex, i == restoredIndex);

            respawnManager.RestoreCheckpoint(checkpoints[restoredIndex]);
            if (data.hasSecuredKey && key != null)
                key.RestoreSecured(player.transform);
            if (cameraFollow != null)
                cameraFollow.SnapToTarget();
        }

        ready = true;
    }

    private void OnCheckpointReached(Checkpoint checkpoint)
    {
        if (!ready || System.Array.IndexOf(checkpoints, checkpoint) < 0)
            return;

        activeCheckpointId = checkpoint.PersistentId;
        SaveProgress(key != null && key.IsCollected);
    }

    public bool SaveNow()
    {
        return SaveProgress(key != null && key.IsSecured);
    }

    public bool TrySaveBeforeLeaving(out string error)
    {
        if (!ready || stats == null)
        {
            error = "El progreso del nivel no está listo para guardarse.";
            return false;
        }

        bool hasSecuredKey = stats.IsRunning
            ? key != null && key.IsSecured
            : GameSession.Current != null && GameSession.Current.hasSecuredKey;
        return progress.TrySave(activeCheckpointId, stats.Deaths, stats.ElapsedTime, hasSecuredKey, out error);
    }

    private bool SaveProgress(bool hasSecuredKey)
    {
        if (!ready || stats == null || !stats.IsRunning)
            return false;

        if (progress.TrySave(activeCheckpointId, stats.Deaths, stats.ElapsedTime, hasSecuredKey, out string error))
            return true;

        Debug.LogWarning("No se pudo guardar el progreso: " + error, this);
        return false;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            SaveNow();
    }

    private void OnApplicationQuit() => SaveNow();
}
