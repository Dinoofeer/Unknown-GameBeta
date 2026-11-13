using System;
using TMPro;
using UnityEngine;

// Indicaciones contextuales del tutorial (GDD, Interfaz Visual). Solo teclado:
// el GDD (Alcance) excluye los mandos.
public sealed class TutorialHints : MonoBehaviour
{
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

    private void Update()
    {
        if (GamePauseState.BlocksGameplayInput || !stats.IsRunning)
        {
            Show(null);
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
