using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button optionsButton;

    [Header("Scene to load when Start is pressed")]
    [SerializeField] private string gameSceneName = "Game";

    private void Awake()
    {
        startButton.onClick.AddListener(OnStartPressed);

        // Options isn't built yet, so the button is greyed out and does nothing.
        // When you're ready, set this to true and add a listener.
        optionsButton.interactable = false;
    }

    private void OnStartPressed()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}