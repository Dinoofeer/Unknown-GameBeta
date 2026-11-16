using System;
using UnknownGame.Save;

namespace UnknownGame.GameFlow
{
    public sealed class RunProgressService
    {
        private readonly SaveGameData current;
        private readonly SaveGameService saves;

        public RunProgressService(SaveGameData current, SaveGameService saves)
        {
            if (current == null || !current.IsValid(out _))
                throw new ArgumentException("La partida activa no es válida.", nameof(current));
            this.current = current;
            this.saves = saves ?? throw new ArgumentNullException(nameof(saves));
        }

        public bool TrySave(string checkpointId, int deaths, float elapsedSeconds, bool hasSecuredKey, out string error)
        {
            if (!TryWrite(current.levelScene, checkpointId, deaths, elapsedSeconds, hasSecuredKey, false, out var snapshot, out error))
                return false;

            current.checkpointId = snapshot.checkpointId;
            current.deaths = snapshot.deaths;
            current.elapsedSeconds = snapshot.elapsedSeconds;
            current.hasSecuredKey = snapshot.hasSecuredKey;
            current.levelScores = snapshot.levelScores;
            return true;
        }

        public bool TryAdvance(string nextLevelScene, int deaths, float elapsedSeconds, out SaveGameData next, out string error)
        {
            return TryWrite(nextLevelScene, string.Empty, deaths, elapsedSeconds, false, true, out next, out error);
        }

        private bool TryWrite(string levelScene, string checkpointId, int deaths, float elapsedSeconds,
            bool hasSecuredKey, bool advancing, out SaveGameData snapshot, out string error)
        {
            snapshot = null;
            if (saves.Load(out var stored, out error) != SaveLoadStatus.Loaded)
            {
                error = "No se pudo leer la partida activa para actualizar su progreso. " + error;
                return false;
            }

            if (stored.runId != current.runId || stored.levelScene != current.levelScene)
            {
                error = "La partida guardada ha cambiado. No se sobrescribirá con el progreso de otra partida.";
                return false;
            }

            var candidate = new SaveGameData
            {
                version = current.version,
                runId = current.runId,
                levelScene = levelScene,
                checkpointId = checkpointId,
                deaths = deaths,
                elapsedSeconds = elapsedSeconds,
                hasSecuredKey = !string.IsNullOrEmpty(checkpointId) && hasSecuredKey,
                levelScores = advancing && levelScene != stored.levelScene
                    ? RecordLevel(stored, deaths, elapsedSeconds) : stored.levelScores
            };

            if (!saves.TrySave(candidate, out error))
                return false;

            snapshot = candidate;
            return true;
        }

        private static LevelScoreProgress RecordLevel(SaveGameData stored, int deaths, float elapsedSeconds)
        {
            var progress = stored.levelScores;
            var results = progress?.results ?? Array.Empty<LevelScore>();
            bool knowsStart = (progress != null && progress.hasStart) || stored.levelScene == "Level_01";
            int startDeaths = progress?.startDeaths ?? 0;
            float startSeconds = progress?.startSeconds ?? 0f;

            // Un guardado anterior puede tener totales de otros niveles sin un punto de partida fiable.
            if (knowsStart && LevelScore.NameForScene(stored.levelScene) != null &&
                startDeaths >= 0 && startDeaths <= deaths && startSeconds >= 0f && startSeconds <= elapsedSeconds)
            {
                Array.Resize(ref results, results.Length + 1);
                results[results.Length - 1] = new LevelScore
                {
                    scene = stored.levelScene,
                    deaths = deaths - startDeaths,
                    timeSeconds = elapsedSeconds - startSeconds
                };
            }

            return new LevelScoreProgress { hasStart = true, startDeaths = deaths, startSeconds = elapsedSeconds, results = results };
        }
    }
}
