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
                hasSecuredKey = false
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
}
