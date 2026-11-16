using System;
using TMPro;
using UnityEngine;

// Indicaciones de teclado del tutorial y avisos breves de recogida en el panel del HUD.
public sealed class TutorialHints : MonoBehaviour
{
    private const float PickupDuration = 3f;

    [Serializable]
    private struct Hint
    {
        public float fromX;
        public float toX;
        public string keyboard;
    }

    [SerializeField] private PlayerDeath player;
    [SerializeField] private RunStats stats;
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Hint[] hints;

    private string shown;
    private string pickupMessage;
    private float pickupTimeRemaining;

    public void ShowPickup(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        pickupMessage = message;
        pickupTimeRemaining = PickupDuration;
    }

    private void Update()
    {
        if (GamePauseState.BlocksGameplayInput || !stats.IsRunning)
        {
            Show(null);
            return;
        }

        if (pickupTimeRemaining > 0f)
        {
            Show(pickupMessage);
            pickupTimeRemaining -= Time.deltaTime;
            return;
        }

        if (!player.IsDead)
        {
            float x = player.transform.position.x;
            foreach (Hint hint in hints)
            {
                if (x >= hint.fromX && x < hint.toX)
                {
                    Show(hint.keyboard);
                    return;
                }
            }
        }

        Show(null);
    }

    private void Show(string message)
    {
        if (shown == message)
            return;

        shown = message;
        panel.SetActive(!string.IsNullOrEmpty(message));
        label.text = message;
    }
}
