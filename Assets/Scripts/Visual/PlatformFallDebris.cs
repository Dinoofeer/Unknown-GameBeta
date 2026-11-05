using UnityEngine;

// Solo visual: suelta escombros cuando la plataforma padre empieza a caer.
// FallingPlatform apaga su collider justo en ese momento, así que nada se ve antes y no la delata.
[RequireComponent(typeof(ParticleSystem))]
public class PlatformFallDebris : MonoBehaviour
{
    private ParticleSystem particles;
    private Collider2D platformCollider;
    private bool wasSolid = true;

    private void Awake()
    {
        particles = GetComponent<ParticleSystem>();
        platformCollider = GetComponentInParent<Collider2D>();
        if (platformCollider is BoxCollider2D box)
        {
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.scale = new Vector3(box.size.x, 0.5f, 1f);
        }
    }

    private void LateUpdate()
    {
        if (platformCollider == null)
            return;

        bool solid = platformCollider.enabled;
        if (wasSolid && !solid)
            particles.Play();
        wasSolid = solid;
    }
}
