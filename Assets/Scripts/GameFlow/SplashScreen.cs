using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SplashScreen : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private CanvasGroup grupo;
    [SerializeField] private TMP_Text textoTerminal;
    [SerializeField] private CanvasGroup logoGrupo;
    [SerializeField] private RectTransform logoRect;

    [Header("Texto de arranque")]
    [SerializeField] private string[] lineas =
    {
        "> iniciando sistema...",
        "> cargando datos...",
        "<color=#FF3B3B>> ERROR: archivo desconocido</color>",
        "> ..."
    };
    [SerializeField] private float velocidadEscritura = 0.07f;
    [Tooltip("Tiempo que espera el > antes de empezar a escribir cada línea")]
    [SerializeField] private float pausaEntreLineas = 0.8f;

    [Header("Cursor")]
    [SerializeField] private string cursor = "_";
    [SerializeField] private float velocidadCursor = 0.5f;
    [SerializeField] private bool cursorSoloAlFinal = true;
    [SerializeField] private float pausaFinal = 1.5f;

    [Header("Logo")]
    [SerializeField] private float duracionGlitchEntrada = 1.6f;
    [SerializeField] private float tiempoVisible = 4f;
    [SerializeField] private float duracionGlitchSalida = 1.2f;
    [SerializeField] private float intensidad = 15f;

    [Header("Audio")]
    [SerializeField] private AudioSource efectos;
    [SerializeField] private AudioClip sonidoTecla;
    [SerializeField] private AudioClip sonidoGlitch;
    [SerializeField, Range(0f, 1f)] private float volumenTecla = 0.5f;
    [SerializeField, Range(0f, 0.3f)] private float variacionTono = 0.1f;

    [Header("Sonido de tecleo")]
    [Tooltip("Marcado: el sonido se repite sin cortes y se detiene justo en la última letra de cada línea")]
    [SerializeField] private bool sonidoContinuo = true;
    [Tooltip("Audio Source solo para el tecleo (necesario si Sonido Continuo está marcado)")]
    [SerializeField] private AudioSource fuenteTecleo;
    [Tooltip("Si está marcado, las líneas sin letras (como > ...) se escriben sin sonido")]
    [SerializeField] private bool silenciarLineasSinLetras = true;
    [Tooltip("Solo si Sonido Continuo está desmarcado: un solo sonido al empezar cada línea")]
    [SerializeField] private bool sonidoPorLinea = false;

    [Header("Escena")]
    [SerializeField] private string siguienteEscena = "TitleScene";

    private Vector2 posicionLogo;
    private bool saliendo;
    private string textoActual = "";
    private bool cursorActivo;
    private bool cursorVisible = true;
    private bool escribiendo;
    private bool escrituraTerminada;

    private bool ModoContinuo => sonidoContinuo && fuenteTecleo != null && sonidoTecla != null;

    private void Awake()
    {
        posicionLogo = logoRect.anchoredPosition;
    }

    private void Start()
    {
        StartCoroutine(ParpadeoCursor());
        StartCoroutine(Secuencia());
    }

    private void Update()
    {
        // Saltar con cualquier tecla
        if (!saliendo && Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            saliendo = true;
            StopAllCoroutines();
            if (fuenteTecleo != null) fuenteTecleo.Stop();
            SceneManager.LoadScene(siguienteEscena);
        }
    }

    // Dibuja el texto actual y el cursor (si toca) al final
    private void Refrescar()
    {
        bool mostrarCursor = cursorActivo && cursorVisible &&
                             (!cursorSoloAlFinal || escrituraTerminada);
        textoTerminal.text = textoActual + (mostrarCursor ? cursor : "");
    }

    private IEnumerator Secuencia()
    {
        grupo.alpha = 1f;
        logoGrupo.alpha = 0f;
        textoActual = "";
        escribiendo = false;
        escrituraTerminada = false;
        cursorActivo = true;
        Refrescar();

        yield return Escribir();

        // El cursor parpadea al final del texto
        escribiendo = false;
        escrituraTerminada = true;
        yield return new WaitForSeconds(pausaFinal);

        cursorActivo = false;
        textoActual = "";
        Refrescar();

        Sonar(sonidoGlitch, 0.8f);
        yield return Glitch(duracionGlitchEntrada, true);

        yield return EsperarConParpadeos(tiempoVisible);

        Sonar(sonidoGlitch, 0.6f);
        yield return Glitch(duracionGlitchSalida, false);

        saliendo = true;
        SceneManager.LoadScene(siguienteEscena);
    }

    private IEnumerator Escribir()
    {
        for (int n = 0; n < lineas.Length; n++)
        {
            string linea = lineas[n];
            int finPrefijo = FinDelPrefijo(linea);

            // 1) El ">" aparece primero, de golpe y sin sonido
            textoActual += linea.Substring(0, finPrefijo);
            escribiendo = false;
            Refrescar();
            yield return new WaitForSeconds(pausaEntreLineas);

            // 2) Después se escribe el resto, letra por letra
            string resto = linea.Substring(finPrefijo);
            bool conSonido = !silenciarLineasSinLetras || TieneLetras(resto);

            escribiendo = true;
            if (conSonido) IniciarSonidoLinea();

            int i = 0;
            while (i < resto.Length)
            {
                // Las etiquetas como <color=...> se agregan completas de una vez
                if (resto[i] == '<')
                {
                    int cierre = resto.IndexOf('>', i);
                    if (cierre == -1) cierre = resto.Length - 1;
                    textoActual += resto.Substring(i, cierre - i + 1);
                    i = cierre + 1;
                    continue;
                }

                textoActual += resto[i];
                Refrescar();

                // Un sonido por letra (solo si no es modo continuo ni por línea)
                if (conSonido && !ModoContinuo && !sonidoPorLinea && resto[i] != ' ')
                    SonarTecla();

                i++;

                // Si solo quedan etiquetas de cierre, esta fue la última letra
                if (SoloEtiquetas(resto, i))
                {
                    DetenerSonidoLinea();
                    textoActual += resto.Substring(i);
                    Refrescar();
                    break;
                }

                yield return new WaitForSeconds(velocidadEscritura);
            }

            DetenerSonidoLinea();
            escribiendo = false;

            // Salto de línea entre líneas (no después de la última)
            if (n < lineas.Length - 1)
            {
                textoActual += "\n";
                Refrescar();
            }
        }
    }

    // Cuántos caracteres del inicio son etiquetas y el símbolo ">" (con su espacio)
    private int FinDelPrefijo(string linea)
    {
        int i = 0;
        while (i < linea.Length && linea[i] == '<')
        {
            int cierre = linea.IndexOf('>', i);
            if (cierre == -1) break;
            i = cierre + 1;
        }

        if (i < linea.Length && linea[i] == '>')
        {
            i++;
            if (i < linea.Length && linea[i] == ' ') i++;
        }
        return i;
    }

    // ¿El texto tiene letras o números (sin contar las etiquetas)?
    private bool TieneLetras(string texto)
    {
        bool enEtiqueta = false;
        foreach (char c in texto)
        {
            if (c == '<') enEtiqueta = true;
            else if (c == '>') enEtiqueta = false;
            else if (!enEtiqueta && char.IsLetterOrDigit(c)) return true;
        }
        return false;
    }

    // ¿Desde esa posición solo quedan etiquetas?
    private bool SoloEtiquetas(string texto, int desde)
    {
        int i = desde;
        while (i < texto.Length)
        {
            if (texto[i] != '<') return false;
            int cierre = texto.IndexOf('>', i);
            if (cierre == -1) return false;
            i = cierre + 1;
        }
        return true;
    }

    private IEnumerator ParpadeoCursor()
    {
        while (true)
        {
            // Mientras escribe, el cursor queda fijo; en las pausas parpadea
            cursorVisible = escribiendo ? true : !cursorVisible;
            Refrescar();
            yield return new WaitForSeconds(velocidadCursor);
        }
    }

    private IEnumerator EsperarConParpadeos(float duracion)
    {
        float t = 0f;
        float proximoGlitch = Random.Range(0.6f, 1.4f);

        while (t < duracion)
        {
            t += Time.deltaTime;
            proximoGlitch -= Time.deltaTime;

            if (proximoGlitch <= 0f)
            {
                yield return GlitchCorto();
                proximoGlitch = Random.Range(0.8f, 1.8f);
            }
            yield return null;
        }
    }

    private IEnumerator GlitchCorto()
    {
        Sonar(sonidoGlitch, 0.3f);
        for (int i = 0; i < 4; i++)
        {
            logoGrupo.alpha = Random.Range(0.3f, 1f);
            logoRect.anchoredPosition = posicionLogo +
                new Vector2(Random.Range(-intensidad, intensidad), 0f);
            yield return new WaitForSeconds(0.04f);
        }
        logoGrupo.alpha = 1f;
        logoRect.anchoredPosition = posicionLogo;
    }

    private IEnumerator Glitch(float duracion, bool entrando)
    {
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duracion);
            float estabilidad = entrando ? p : 1f - p;

            logoGrupo.alpha = Random.value < estabilidad ? 1f : 0f;
            float temblor = (1f - estabilidad) * intensidad;
            logoRect.anchoredPosition = posicionLogo +
                new Vector2(Random.Range(-temblor, temblor), Random.Range(-temblor, temblor) * 0.3f);

            yield return null;
        }

        logoGrupo.alpha = entrando ? 1f : 0f;
        logoRect.anchoredPosition = posicionLogo;
    }

    // --- Sonido de tecleo ---

    private void IniciarSonidoLinea()
    {
        if (ModoContinuo)
        {
            fuenteTecleo.clip = sonidoTecla;
            fuenteTecleo.loop = true;
            fuenteTecleo.volume = volumenTecla;
            fuenteTecleo.pitch = 1f;
            fuenteTecleo.Play();
        }
        else if (sonidoPorLinea)
        {
            SonarTecla();
        }
    }

    private void DetenerSonidoLinea()
    {
        if (ModoContinuo)
            fuenteTecleo.Stop();
    }

    private void SonarTecla()
    {
        if (efectos == null || sonidoTecla == null) return;
        efectos.pitch = 1f + Random.Range(-variacionTono, variacionTono);
        efectos.PlayOneShot(sonidoTecla, volumenTecla);
    }

    private void Sonar(AudioClip clip, float volumen)
    {
        if (efectos == null)
        {
            Debug.LogWarning("SplashScreen: falta asignar el campo Efectos (Audio Source).");
            return;
        }
        if (clip == null) return;
        efectos.pitch = 1f;
        efectos.PlayOneShot(clip, volumen);
    }
}