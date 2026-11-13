using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TitleScreen : MonoBehaviour
{
    [Header("Pantalla")]
    [SerializeField] private CanvasGroup pantalla;
    [SerializeField] private CanvasGroup textoPresiona;
    [SerializeField] private float tiempoFade = 1f;
    [SerializeField] private float velocidadParpadeo = 2f;
    [SerializeField] private string siguienteEscena = "Menu-Principal";
    [SerializeField] private LogoTitulo logo;

    [Header("Audio")]
    [SerializeField] private AudioSource musica;
    [SerializeField] private AudioSource efectos;
    [SerializeField] private AudioClip sonidoInicio;

    private bool cambiando;

    private IEnumerator Start()
    {
        pantalla.alpha = 0f;
        if (musica != null) musica.Play();
        yield return Fade(0f, 1f);
    }

    private void Update()
    {
        if (cambiando) return;

        // Parpadeo suave del texto
        float onda = Mathf.Sin(Time.time * velocidadParpadeo) * 0.5f + 0.5f;
        textoPresiona.alpha = 0.3f + 0.7f * onda;

        bool tecla = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        bool mouse = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool mando = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;

        if (tecla || mouse || mando)
            StartCoroutine(Continuar());
    }

    private IEnumerator Continuar()
{
    cambiando = true;
    if (efectos != null && sonidoInicio != null)
        efectos.PlayOneShot(sonidoInicio);

    // el logo se achica mientras la pantalla se desvanece
    if (logo != null) logo.Salir();

    yield return Fade(1f, 0f);
    SceneManager.LoadScene(siguienteEscena);
}

    private IEnumerator Fade(float desde, float hasta)
    {
        float t = 0f;
        while (t < tiempoFade)
        {
            t += Time.deltaTime;
            pantalla.alpha = Mathf.Lerp(desde, hasta, t / tiempoFade);
            yield return null;
        }
        pantalla.alpha = hasta;
    }
}