using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Muestra el cronómetro y el contador de muertes. 
// El contador "salta" cada vez que sube y la calavera cambia a su versión roja
public class StatsHUD : MonoBehaviour
{
    [SerializeField] private RunStats stats;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text deathsText;

    [Header("Calavera")]
    [SerialSizeField] private Image skullIcon;  //imagen de la calavera que cambia de color al morir
    [SerializeField] private Sprite skullNormal;
    [SerializeField] private Sprite skullHit; //calavera roja que se muestra al morir

    [Header("Salto al morir")]
    [Tooltip("Escala máxima del salto del contador al morir.")]
    [SerializeField, Min(1f)] private float punchScale = 1.4f;
    [SerializeField, Min(0.01f)] private float punchDuration = 0.25f;

    [Header("Colores (GDD)")]   //header solo indica titulo
    [SerializeField] private Color normalColor = new Color32(0xFD, 0xFE, 0xC2, 0xFF); // crema
    [SerializeField] private Color hitColor = new Color32(0xE1, 0x34, 0x48, 0xFF);    // rojo vivo

    private int shownDeaths = -1;
    private float punchTimer;

    private void Update()
    {
        if (stats == null)
            return;
        
        UpdateTimer();
        UpdateDeaths();

        // float time = stats.ElapsedTime;
        // int minutes = (int)(time / 60f);
        // float seconds = time % 60f;
        // timerText.text = $"{minutes:00}:{seconds:00.00}";

        // if (stats.Deaths != shownDeaths)
        // {
        //     shownDeaths = stats.Deaths;
        //     deathsText.text = $"Muertes: {shownDeaths}";
        //     if (shownDeaths > 0)
        //         punchTimer = punchDuration;
        // }

        // if (punchTimer > 0f)
        // {
        //     punchTimer -= Time.deltaTime;
        //     float k = Mathf.Clamp01(punchTimer / punchDuration);
        //     deathsText.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, punchScale, k);
        // }
    }

    private void UpdateTimer()
    {
        float time = stats.ElapsedTime;
        int minutes = (int)(time / 60f);
        float seconds = time % 60f;
        timerText.text = $"{minutes:00}:{seconds:00.00}"; // formato MM:SS del GDD
    }

    private void UpdateDeaths()
    {
        if (stats.Deaths != shownDeaths)
        {
            bool firstFrame = shownDeaths < 0;   // al cargar la escena no debe saltar
            shownDeaths = stats.Deaths;
            deathsText.text = shownDeaths.ToString();   // solo el número: la calavera ya dice "muertes"

            if (!firstFrame && shownDeaths > 0)
                StartPunch();
        }

        if (punchTimer <= 0f)
            return;

        // unscaled: el salto se ve aunque el juego se pause o ralentice al morir
        punchTimer -= Time.unscaledDeltaTime;
        float k = Mathf.Clamp01(punchTimer / punchDuration);
        deathsText.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, punchScale, k);

        if (punchTimer <= 0f)
            EndPunch();
    }

    private void StartPunch()
    {
        punchTimer = punchDuration;
        deathsText.color = hitColor;
        if (skullIcon != null && skullHit != null)
            skullIcon.sprite = skullHit;
    }

    private void EndPunch()
    {
        deathsText.rectTransform.localScale = Vector3.one;
        deathsText.color = normalColor;
        if (skullIcon != null && skullNormal != null)
            skullIcon.sprite = skullNormal;
    }
}
