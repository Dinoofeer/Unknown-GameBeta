using System;
using System.IO;
using System.Security;
using System.Text;
using UnityEngine;

namespace UnknownGame.Save
{
    public enum SaveLoadStatus
    {
        Missing,
        Loaded,
        Invalid,
        UnsupportedVersion,
        IOError
    }

    public sealed class SaveGameService
    {
        public const string FileName = "partida.json";
        public string SaveFilePath { get; }

        public SaveGameService(string directory = null)
        {
            if (directory != null && string.IsNullOrWhiteSpace(directory))
                throw new ArgumentException("La carpeta de guardado está vacía.", nameof(directory));

            directory = directory ?? Path.Combine(Application.persistentDataPath, "Save");
            SaveFilePath = Path.Combine(Path.GetFullPath(directory), FileName);
        }

        public SaveLoadStatus Load(out SaveGameData data, out string error)
        {
            data = null;
            error = null;
            string json;
            try
            {
                json = File.ReadAllText(SaveFilePath, Encoding.UTF8).Trim();
            }
            catch (FileNotFoundException)
            {
                return SaveLoadStatus.Missing;
            }
            catch (DirectoryNotFoundException)
            {
                return SaveLoadStatus.Missing;
            }
            catch (Exception exception) when (IsFileError(exception))
            {
                error = exception.Message;
                return SaveLoadStatus.IOError;
            }

            if (!json.StartsWith("{", StringComparison.Ordinal) || !json.EndsWith("}", StringComparison.Ordinal))
            {
                error = "El archivo de guardado no contiene un objeto JSON completo.";
                return SaveLoadStatus.Invalid;
            }

            SaveGameData loaded;
            try
            {
                loaded = JsonUtility.FromJson<SaveGameData>(json);
            }
            catch (ArgumentException exception)
            {
                error = exception.Message;
                return SaveLoadStatus.Invalid;
            }

            if (loaded != null && loaded.version > 0 && loaded.version != SaveGameData.CurrentVersion)
            {
                error = "La versión del guardado no es compatible.";
                return SaveLoadStatus.UnsupportedVersion;
            }

            if (loaded == null)
            {
                error = "El archivo de guardado está vacío.";
                return SaveLoadStatus.Invalid;
            }

            if (!loaded.IsValid(out error))
                return SaveLoadStatus.Invalid;

            data = loaded;
            return SaveLoadStatus.Loaded;
        }

        public bool TryCreateNew(string levelScene, out SaveGameData data, out string error)
        {
            SaveGameData candidate = SaveGameData.CreateNew(levelScene);
            if (!TrySave(candidate, out error))
            {
                data = null;
                return false;
            }

            data = candidate;
            return true;
        }

        public bool TrySave(SaveGameData data, out string error)
        {
            if (data == null)
            {
                error = "No hay datos de partida para guardar.";
                return false;
            }

            if (!data.IsValid(out error))
                return false;

            string temporaryPath = SaveFilePath + ".tmp";
            try
            {
                string json = JsonUtility.ToJson(data, true);
                Directory.CreateDirectory(Path.GetDirectoryName(SaveFilePath));
                using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, true))
                    {
                        writer.Write(json);
                        writer.Flush();
                    }
                    stream.Flush(true);
                }

                // La partida anterior se sustituye solo cuando la nueva escritura ha terminado.
                if (File.Exists(SaveFilePath))
                    File.Replace(temporaryPath, SaveFilePath, null);
                else
                    File.Move(temporaryPath, SaveFilePath);

                return true;
            }
            catch (Exception exception) when (IsFileError(exception))
            {
                error = exception.Message;
                return false;
            }
            finally
            {
                try
                {
                    if (File.Exists(temporaryPath))
                        File.Delete(temporaryPath);
                }
                catch (Exception exception) when (IsFileError(exception))
                {
                    // Un temporal que no pueda retirarse nunca se considera una partida al cargar.
                }
            }
        }

        private static bool IsFileError(Exception exception)
        {
            return exception is IOException || exception is UnauthorizedAccessException
                || exception is SecurityException || exception is NotSupportedException;
        }
    }
}
