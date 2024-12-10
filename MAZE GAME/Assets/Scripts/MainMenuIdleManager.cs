using UnityEngine;
using System.Collections;
using UnityEngine.Video;

public class MainMenuIdleManager : MonoBehaviour
{
    public CanvasGroup popupCanvasGroup; // Reference to the popup panel's CanvasGroup
    public VideoPlayer popupVideoPlayer; // Reference to the Video Player
    public float idleTimeThreshold = 10f; // Idle time before showing the popup

    private float idleTimer = 0f; // Tracks idle time
    private bool isPopupVisible = false; // Tracks if the popup is visible
    private Vector3 lastMousePosition; // Tracks the last mouse position

    void Start()
    {
        popupCanvasGroup.alpha = 0f; // Ensure transparency
        popupCanvasGroup.gameObject.SetActive(false); // Ensure it's inactive
        idleTimer = 0f; // Reset idle timer
        lastMousePosition = Input.mousePosition; // Track initial mouse position
    }



    void Update()
    {
        // Detect user activity (mouse movement, key press, etc.)
        if (Input.anyKey || Input.mousePosition != lastMousePosition)
        {
            idleTimer = 0f; // Reset the idle timer
            lastMousePosition = Input.mousePosition; // Update last mouse position

            if (isPopupVisible)
            {
                HidePopup(); // Hide the popup on activity
            }
        }
        else
        {
            idleTimer += Time.deltaTime; // Increment the idle timer
        }

        // Show the popup if the idle time threshold is reached
        if (idleTimer >= idleTimeThreshold && !isPopupVisible)
        {
            ShowPopup();
        }
    }

    public void ShowPopup()
    {
        isPopupVisible = true;
        StartCoroutine(FadeInPopup());
        popupVideoPlayer.Play(); // Start playing the video
    }

    public void HidePopup()
    {
        isPopupVisible = false;
        StartCoroutine(FadeOutPopup());
        popupVideoPlayer.Stop(); // Stop playing the video
    }


    private IEnumerator FadeInPopup()
    {
        popupCanvasGroup.alpha = 0f;
        popupCanvasGroup.gameObject.SetActive(true); // Ensure the popup is active

        while (popupCanvasGroup.alpha < 1f)
        {
            popupCanvasGroup.alpha += Time.deltaTime * 5f; 
            yield return null;
        }

        popupCanvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOutPopup()
    {
        while (popupCanvasGroup.alpha > 0f)
        {
            popupCanvasGroup.alpha -= Time.deltaTime * 5f; 
            yield return null;
        }

        popupCanvasGroup.alpha = 0f;
        popupCanvasGroup.gameObject.SetActive(false); // Deactivate the popup after fading out
    }


}
