using System;

namespace UnknownGame.Save
{
    [Serializable]
    public sealed class SaveGameData
    {
        public const int CurrentVersion = 2;

        public int version;
        public string runId;
        public string levelScene;
        public string checkpointId;
        public int deaths;
        public float elapsedSeconds;
        public bool hasSecuredKey;
        public LevelScoreProgress levelScores;

        public static SaveGameData CreateNew(string levelScene)
        {
            return new SaveGameData
            {
                version = CurrentVersion,
                runId = Guid.NewGuid().ToString("N"),
                levelScene = levelScene,
                checkpointId = string.Empty,
                deaths = 0,
                elapsedSeconds = 0f,
                hasSecuredKey = false,
                levelScores = new LevelScoreProgress { hasStart = true, results = Array.Empty<LevelScore>() }
            };
        }

        public bool IsValid(out string error)
        {
            if (version != CurrentVersion)
                error = "La versión del guardado no es compatible.";
            else if (!Guid.TryParseExact(runId, "N", out _))
                error = "El identificador de partida no es válido.";
            else if (!IsIdentifier(levelScene, false) || !IsIdentifier(checkpointId, true))
                error = "El nivel o el identificador del checkpoint no es válido.";
            else if (deaths < 0 || elapsedSeconds < 0f || float.IsNaN(elapsedSeconds)
                     || float.IsInfinity(elapsedSeconds))
                error = "Las estadísticas de la partida no son válidas.";
            else
            {
                error = null;
                return true;
            }

            return false;
        }

        private static bool IsIdentifier(string value, bool allowEmpty)
        {
            if (value == null || value.Length > 128 || (!allowEmpty && value.Length == 0))
                return false;

            foreach (char character in value)
            {
                if (!(character >= 'a' && character <= 'z')
                    && !(character >= 'A' && character <= 'Z')
                    && !(character >= '0' && character <= '9')
                    && character != '_' && character != '-')
                    return false;
            }

            return true;
        }
    }

    [Serializable]
    public sealed class LevelScoreProgress
    {
        public bool hasStart;
        public int startDeaths;
        public float startSeconds;
        public LevelScore[] results;
    }

    [Serializable]
    public sealed class LevelScore
    {
        public string scene;
        public int deaths;
        public float timeSeconds;

        public static string NameForScene(string scene) => scene switch
        {
            "Level_01" => "Nivel 1 - El despertar",
            "Level_02" => "Nivel 2 - Desafío de datos",
            "Level_03" => "Nivel 3 - El umbral de salida",
            _ => null
        };
    }
}
