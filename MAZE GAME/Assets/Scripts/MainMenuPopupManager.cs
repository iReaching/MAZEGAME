using UnityEngine;
using System.Collections;
using UnityEngine.Video;

public class MainMenuManager : MonoBehaviour
{
    public GameObject idleVideoCanvas;
    public CanvasGroup mainMenuCanvasGroup; // Reference to the main menu popup
    public VideoPlayer idleVideoPlayer; // Video player for the idle video
    public float fadeSpeed = 5f; // Speed for fading in/out
    public float idleTimeThreshold = 10f; // Time threshold for idle video to play

    private float idleTimer = 0f; // Tracks idle time
    private bool isPopupVisible = true; // Tracks if the popup is active
    private Vector3 lastMousePosition; // Last tracked mouse position

    private void Start()
    {
        // Ensure canvas and video are initially inactive
        idleVideoCanvas.SetActive(false);
        idleTimer = 0f;
        lastMousePosition = Input.mousePosition;
    }

    private void Update()
    {
        if (isPopupVisible) // Ensure the popup is active
        {
            // Detect user activity (keyboard press or mouse movement)
            if (Input.anyKey || Input.mousePosition != lastMousePosition)
            {
                idleTimer = 0f; // Reset idle timer on activity
                lastMousePosition = Input.mousePosition; // Update last mouse position

                if (idleVideoPlayer.isPlaying || idleVideoCanvas.activeSelf)
                {
                    idleVideoPlayer.Stop(); // Stop video if playing
                    idleVideoCanvas.SetActive(false); // Hide the canvas
                }
            }
            else
            {
                idleTimer += Time.deltaTime; // Increment idle timer
            }

            // Show the canvas and play video if idle time threshold is reached
            if (idleTimer >= idleTimeThreshold && !idleVideoPlayer.isPlaying)
            {
                idleVideoCanvas.SetActive(true); // Show the canvas
                idleVideoPlayer.Play(); // Start playing the video
            }
        }
    }


    public void OnStartButtonPressed()
    {
        StartCoroutine(FadeOutMenu());
    }

    private IEnumerator FadeOutMenu()
    {
        while (mainMenuCanvasGroup.alpha > 0f)
        {
            mainMenuCanvasGroup.alpha -= Time.deltaTime * fadeSpeed; // Gradual fade out
            yield return null;
        }

        mainMenuCanvasGroup.alpha = 0f;
        mainMenuCanvasGroup.gameObject.SetActive(false); // Deactivate the popup

        idleVideoPlayer.Stop(); // Ensure the video stops
        isPopupVisible = false; // Mark the popup as inactive

        Game.instance.StartGame(); // Start the game
    }

    public void OnExitButtonPressed()
    {
        Application.Quit(); // Quit the application
    }
}
