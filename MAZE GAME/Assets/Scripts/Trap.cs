using System.Collections;
using UnityEngine;

public class Trap : MonoBehaviour
{
    Game game;
    private Collider2D spikeCollider;
    private SpriteRenderer spikeRenderer;

    [SerializeField] private float spikeCooldown = 1.5f; // Time between spikes appearing and disappearing

    private void Start()
    {
        // Cache components
        spikeCollider = GetComponent<Collider2D>();
        spikeRenderer = GetComponent<SpriteRenderer>();

        // Start the visibility toggling coroutine
        StartCoroutine(ToggleSpikeState());
    }

    private IEnumerator ToggleSpikeState()
    {
        while (true)
        {
            // Enable spikes
            SetSpikeState(true);
            yield return new WaitForSeconds(spikeCooldown);

            // Disable spikes
            SetSpikeState(false);
            yield return new WaitForSeconds(spikeCooldown);
        }
    }

    private void SetSpikeState(bool isActive)
    {
        // Enable or disable the spike's functionality
        spikeCollider.enabled = isActive;
        spikeRenderer.enabled = isActive;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (spikeCollider.enabled && collision.CompareTag("Player"))
        {
            TriggerTrap(collision.transform);
        }
    }

    private void TriggerTrap(Transform player)
    {
        Debug.Log("Trap triggered! Player penalized.");

        // Penalize the player: reduce time
        Game.instance.AddTime(-5f);

        // Play a sound or visual effect
        AudioManager.instance.PlaySFX("TrapTriggered");
    }
}
