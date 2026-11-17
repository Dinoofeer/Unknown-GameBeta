using System;
using System.IO;
using System.Text;
using UnityEngine;

// GDD (Datos y Persistencia › configuracion): volumen general, de música y de efectos (0 a 1)
// y pantalla completa. Se guarda en JSON con escritura segura (.tmp) y réplica .bak,
// y se aplica al arrancar el juego.
// Va en un archivo propio para que "Crear partida" (que reemplaza la partida) no borre los ajustes.
public static class GameSettings
{
    [Serializable]
    private class Data
    {
        public int version = 1;
        public float volumenMaster = 1f;
        public float volumenMusica = 1f;
        public float volumenSFX = 1f;
        public bool pantallaCompleta = true;
    }

    public const string FileName = "configuracion.json";
    private static Data data;

    public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static float Master { get { Ensure(); return data.volumenMaster; } set { Ensure(); data.volumenMaster = Mathf.Clamp01(value); Apply(); } }
    public static float Music { get { Ensure(); return data.volumenMusica; } set { Ensure(); data.volumenMusica = Mathf.Clamp01(value); } }
    public static float Sfx { get { Ensure(); return data.volumenSFX; } set { Ensure(); data.volumenSFX = Mathf.Clamp01(value); } }
    public static bool FullScreen { get { Ensure(); return data.pantallaCompleta; } set { Ensure(); data.pantallaCompleta = value; Apply(); } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void LoadAtStartup()
    {
        data = null;
        Ensure();
        Apply();
    }

    private static void Ensure()
    {
        if (data != null)
            return;

        data = new Data();
        try
        {
            if (File.Exists(FilePath))
                JsonUtility.FromJsonOverwrite(File.ReadAllText(FilePath, Encoding.UTF8), data);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Configuración: no se pudo leer, se usan los valores por defecto. " + exception.Message);
            data = new Data();
        }
    }

    public static void Apply()
    {
        Ensure();
        AudioListener.volume = data.volumenMaster;
        if (Screen.fullScreen != data.pantallaCompleta)
            Screen.fullScreen = data.pantallaCompleta;
    }

    public static bool Save()
    {
        Ensure();
        string temporaryPath = FilePath + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(data, true), new UTF8Encoding(false));
            if (File.Exists(FilePath))
                File.Replace(temporaryPath, FilePath, Path.ChangeExtension(FilePath, ".bak"));
            else
                File.Move(temporaryPath, FilePath);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Configuración: no se pudo guardar. " + exception.Message);
            try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); } catch (Exception) { }
            return false;
        }
    }
}
