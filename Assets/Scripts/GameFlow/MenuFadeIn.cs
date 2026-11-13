using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MenuFadeIn : MonoBehaviour
{
    public Color colorCortina = Color.white;  // el color con el que termina tu intro
    public float retraso = 0.3f;
    public float duracion = 4f;               // más alto = más lento

    CanvasGroup grupo;

    void Awake()
    {
        // Crea una cortina propia, por encima de cualquier otro Canvas
        var go = new GameObject("CortinaAuto");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;

        grupo = go.AddComponent<CanvasGroup>();
        grupo.alpha = 1f;
        grupo.blocksRaycasts = true;

        var imgGo = new GameObject("Color");
        imgGo.transform.SetParent(go.transform, false);
        var img = imgGo.AddComponent<Image>();
        img.color = colorCortina;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Todo el audio empieza en silencio
        AudioListener.volume = 0f;
    }

    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(retraso);

        float t = 0f;
        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duracion);
            grupo.alpha = 1f - k;
            AudioListener.volume = k;
            yield return null;
        }

        grupo.alpha = 0f;
        grupo.blocksRaycasts = false;
        AudioListener.volume = 1f;
        Destroy(grupo.gameObject);
    }
}