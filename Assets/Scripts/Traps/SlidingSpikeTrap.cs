using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class SlidingSpikeTrap : MonoBehaviour
{
    [SerializeField] private Transform spikes;
    [SerializeField, Min(0.1f)] private float distance = 3f;
    [SerializeField, Min(0.1f)] private float speed = 4f;
    [SerializeField, Min(0f)] private float warningTime = 0.25f;
    [SerializeField, Min(0f)] private float shakeAmplitude = 0.05f;

    private Vector3 startPosition;
    private Coroutine slideRoutine;
    private bool triggered;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        if (spikes == null)
        {
            Debug.LogError($"{name}: falta asignar Spikes en SlidingSpikeTrap.", this);
            enabled = false;
            return;
        }

        startPosition = spikes.position;
    }

    private void OnEnable() => RespawnManager.PlayerRespawned += ResetTrap;

    private void OnDisable()
    {
        RespawnManager.PlayerRespawned -= ResetTrap;
        ResetTrap();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D player = other.attachedRigidbody;
        if (!isActiveAndEnabled || triggered || player == null || !player.CompareTag("Player"))
            return;

        triggered = true;
        float direction = Mathf.Sign(player.position.x - startPosition.x);
        slideRoutine = StartCoroutine(Slide(direction));
    }

    private IEnumerator Slide(float direction)
    {
        for (float elapsed = 0f; elapsed < warningTime; elapsed += Time.deltaTime)
        {
            if (Time.deltaTime > 0f)
                spikes.position = startPosition + (Vector3)(Random.insideUnitCircle * shakeAmplitude);
            yield return null;
        }

        Vector3 target = startPosition + Vector3.right * (direction * distance);
        spikes.position = startPosition;
        while (spikes.position != target)
        {
            spikes.position = Vector3.MoveTowards(spikes.position, target, speed * Time.deltaTime);
            yield return null;
        }

        spikes.position = target;
        slideRoutine = null;
    }

    private void ResetTrap()
    {
        if (slideRoutine != null)
        {
            StopCoroutine(slideRoutine);
            slideRoutine = null;
        }

        if (spikes != null)
            spikes.position = startPosition;
        triggered = false;
    }
}
