using System;
using UnityEngine;

// Mecánica 04 (GDD): muerte del jugador. Al morir lanza partículas, se oculta y pierde el control.
// No sabe a dónde reaparece: eso lo decide RespawnManager.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDeath : MonoBehaviour
{
    // Para sistemas que reaccionan a la muerte sin que las trampas los conozcan
    // (reaparición, contador de muertes, manchas de sangre...). Envía la posición de la muerte.
    public static event Action<Vector2> Died;

    [Tooltip("Efecto al morir (opcional).")]
    [SerializeField] private GameObject deathEffectPrefab;
    [Tooltip("Scripts que se desactivan mientras está muerto (movimiento, salto...).")]
    [SerializeField] private Behaviour[] controlScripts;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Rigidbody2D rb;
    private PlayerAudio playerAudio;

    public bool IsDead { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerAudio = GetComponent<PlayerAudio>(); 

    }

    public void Die()
    {
        if (IsDead)
            return;

        IsDead = true;
        Vector2 deathPosition = rb.position;
        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, deathPosition, Quaternion.identity);

        if (playerAudio != null) playerAudio.PlayDeath();   


        SetAlive(false);
        Died?.Invoke(deathPosition);
    }

    public void Respawn(Vector2 position)
    {
        SetAlive(false);
        // Se mueven Rigidbody y Transform para que la interpolación no dibuje un "viaje" hasta el checkpoint.
        rb.position = position;
        transform.position = position;
        rb.linearVelocity = Vector2.zero;
        SetAlive(true);
        IsDead = false;
    }

    private void SetAlive(bool alive)
    {
        // Sin física mientras está muerto: no cae ni toca más trampas.
        rb.simulated = alive;
        if (spriteRenderer != null)
            spriteRenderer.enabled = alive;
        foreach (Behaviour script in controlScripts)
        {
            if (script != null)
                script.enabled = alive;
        }
    }
}
