using TMPro;
using UnityEngine;

// Muestra el cronómetro y el contador de muertes. El contador "salta" cada vez que sube.
public class StatsHUD : MonoBehaviour
{
    [SerializeField] private RunStats stats;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text deathsText;
    [SerializeField] private bool compactFormat;
    [SerializeField] private LevelKey key;
    [SerializeField] private GameObject keyIndicator;
    [Tooltip("Escala máxima del salto del contador al morir.")]
    [SerializeField, Min(1f)] private float punchScale = 1.4f;
    [SerializeField, Min(0.01f)] private float punchDuration = 0.25f;

    private int shownDeaths = -1;
    private float punchTimer;

    private void Update()
    {
        if (stats == null)
            return;

        float time = stats.ElapsedTime;
        int minutes = (int)(time / 60f);
        float seconds = time % 60f;
        timerText.text = compactFormat ? $"{minutes:00}:{(int)seconds:00}" : $"{minutes:00}:{seconds:00.00}";

        if (keyIndicator != null)
        {
            bool hasKey = key != null && key.IsCollected;
            if (keyIndicator.activeSelf != hasKey)
                keyIndicator.SetActive(hasKey);
        }

        if (stats.Deaths != shownDeaths)
        {
            shownDeaths = stats.Deaths;
            deathsText.text = compactFormat ? shownDeaths.ToString() : $"Muertes: {shownDeaths}";
            if (shownDeaths > 0)
                punchTimer = punchDuration;
        }

        if (punchTimer > 0f)
        {
            punchTimer -= Time.deltaTime;
            float k = Mathf.Clamp01(punchTimer / punchDuration);
            deathsText.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, punchScale, k);
        }
    }
}
