using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Mainmenu : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private string gameSceneName = "Game";
    bool optionsOpen;
    string error;
    void Awake()
    {
        Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        if (startButton != null) startButton.onClick.AddListener(OnStartPressed);
        if (optionsButton != null) { optionsButton.interactable = true; optionsButton.onClick.AddListener(ToggleOptions); }
    }
    void ToggleOptions() => optionsOpen = !optionsOpen;
    void OnStartPressed()
    {
        if (Application.CanStreamedLevelBeLoaded(gameSceneName)) SceneManager.LoadScene(gameSceneName);
        else { error = "Add '" + gameSceneName + "' to Build Profiles > Scene List."; Debug.LogError(error, this); }
    }
    void OnGUI()
    {
        if (!string.IsNullOrEmpty(error)) GUI.Box(new Rect(20, 20, Screen.width - 40, 45), error);
        if (!optionsOpen) return;
        GUILayout.BeginArea(new Rect(Screen.width / 2f - 180, Screen.height / 2f - 120, 360, 240), GUI.skin.box);
        GUILayout.Label("BCH Cow Dating - Options");
        GUILayout.Label("Mouse sensitivity");
        float old = CowDatingGame.LookScale;
        float value = GUILayout.HorizontalSlider(old, 0.25f, 2.5f);
        if (!Mathf.Approximately(value, old)) PlayerPrefs.SetFloat("BCH.LookScale", value);
        GUILayout.Label("WASD move | Shift sprint | Space jump\nE talk | 1/Y and 2/N reply | Esc pause");
        if (GUILayout.Button("Close", GUILayout.Height(40))) optionsOpen = false;
        GUILayout.EndArea();
    }
    void OnDestroy()
    {
        if (startButton != null) startButton.onClick.RemoveListener(OnStartPressed);
        if (optionsButton != null) optionsButton.onClick.RemoveListener(ToggleOptions);
        PlayerPrefs.Save();
    }
}
