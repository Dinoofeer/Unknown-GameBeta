using System;
using UnknownGame.Save;

namespace UnknownGame.GameFlow
{
    public sealed class MenuGameFlow
    {
        private readonly SaveGameService saves;
        private readonly Func<string, bool> canLoadLevel;
        private readonly string firstLevelScene;

        public MenuGameFlow(SaveGameService saves, Func<string, bool> canLoadLevel, string firstLevelScene)
        {
            this.saves = saves;
            this.canLoadLevel = canLoadLevel;
            this.firstLevelScene = firstLevelScene;
        }

        public SaveLoadStatus GetSaveStatus() => saves.Load(out _, out _);

        public bool TryStartNew(bool replaceExisting, out SaveGameData data, out string error)
        {
            data = null;
            if (!canLoadLevel(firstLevelScene))
            {
                error = "El primer nivel no está disponible. No se ha cambiado la partida guardada.";
                return false;
            }

            SaveLoadStatus status = GetSaveStatus();
            if (status == SaveLoadStatus.IOError)
            {
                error = GetStatusMessage(status);
                return false;
            }

            if (status != SaveLoadStatus.Missing && !replaceExisting)
            {
                error = "Confirma que quieres reemplazar la partida anterior.";
                return false;
            }

            if (!saves.TryCreateNew(firstLevelScene, out data, out _))
            {
                error = "No se pudo guardar la partida nueva. Revisa el espacio y los permisos de la carpeta de guardado.";
                return false;
            }

            error = null;
            return true;
        }

        public bool TryContinue(out SaveGameData data, out string error)
        {
            SaveLoadStatus status = saves.Load(out data, out _);
            if (status != SaveLoadStatus.Loaded)
            {
                error = GetStatusMessage(status);
                return false;
            }

            if (!canLoadLevel(data.levelScene))
            {
                data = null;
                error = "El nivel de esta partida no está disponible en esta versión del juego.";
                return false;
            }

            error = null;
            return true;
        }

        public static string GetStatusMessage(SaveLoadStatus status)
        {
            switch (status)
            {
                case SaveLoadStatus.Missing:
                    return "No hay una partida guardada. Puedes empezar con Jugar.";
                case SaveLoadStatus.Invalid:
                    return "El guardado está dañado. Puedes crear una partida nueva para reemplazarlo.";
                case SaveLoadStatus.UnsupportedVersion:
                    return "El guardado pertenece a otra versión. Puedes crear una partida nueva para reemplazarlo.";
                case SaveLoadStatus.IOError:
                    return "No se pudo acceder al guardado. Revisa los permisos de su carpeta y vuelve a intentarlo.";
                default:
                    return string.Empty;
            }
        }
    }
}
