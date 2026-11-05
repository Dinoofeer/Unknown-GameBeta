using UnityEngine;

// Solo visual: polvo en la mitad limpia del nivel y chispas en la deteriorada, según dónde esté la cámara.
public class AmbientParticlesZone : MonoBehaviour
{
    [SerializeField] private ParticleSystem cleanZone;
    [SerializeField] private ParticleSystem brokenZone;
    [SerializeField] private float brokenFromX = 150f;

    private void LateUpdate()
    {
        bool broken = transform.position.x >= brokenFromX;
        SetEmitting(cleanZone, !broken);
        SetEmitting(brokenZone, broken);
    }

    private static void SetEmitting(ParticleSystem system, bool emitting)
    {
        if (system == null)
            return;
        ParticleSystem.EmissionModule emission = system.emission;
        emission.enabled = emitting;
    }
}
