using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// ONE of these in the scene, on your Canvas (not on the panel).
/// Prefabs can't reference scene objects, so cows talk to this via DialogueUI.Instance.
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance.open;

    public GameObject panel;      // the text box panel (hidden at start)
    public TMP_Text messageText;
    public Button yesButton;
    public Button noButton;

    [Header("Debug")]
    public bool logClicks = true;   // prints what the mouse is clicking on while the dialog is open

    Action onYes, onNo;
    bool open;
    bool answering;   // true only while Yes/No is being asked (not during a reply)
    CursorLockMode previousLock;
    bool previousVisible;

    void Awake()
    {
        Instance = this;
        SetVisible(false);
        yesButton.onClick.AddListener(() => Close(onYes));
        noButton.onClick.AddListener(() => Close(onNo));

        EnsureClickable();
    }


    // Fixes the usual reasons UI buttons can't be clicked.
    void EnsureClickable()
    {
        // 1) EventSystem + the right input module for your Input setting
        EventSystem es = FindFirstObjectByType<EventSystem>();
        if (es == null)
        {
            es = new GameObject("EventSystem").AddComponent<EventSystem>();
            Debug.LogWarning("DialogueUI: there was no EventSystem, so I created one.");
        }
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        StandaloneInputModule oldModule = es.GetComponent<StandaloneInputModule>();
        if (oldModule != null) Destroy(oldModule);
        if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
            es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        if (es.GetComponent<BaseInputModule>() == null)
            es.gameObject.AddComponent<StandaloneInputModule>();
#endif

        // 2) Canvas needs a Graphic Raycaster
        Canvas canvas = panel.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Canvas root = canvas.rootCanvas;
            if (root.GetComponent<GraphicRaycaster>() == null)
                root.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    // Makes sure nothing above the buttons is switching clicks off.
    void MakeButtonsInteractable()
    {
        foreach (Button b in new[] { yesButton, noButton })
        {
            b.interactable = true;
            foreach (CanvasGroup g in b.GetComponentsInParent<CanvasGroup>(true))
            {
                g.interactable = true;
                g.blocksRaycasts = true;
            }
        }
    }

    public void Show(string message, Action yes, Action no)
    {
        onYes = yes;
        onNo = no;
        messageText.text = message;

        previousLock = Cursor.lockState;
        previousVisible = Cursor.visible;

        answering = true;
        SetVisible(true);
        MakeButtonsInteractable();
        FreeCursor();
    }

    // Runs after every other script's Update, so a controller that
    // re-locks the cursor each frame can't win while the dialog is open.
    void LateUpdate()
    {
        if (!open) return;
        FreeCursor();

        // Keyboard fallback: works even if mouse clicks don't
        if (answering)
        {
            if (KeyPressed(KeyCode.Y)) Close(onYes);
            else if (KeyPressed(KeyCode.N)) Close(onNo);
        }

        if (logClicks && MouseClicked()) LogWhatWasClicked();
    }

    void LogWhatWasClicked()
    {
        EventSystem es = EventSystem.current;
        if (es == null) { Debug.LogWarning("[DialogueUI] Click: there is NO EventSystem."); return; }

        var data = new PointerEventData(es) { position = MousePosition() };
        var hits = new List<RaycastResult>();
        es.RaycastAll(data, hits);

        string top = hits.Count > 0 ? hits[0].gameObject.name : "NOTHING (no UI under the mouse)";
        Debug.Log($"[DialogueUI] Click at {data.position}. Top UI hit: {top}. " +
                  $"Input module: {(es.currentInputModule != null ? es.currentInputModule.GetType().Name : "NONE")}. " +
                  $"Cursor lock: {Cursor.lockState}");
    }

    // Shows/hides everything explicitly, so it works even if the buttons
    // or text are not children of the panel.
    void SetVisible(bool visible)
    {
        open = visible;
        panel.SetActive(visible);
        messageText.gameObject.SetActive(visible);
        yesButton.gameObject.SetActive(visible);
        noButton.gameObject.SetActive(visible);
    }

    /// Shows a message with no buttons for a moment (the cow's reply), then calls done.
    public void ShowReply(string message, float seconds, Action done)
    {
        previousLock = Cursor.lockState;
        previousVisible = Cursor.visible;
        StartCoroutine(ReplyRoutine(message, seconds, done));
    }

    System.Collections.IEnumerator ReplyRoutine(string message, float seconds, Action done)
    {
        answering = false;
        open = true;
        messageText.text = message;
        panel.SetActive(true);
        messageText.gameObject.SetActive(true);
        yesButton.gameObject.SetActive(false);
        noButton.gameObject.SetActive(false);

        yield return new WaitForSecondsRealtime(seconds);

        SetVisible(false);
        Cursor.lockState = previousLock;
        Cursor.visible = previousVisible;
        done?.Invoke();
    }

    void FreeCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Close(Action callback)
    {
        answering = false;
        SetVisible(false);
        Cursor.lockState = previousLock;
        Cursor.visible = previousVisible;
        callback?.Invoke();
    }

    // ---- input helpers (work with the old and the new Input System) ----
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    bool KeyPressed(KeyCode key)
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return false;
        return key == KeyCode.Y ? kb.yKey.wasPressedThisFrame : kb.nKey.wasPressedThisFrame;
    }
    bool MouseClicked() => UnityEngine.InputSystem.Mouse.current != null
                           && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
    Vector2 MousePosition() => UnityEngine.InputSystem.Mouse.current.position.ReadValue();
#else
    bool KeyPressed(KeyCode key) => Input.GetKeyDown(key);
    bool MouseClicked() => Input.GetMouseButtonDown(0);
    Vector2 MousePosition() => Input.mousePosition;
#endif
}