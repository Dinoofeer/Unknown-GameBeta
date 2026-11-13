using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

[System.Serializable]
public class EscenaIntro
{
    public Sprite imagen;
    public VideoClip video;   // opcional: si tiene video, se usa en lugar de la imagen
    [TextArea(2, 5)] public string texto;
    public AudioClip narracion;
    public float duracion = 6f;
}

public class IntroManager : MonoBehaviour
{
    public Image pantalla;
    public GameObject pantallaVideo;
    public VideoPlayer reproductorVideo;
    public CanvasGroup fade;
    public TMP_Text subtitulo;
    public RectTransform fondoSubtitulo;
    public Vector2 margenFondo = new Vector2(40, 20);
    public AudioSource voz;
    public AudioSource musica;
    public EscenaIntro[] escenas;
    public Material materialSubtitulo;
    public string siguienteEscena = "Menu-Principal";

    public float duracionFade = 2f;
    public float pausaNegro = 0.8f;
    public float velocidadTexto = 0.04f;
    public float volumenMusica = 0.35f;
    public float duracionFadeMusica = 3f;
    public float fadeFinalVideo = 1.5f;   // ya no se usa: ahora hay destello

    [Header("Destello al terminar el video")]
    public Color colorFlash = Color.white;
    public float duracionFlash = 0.25f;   // qué tan rápido llega el destello
    public float pausaFlash = 0.15f;      // cuánto se queda en pantalla

    [Header("Sonido del botón Saltar")]
    public AudioSource efectos;
    public AudioClip sonidoClick;

    bool saltando;
    bool videoTerminado;

    void Start()
    {
        if (materialSubtitulo != null)
        subtitulo.fontMaterial = materialSubtitulo;

        fade.alpha = 1;
        musica.volume = 0;
        musica.loop = true;
        musica.Play();
        pantallaVideo.SetActive(false);
        StartCoroutine(FadeMusica(0, volumenMusica, duracionFadeMusica));
        StartCoroutine(ReproducirIntro());
    }

    void Update()
    {
        if (saltando || Keyboard.current == null) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame ||
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            saltando = true;
            StopAllCoroutines();
            StartCoroutine(SalirRapido());
        }
    }

    // Lo llama el botón Saltar
    public void Saltar()
    {
        if (saltando) return;
        saltando = true;
        StopAllCoroutines();

        if (reproductorVideo != null)
            reproductorVideo.Stop();

        if (efectos != null && sonidoClick != null)
            efectos.PlayOneShot(sonidoClick);

        StartCoroutine(SalirRapido());
    }

    IEnumerator SalirRapido()
    {
        StartCoroutine(Fade(fade.alpha, 1, 1f));
        yield return FadeMusica(musica.volume, 0, 1f);
        SceneManager.LoadScene(siguienteEscena);
    }

    IEnumerator ReproducirIntro()
    {
        // Preparar el primer video desde el inicio para evitar tirones
        foreach (var esc in escenas)
        {
            if (esc.video != null)
            {
                reproductorVideo.clip = esc.video;
                reproductorVideo.playOnAwake = false;
                reproductorVideo.waitForFirstFrame = true;
                reproductorVideo.Prepare();
                break;
            }
        }

        for (int i = 0; i < escenas.Length; i++)
        {
            var e = escenas[i];
            bool hayVideo = e.video != null;
            bool esUltima = i == escenas.Length - 1;

            subtitulo.text = e.texto;
            subtitulo.maxVisibleCharacters = 0;

            if (hayVideo)
            {
                if (reproductorVideo.clip != e.video)
                {
                    reproductorVideo.clip = e.video;
                    reproductorVideo.Prepare();
                }
                while (!reproductorVideo.isPrepared) yield return null;

                // Dibuja el primer cuadro mientras todo sigue en negro
                reproductorVideo.Play();
                reproductorVideo.Pause();
                reproductorVideo.time = 0;
                yield return null;
                yield return null;

                pantalla.gameObject.SetActive(false);
                pantallaVideo.SetActive(true);
            }
            else
            {
                pantallaVideo.SetActive(false);
                pantalla.gameObject.SetActive(true);
                pantalla.sprite = e.imagen;
            }

            yield return Fade(1, 0, duracionFade);

            if (hayVideo)
            {
                videoTerminado = false;
                reproductorVideo.loopPointReached += AlTerminarVideo;
                reproductorVideo.Play();
            }

            StartCoroutine(Escribir(e.texto));

            if (e.narracion != null)
            {
                voz.clip = e.narracion;
                voz.Play();
            }

            if (hayVideo)
            {
                // Esperar a que el video termine de verdad
                while (!videoTerminado) yield return null;
                reproductorVideo.loopPointReached -= AlTerminarVideo;

                if (esUltima)
                {
                    // Destello y paso directo al menú
                    StartCoroutine(FadeMusica(musica.volume, 0, duracionFlash + pausaFlash));
                    yield return Destello();
                    SceneManager.LoadScene(siguienteEscena);
                    yield break;
                }
            }
            else
            {
                float espera = e.narracion != null ? e.narracion.length : e.duracion;
                yield return new WaitForSeconds(espera);
            }

            yield return Fade(0, 1, duracionFade);
            if (hayVideo) reproductorVideo.Stop();
            yield return new WaitForSeconds(pausaNegro);
        }

        yield return FadeMusica(musica.volume, 0, duracionFadeMusica);
        SceneManager.LoadScene(siguienteEscena);
    }

    void AlTerminarVideo(VideoPlayer vp)
    {
        videoTerminado = true;
    }

    // El fundido pasa a ser un destello del color elegido
    IEnumerator Destello()
    {
        Image imagenFade = fade.GetComponentInChildren<Image>();
        if (imagenFade != null)
            imagenFade.color = new Color(colorFlash.r, colorFlash.g, colorFlash.b, 1f);

        yield return Fade(0, 1, duracionFlash);
        yield return new WaitForSeconds(pausaFlash);
    }

    IEnumerator Escribir(string texto)
    {
        subtitulo.text = texto;
        subtitulo.maxVisibleCharacters = 0;
        for (int i = 0; i <= texto.Length; i++)
        {
            subtitulo.maxVisibleCharacters = i;
            yield return new WaitForSeconds(velocidadTexto);
        }
    }

    IEnumerator Fade(float desde, float hasta, float duracion)
    {
        float t = 0;
        while (t < duracion)
        {
            t += Time.deltaTime;
            fade.alpha = Mathf.SmoothStep(desde, hasta, t / duracion);
            yield return null;
        }
        fade.alpha = hasta;
    }

    IEnumerator FadeMusica(float desde, float hasta, float duracion)
    {
        float t = 0;
        while (t < duracion)
        {
            t += Time.deltaTime;
            musica.volume = Mathf.Lerp(desde, hasta, t / duracion);
            yield return null;
        }
        musica.volume = hasta;
    }
}