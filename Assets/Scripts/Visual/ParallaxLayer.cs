using UnityEngine;

// Solo visual: capa de fondo que sigue a la cámara a otra velocidad.
// Funde de la versión limpia a la rota según avanza el nivel.
public class ParallaxLayer : MonoBehaviour
{
    [Tooltip("0 = fija en el mundo; 1 = pegada a la cámara.")]
    [SerializeField, Range(0f, 1f)] private float factor = 0.7f;
    [SerializeField] private SpriteRenderer cleanLayer;
    [SerializeField] private SpriteRenderer brokenLayer;
    [Tooltip("x de la cámara donde empieza y termina el fundido a la versión rota.")]
    [SerializeField] private Vector2 fadeRange = new Vector2(140f, 165f);

    private Transform cameraTransform;
    private Vector3 startPosition;

    private void Awake()
    {
        startPosition = transform.position;
        cameraTransform = Camera.main != null ? Camera.main.transform : null;
    }

    private void LateUpdate()
    {
        if (cameraTransform == null)
            return;

        Vector3 cam = cameraTransform.position;
        transform.position = new Vector3(
            startPosition.x + (cam.x - startPosition.x) * factor,
            startPosition.y + (cam.y - startPosition.y) * factor,
            startPosition.z);

        float broken = Mathf.InverseLerp(fadeRange.x, fadeRange.y, cam.x);
        SetAlpha(cleanLayer, 1f - broken);
        SetAlpha(brokenLayer, broken);
    }

    private static void SetAlpha(SpriteRenderer layer, float alpha)
    {
        if (layer == null)
            return;
        Color color = layer.color;
        color.a = alpha;
        layer.color = color;
    }
}
