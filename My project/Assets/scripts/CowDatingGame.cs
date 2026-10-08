using UnityEngine;
using UnityEngine.SceneManagement;

// Automatically created by the player controller. Add one manually to customize the goal.
[DefaultExecutionOrder(-100)]
public class CowDatingGame : MonoBehaviour
{
    public static CowDatingGame Instance { get; private set; }
    public static bool Paused => Instance != null && Instance.paused;
    public static bool Blocked => Paused || DialogueUI.IsOpen || SlapHand.IsPlaying || (Instance != null && Instance.won);
    public static float LookScale => PlayerPrefs.GetFloat("BCH.LookScale", 1f);
    [Min(1)] public int datesToWin = 3;
    public string menuSceneName = "MainMenu";
    int dates;
    bool paused, won;
    float savedTimeScale = 1f;
    CowInteractable focused;
    string toast = "Welcome to BCH! Talk to cows and earn three cafe dates.";
    float toastUntil;
    GUIStyle textStyle, headingStyle;
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        toastUntil = Time.unscaledTime + 8;
    }
    void Update()
    {
        if (GameInput.Pressed(KeyCode.Escape) && !DialogueUI.IsOpen && !SlapHand.IsPlaying && !won) SetPaused(!paused);
        if ((paused || won) && GameInput.Pressed(KeyCode.R)) Restart();
        focused = Blocked ? null : CowInteractable.FindFocused();
        if (focused != null && GameInput.Pressed(focused.interactKey)) focused.Interact();
    }
    public void Notify(string message) { toast = message; toastUntil = Time.unscaledTime + 5; }
    public void RecordDate(string cowName)
    {
        dates++;
        Notify(cowName + " accepted! Cafe dates: " + dates + "/" + datesToWin);
        if (dates >= Mathf.Max(1, datesToWin)) won = true;
    }
    void SetPaused(bool value)
    {
        paused = value;
        if (value) { savedTimeScale = Time.timeScale; Time.timeScale = 0; }
        else Time.timeScale = savedTimeScale;
        Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = value;
    }
    public void Restart()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!Application.CanStreamedLevelBeLoaded(scene.name))
        { Notify("Add this scene to File > Build Profiles > Scene List to enable restart."); return; }
        Time.timeScale = paused ? savedTimeScale : 1;
        SceneManager.LoadScene(scene.name);
    }
    void OnDestroy()
    {
        if (Instance != this) return;
        if (paused) Time.timeScale = savedTimeScale;
        Instance = null;
    }
    void OnGUI()
    {
        if (textStyle == null)
        {
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };
            headingStyle = new GUIStyle(textStyle) { fontSize = 26, alignment = TextAnchor.MiddleCenter };
        }
        float scale = Mathf.Clamp(Screen.height / 720f, 0.65f, 2.5f);
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        float w = Screen.width / scale, h = Screen.height / scale;
        GUI.Box(new Rect(16, 16, 390, 100), "");
        GUI.Label(new Rect(28, 23, 365, 30), "BCH: Udderly in Love", textStyle);
        GUI.Label(new Rect(28, 55, 365, 52), "Goal: " + dates + " / " + datesToWin + " cafe dates\nWASD move | Shift sprint | Space jump | Esc pause", textStyle);
        if (Time.unscaledTime < toastUntil) GUI.Label(new Rect(24, 125, 420, 90), toast, textStyle);
        if (!Blocked)
        {
            GUI.Label(new Rect(w / 2 - 10, h / 2 - 16, 20, 32), "+", headingStyle);
            if (focused != null) GUI.Label(new Rect(w / 2 - 240, h - 110, 480, 70), "[" + focused.interactKey + "] Talk to " + focused.DisplayName + "\n" + focused.Status, headingStyle);
        }
        if ((paused || won) && !DialogueUI.IsOpen && !SlapHand.IsPlaying)
        {
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            GUILayout.BeginArea(new Rect(w / 2 - 240, h / 2 - 180, 480, 360), GUI.skin.box);
            GUILayout.Label(won ? "You're udderly charming!" : "Taking a study break", headingStyle);
            GUILayout.Label(won ? "Three cows, three cafe dates. BCH's newest romance legend!" : "Chat twice: learn their interests, then choose a thoughtful date.", textStyle);
            if (!won && GUILayout.Button("Resume", GUILayout.Height(40))) SetPaused(false);
            GUILayout.Label("Mouse sensitivity", textStyle);
            float value = GUILayout.HorizontalSlider(LookScale, 0.25f, 2.5f);
            if (!Mathf.Approximately(value, LookScale)) PlayerPrefs.SetFloat("BCH.LookScale", value);
            if (GUILayout.Button("Play again [R]", GUILayout.Height(40))) Restart();
            if (Application.CanStreamedLevelBeLoaded(menuSceneName) && GUILayout.Button("Main menu", GUILayout.Height(40)))
            { Time.timeScale = 1; SceneManager.LoadScene(menuSceneName); }
            GUILayout.EndArea();
        }
        GUI.matrix = previous;
    }
}
