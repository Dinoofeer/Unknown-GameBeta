using System.Collections.Generic;
using UnityEngine;

// Mecánica secundaria (GDD): deja una mancha en el punto exacto de cada muerte.
// Las manchas no se borran al reaparecer.
public class BloodStainSpawner : MonoBehaviour
{
    [SerializeField] private GameObject stainPrefab;
    [Tooltip("Máximo de manchas a la vez; al pasarlo se reutiliza la más antigua.")]
    [SerializeField, Min(1)] private int maxStains = 200;
    [Tooltip("Sprites entre los que se elige al azar. Vacío = el sprite del prefab.")]
    [SerializeField] private Sprite[] stainVariants;
    [SerializeField, Min(0.1f)] private float minScale = 0.85f;
    [SerializeField, Min(0.1f)] private float maxScale = 1.15f;

    private readonly Queue<GameObject> stains = new Queue<GameObject>();

    private void OnEnable() => PlayerDeath.Died += OnPlayerDied;
    private void OnDisable() => PlayerDeath.Died -= OnPlayerDied;

    private void OnPlayerDied(Vector2 position)
    {
        if (stainPrefab == null)
            return;

        // Giros de 90° para que los píxeles sigan alineados con los del resto del juego.
        Quaternion rotation = Quaternion.Euler(0f, 0f, 90f * Random.Range(0, 4));
        GameObject stain;
        if (stains.Count >= maxStains)
        {
            stain = stains.Dequeue();
            stain.transform.SetPositionAndRotation(position, rotation);
        }
        else
        {
            stain = Instantiate(stainPrefab, position, rotation, transform);
        }
        ApplyRandomLook(stain);
        stains.Enqueue(stain);
    }

    private void ApplyRandomLook(GameObject stain)
    {
        stain.transform.localScale = Vector3.one * Random.Range(minScale, maxScale);
        if (!stain.TryGetComponent(out SpriteRenderer spriteRenderer))
            return;

        spriteRenderer.flipX = Random.value < 0.5f;
        if (stainVariants != null && stainVariants.Length > 0)
            spriteRenderer.sprite = stainVariants[Random.Range(0, stainVariants.Length)];
    }
}
