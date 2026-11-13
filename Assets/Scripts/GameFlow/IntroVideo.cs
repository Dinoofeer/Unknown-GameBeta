using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class IntroVideo : MonoBehaviour
{
    [SerializeField] private VideoPlayer video;
    [SerializeField] private string siguienteEscena = "Menu-Principal";
    [SerializeField] private bool soloPrimeraVez = true;
    [SerializeField] private bool saltarConCualquierTecla = false;

    [Header("Sonido del botón")]
    [SerializeField] private AudioSource efectos;
    [SerializeField] private AudioClip sonidoClick;
    [SerializeField] private float esperaTrasClick = 0.3f;

    private const string Clave = "IntroVista";
    private bool saliendo;

    private void Start()
    {
        // Si ya la vio antes, pasa directo al menú
        if (soloPrimeraVez && PlayerPrefs.GetInt(Clave, 0) == 1)
        {
            Terminar();
            return;
        }

        video.loopPointReached += _ => Terminar();
        video.Play();
    }

    private void Update()
    {
        if (saliendo || !saltarConCualquierTecla) return;

        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            Terminar();
    }

    // Lo llama el botón Saltar
    public void Saltar()
    {
        if (saliendo) return;
        saliendo = true;
        StartCoroutine(SaltarConSonido());
    }

    private IEnumerator SaltarConSonido()
    {
        video.Stop();

        if (efectos != null && sonidoClick != null)
            efectos.PlayOneShot(sonidoClick);

        yield return new WaitForSeconds(esperaTrasClick);
        Cargar();
    }

    // Fin del video, ya vista o tecla
    private void Terminar()
    {
        if (saliendo) return;
        saliendo = true;
        Cargar();
    }

    private void Cargar()
    {
        PlayerPrefs.SetInt(Clave, 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(siguienteEscena);
    }
}