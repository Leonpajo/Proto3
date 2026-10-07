using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// ONE of these in the scene, on your Canvas (or any object).
/// Prefabs can't reference scene objects, so cows talk to this via DialogueUI.Instance.
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance.open;

    public GameObject panel;      // the text box panel (hidden at start)
    public TMP_Text messageText;
    public Button yesButton;
    public Button noButton;

    Action onYes, onNo;
    bool open;
    CursorLockMode previousLock;
    bool previousVisible;

    void Awake()
    {
        Instance = this;
        SetVisible(false);
        yesButton.onClick.AddListener(() => Close(onYes));
        noButton.onClick.AddListener(() => Close(onNo));

        if (FindFirstObjectByType<EventSystem>() == null)
            Debug.LogError("DialogueUI: no EventSystem in the scene, buttons can't be clicked. " +
                           "Add one via GameObject > UI > Event System.");
    }

    public void Show(string message, Action yes, Action no)
    {
        onYes = yes;
        onNo = no;
        messageText.text = message;

        previousLock = Cursor.lockState;
        previousVisible = Cursor.visible;

        SetVisible(true);
        FreeCursor();
    }

    // Runs after every other script's Update, so a first-person controller
    // that re-locks the cursor each frame can't win while the dialog is open.
    void LateUpdate()
    {
        if (IsOpen) FreeCursor();
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

    void FreeCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Close(Action callback)
    {
        SetVisible(false);
        Cursor.lockState = previousLock;
        Cursor.visible = previousVisible;
        callback?.Invoke();
    }
}