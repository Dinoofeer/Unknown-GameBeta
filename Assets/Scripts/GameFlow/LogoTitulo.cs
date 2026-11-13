using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class LogoTitulo : MonoBehaviour
{
    [Header("Entrada (viene del splash)")]
    [SerializeField] private float retrasoEntrada = 0.3f;
    [SerializeField] private float duracionEntrada = 1.8f;
    [SerializeField] private float escalaInicial = 0.2f;

    [Header("Salida (al presionar una tecla)")]
    [SerializeField] private float duracionSalida = 0.8f;
    [SerializeField] private float escalaFinal = 0.2f;

    private CanvasGroup grupo;
    private RectTransform rt;
    private Vector3 escalaBase;

    private void Awake()
    {
        grupo = GetComponent<CanvasGroup>();
        rt = GetComponent<RectTransform>();
        escalaBase = rt.localScale;

        // Empieza chico e invisible
        rt.localScale = escalaBase * escalaInicial;
        grupo.alpha = 0f;
    }

    private void Start()
    {
        StartCoroutine(Entrada());
    }

    public void Salir()
    {
        StopAllCoroutines();
        StartCoroutine(Salida());
    }

    private IEnumerator Entrada()
    {
        yield return new WaitForSeconds(retrasoEntrada);
        yield return Animar(escalaInicial, 1f, 0f, 1f, duracionEntrada);
    }

    private IEnumerator Salida()
    {
        float escalaActual = rt.localScale.x / escalaBase.x;
        yield return Animar(escalaActual, escalaFinal, grupo.alpha, 0f, duracionSalida);
    }

    private IEnumerator Animar(float escalaDesde, float escalaHasta,
                                float alphaDesde, float alphaHasta, float duracion)
    {
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / duracion);
            rt.localScale = escalaBase * Mathf.Lerp(escalaDesde, escalaHasta, p);
            grupo.alpha = Mathf.Lerp(alphaDesde, alphaHasta, p);
            yield return null;
        }
        rt.localScale = escalaBase * escalaHasta;
        grupo.alpha = alphaHasta;
    }
}