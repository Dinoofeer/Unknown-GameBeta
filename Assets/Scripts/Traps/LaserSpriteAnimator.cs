using UnityEngine;

// Anima el sprite del láser (laser_spritesheet) sincronizado con TimedLaser.
// TimedLaser sigue decidiendo cuándo mata; este script solo elige qué fotograma se ve.
//
// Fotogramas (una fila de la hoja, de izquierda a derecha):
//   0 apagado · 1 aviso (punteado) · 2 activo · 3 distorsionado · 4 disipándose · 5 partículas
//
// Ciclo:  ...apagado (0) → aviso (1) → ENCENDIDO: activo (2) → distorsionado (3) → APAGADO: disipa (4) → partículas (5) → apagado (0)
// Solo los fotogramas 2 y 3 matan, porque son los que TimedLaser tiene encendidos.
[RequireComponent(typeof(SpriteRenderer))]
public class LaserSpriteAnimator : MonoBehaviour
{
    [Tooltip("El láser que controla los tiempos. Si está vacío, se busca en el padre.")]
    [SerializeField] private TimedLaser laser;

    [Tooltip("Los 6 fotogramas de UNA fila, en orden: apagado, aviso, activo, distorsionado, disipando, partículas.")]
    [SerializeField] private Sprite[] frames = new Sprite[6];

    [Header("Tiempos (segundos)")]
    [Tooltip("Cuánto antes de encenderse se muestra el aviso punteado. No mata.")]
    [SerializeField, Min(0f)] private float warningTime = 0.3f;
    [Tooltip("Cuánto antes de apagarse el haz se distorsiona. Sigue matando.")]
    [SerializeField, Min(0f)] private float distortTime = 0.25f;
    [Tooltip("Duración de cada fotograma de disipación (4 y 5) al apagarse.")]
    [SerializeField, Min(0f)] private float dissipateFrameTime = 0.08f;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (laser == null)
            laser = GetComponentInParent<TimedLaser>();
        if (laser == null)
            Debug.LogError($"{name}: LaserSpriteAnimator necesita un TimedLaser (asígnalo o ponlo como hijo del láser).", this);
        if (frames == null || frames.Length < 6)
            Debug.LogError($"{name}: faltan fotogramas. Necesita 6, en orden.", this);
    }

    private void LateUpdate()
    {
        if (laser == null || !laser.isActiveAndEnabled || frames == null || frames.Length < 6)
        {
            Show(0);
            return;
        }

        float inState = laser.SecondsInState;
        float left = laser.SecondsLeftInState;
        int frame;

        if (laser.IsActive)
            frame = left <= distortTime ? 3 : 2;           // encendido
        else if (left <= warningTime)
            frame = 1;                                      // a punto de encenderse
        else if (inState < dissipateFrameTime)
            frame = 4;                                      // recién apagado
        else if (inState < dissipateFrameTime * 2f)
            frame = 5;
        else
            frame = 0;                                      // apagado

        Show(frame);
    }

    private void Show(int index)
    {
        if (frames != null && index < frames.Length && frames[index] != null)
            spriteRenderer.sprite = frames[index];
    }
}
