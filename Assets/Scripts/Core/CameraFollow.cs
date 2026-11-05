using UnityEngine;

// Cámara que sigue al jugador con suavizado, sin mostrar nada fuera de los límites del nivel.
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 offset = new Vector2(3f, 1.5f);
    [Tooltip("Rapidez del seguimiento (más alto = más pegada).")]
    [SerializeField, Min(0f)] private float sharpness = 6f;
    [Tooltip("Esquina inferior izquierda del nivel.")]
    [SerializeField] private Vector2 minBounds = new Vector2(-10f, -6f);
    [Tooltip("Esquina superior derecha del nivel.")]
    [SerializeField] private Vector2 maxBounds = new Vector2(130f, 20f);

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desired = target.position + (Vector3)offset;
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        desired.x = Mathf.Clamp(desired.x, minBounds.x + halfWidth, maxBounds.x - halfWidth);
        desired.y = Mathf.Clamp(desired.y, minBounds.y + halfHeight, maxBounds.y - halfHeight);
        desired.z = transform.position.z;

        float t = 1f - Mathf.Exp(-sharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desired, t);
    }
}
