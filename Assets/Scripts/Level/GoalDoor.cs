using System;
using UnityEngine;

// Mecánica 06 (GDD): puerta de meta. Bloqueada (sólida) hasta que el jugador llega con la llave.
// Al tocarla con la llave se abre y el nivel se completa.
public class GoalDoor : MonoBehaviour
{
    // Para el futuro flujo de niveles (siguiente nivel, guardado...).
    public static event Action LevelCompleted;

    [Tooltip("Vacío = sin llave: la puerta está abierta desde el inicio (portal de salida).")]
    [SerializeField] private LevelKey requiredKey;
    [Tooltip("Collider sólido que bloquea el paso mientras está cerrada.")]
    [SerializeField] private Collider2D blockingCollider;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color lockedColor = new Color(0.5f, 0.2f, 0.2f);
    [SerializeField] private Color openColor = new Color(0.3f, 0.9f, 0.4f);
    [Tooltip("Diseño troll: tocar la puerta cerrada sin la llave mata al jugador.")]
    [SerializeField] private bool lethalWhenLocked = false;
    [Tooltip("Opcional: se enciende solo cuando la puerta se puede cruzar (por ejemplo, la luz del portal).")]
    [SerializeField] private Behaviour unlockedOnlyEffect;
    [Tooltip("Opcional: objeto visible solo mientras la puerta sigue cerrada (por ejemplo, el candado).")]
    [SerializeField] private GameObject lockedOnlyObject;
    [Tooltip("Opcional: lo que dice el Sistema al completar el nivel.")]
    [TextArea(1, 3)]
    [SerializeField] private string completedLine;
    [Header("Audio")]
    [SerializeField] private AudioClip openClip;

    private bool completed;

    private void Awake()
    {
        if (blockingCollider == null)
        {
            Debug.LogError($"{name}: falta blockingCollider en GoalDoor.", this);
            enabled = false;
            return;
        }
        SetOpen(requiredKey == null);
    }

    private void Update()
    {
        // La llave puede cogerse y perderse en cualquier momento: el efecto refleja el estado actual.
        bool unlocked = completed || requiredKey == null || requiredKey.IsCollected;
        if (unlockedOnlyEffect != null && !completed)
            unlockedOnlyEffect.enabled = unlocked;
        if (lockedOnlyObject != null)
            lockedOnlyObject.SetActive(!unlocked);
    }

    // Lo dispara un trigger algo mayor que el collider sólido, para detectar al jugador al tocarla.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (completed || !other.CompareTag("Player"))
            return;

        if (requiredKey == null || requiredKey.IsCollected)
        {
            completed = true;
            SfxPlayer.Play(openClip);   // audio
            if (requiredKey != null)
                requiredKey.Consume();
            SetOpen(true);
            Debug.Log("Nivel completado.");
            LevelCompleted?.Invoke();
            if (!string.IsNullOrEmpty(completedLine))
                Narrator.Say(completedLine);
        }
        else if (lethalWhenLocked && other.attachedRigidbody != null
                 && other.attachedRigidbody.TryGetComponent(out PlayerDeath player))
        {
            player.Die();
        }
    }

    private void SetOpen(bool open)
    {
        blockingCollider.enabled = !open;
        if (spriteRenderer != null)
            spriteRenderer.color = open ? openColor : lockedColor;
    }
}
