using UnityEngine;


public class TorchTimer : MonoBehaviour
{
    public float lifetime = 4f; // Total lifetime of the torch
    public float fadeDuration = 1f; // Duration of the fade-out effect (adjust based on lifetime)

    private SpriteRenderer spriteRenderer; // Reference to the sprite renderer
    private UnityEngine.Rendering.Universal.Light2D torchLight; // Reference to the 2D light component
    private float fadeStartTime; // Time when fading begins
    private bool isFading = false; // Tracks if fading has begun

    void Start()
    {
        // Get references to the SpriteRenderer and Light2D
        spriteRenderer = GetComponent<SpriteRenderer>();
        torchLight = GetComponentInChildren<UnityEngine.Rendering.Universal.Light2D>();

        // Start the fade-out process after (lifetime - fadeDuration) seconds
        Invoke(nameof(StartFading), lifetime - fadeDuration);

        // Destroy the torch entirely after its full lifetime
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (isFading)
        {
            // Calculate fade progress (0 to 1)
            float fadeProgress = (Time.time - fadeStartTime) / fadeDuration;

            // Fade the sprite's alpha
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = Mathf.Lerp(1, 0, fadeProgress);
                spriteRenderer.color = color;
            }

            // Fade the light's intensity
            if (torchLight != null)
            {
                torchLight.intensity = Mathf.Lerp(torchLight.intensity, 0, fadeProgress);
            }
        }
    }

    void StartFading()
    {
        isFading = true; // Start the fade effect
        fadeStartTime = Time.time; // Record the fade start time
    }
}
