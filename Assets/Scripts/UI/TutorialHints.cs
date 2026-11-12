using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class TutorialHints : MonoBehaviour
{
    [Serializable]
    private struct Hint
    {
        public float fromX;
        public float toX;
        public string keyboard;
        public string gamepad;
    }

    [SerializeField] private PlayerDeath player;
    [SerializeField] private RespawnManager respawn;
    [SerializeField] private RunStats stats;
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Hint[] hints;

    private bool usingGamepad;
    private string shown;

    private void Awake() => usingGamepad = Keyboard.current == null && Gamepad.current != null;

    private void Update()
    {
        var keyboard = Keyboard.current;
        var gamepad = Gamepad.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            usingGamepad = false;
        else if (gamepad != null && (gamepad.leftStick.ReadValue().sqrMagnitude > 0.25f
                                     || gamepad.buttonSouth.wasPressedThisFrame || gamepad.startButton.wasPressedThisFrame))
            usingGamepad = true;

        if (GamePauseState.BlocksGameplayInput || !stats.IsRunning)
        {
            Show(null);
            return;
        }

        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            respawn.RestartFromCheckpoint();

        if (!player.IsDead)
        {
            float x = player.transform.position.x;
            foreach (Hint hint in hints)
            {
                if (x >= hint.fromX && x < hint.toX)
                {
                    Show(usingGamepad ? hint.gamepad : hint.keyboard);
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
