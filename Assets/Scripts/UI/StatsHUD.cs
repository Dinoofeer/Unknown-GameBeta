using TMPro;
using UnityEngine;

public class StatsHUD : MonoBehaviour
{
    [SerializeField] private RunStats stats;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text deathsText;
    [SerializeField] private bool compactFormat;
    [SerializeField] private LevelKey key;
    [SerializeField] private GameObject keyIndicator;

    [Header("Calavera")]
    [SerializeField] private UnityEngine.UI.Image skullIcon;
    [SerializeField] private Sprite skullNormal;
    [SerializeField] private Sprite skullHit;

    [Header("Salto al morir")]
    [Tooltip("Escala máxima del salto del contador al morir.")]
    [SerializeField, Min(1f)] private float punchScale = 1.4f;
    [SerializeField, Min(0.01f)] private float punchDuration = 0.25f;

    [Header("Colores (GDD)")]
    [SerializeField] private Color normalColor = new Color32(0xFD, 0xFE, 0xC2, 0xFF);
    [SerializeField] private Color hitColor = new Color32(0xE1, 0x34, 0x48, 0xFF);

    private int shownDeaths = -1;
    private float punchTimer;
    private Color skullColor;

    private void Awake()
    {
        if (skullIcon != null)
            skullColor = skullIcon.color;
    }

    private void Update()
    {
        if (stats == null)
            return;

        UpdateTimer();
        UpdateDeaths();

        if (keyIndicator != null)
        {
            bool hasKey = key != null && key.IsCollected;
            if (keyIndicator.activeSelf != hasKey)
                keyIndicator.SetActive(hasKey);
        }
    }

    private void UpdateTimer()
    {
        float time = stats.ElapsedTime;
        int minutes = (int)(time / 60f);
        float seconds = time % 60f;
        timerText.text = compactFormat ? $"{minutes:00}:{(int)seconds:00}" : $"{minutes:00}:{seconds:00.00}";
    }

    private void UpdateDeaths()
    {
        if (stats.Deaths != shownDeaths)
        {
            bool deathAdded = shownDeaths >= 0 && stats.Deaths > shownDeaths;
            shownDeaths = stats.Deaths;
            deathsText.text = compactFormat || skullIcon != null ? shownDeaths.ToString() : $"Muertes: {shownDeaths}";

            if (deathAdded)
            {
                punchTimer = punchDuration;
                SetDeathColors(true);
            }
        }

        if (punchTimer <= 0f)
            return;

        punchTimer -= Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(punchTimer / punchDuration);
        deathsText.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, punchScale, progress);

        if (punchTimer <= 0f)
            SetDeathColors(false);
    }

    private void SetDeathColors(bool hit)
    {
        if (skullIcon == null)
            return;

        deathsText.color = hit ? hitColor : normalColor;
        if (skullNormal != null && skullHit != null)
            skullIcon.sprite = hit ? skullHit : skullNormal;
        else
            skullIcon.color = hit ? hitColor : skullColor;
    }
}
