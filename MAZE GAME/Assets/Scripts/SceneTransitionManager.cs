using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager instance;
    public Image fadeImage; // Drag the FadeImage here
    public float fadeDuration = 1f; // Duration of the fade effect

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Prevent the manager from being destroyed
        }
        else
        {
            Destroy(gameObject);
        }

        if (fadeImage == null)
        {
            fadeImage = FindObjectOfType<Image>(); // Find FadeImage dynamically
            if (fadeImage != null)
                fadeImage.gameObject.SetActive(false); // Disable it initially
        }
    }


    private void Start()
    {
        StartCoroutine(FadeIn()); // Start with a fade-in effect
    }

    public void TransitionToScene(string sceneName)
    {
        StartCoroutine(FadeOut(sceneName));
    }

    private IEnumerator FadeIn()
    {
        float alpha = 1f; // Start fully opaque
        fadeImage.gameObject.SetActive(true);

        while (alpha > 0f)
        {
            alpha -= Time.deltaTime / fadeDuration;
            fadeImage.color = new Color(0f, 0f, 0f, Mathf.Clamp01(alpha));
            yield return null;
        }

        fadeImage.color = new Color(0f, 0f, 0f, 0f); // Fully transparent
        fadeImage.gameObject.SetActive(false); // Hide the image
    }


    private IEnumerator FadeOut(string sceneName)
    {
        float alpha = 0f; // Start fully transparent
        fadeImage.gameObject.SetActive(true);

        while (alpha < 1f)
        {
            alpha += Time.deltaTime / fadeDuration;
            fadeImage.color = new Color(0f, 0f, 0f, Mathf.Clamp01(alpha));
            yield return null;
        }

        fadeImage.color = new Color(0f, 0f, 0f, 1f); // Fully opaque
        SceneManager.LoadScene(sceneName);

        // Trigger FadeIn after loading the scene
        StartCoroutine(FadeIn());
    }

}
