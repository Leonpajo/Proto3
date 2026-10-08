using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Put on the Canvas, not on the dialogue panel. Missing references use a built-in fallback UI.
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance.open;
    public GameObject panel;
    public TMP_Text messageText;
    public Button yesButton, noButton;
    public bool logClicks = false;
    Action onYes, onNo, onCancel, replyDone;
    bool open, answering;
    string message, yesLabel = "Yes", noLabel = "No";
    CursorLockMode previousLock;
    bool previousVisible;
    Coroutine replyRoutine;
    bool Wired => panel != null && messageText != null && yesButton != null && noButton != null;
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        HideObjects();
        if (yesButton != null) yesButton.onClick.AddListener(ChooseYes);
        if (noButton != null) noButton.onClick.AddListener(ChooseNo);
        if (Wired) EnsureClickable();
    }
    void EnsureClickable()
    {
        EventSystem es = FindFirstObjectByType<EventSystem>();
        if (es == null) es = new GameObject("EventSystem").AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var oldModule = es.GetComponent<StandaloneInputModule>();
        if (oldModule != null) { oldModule.enabled = false; Destroy(oldModule); }
        if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
            es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        if (es.GetComponent<BaseInputModule>() == null) es.gameObject.AddComponent<StandaloneInputModule>();
#endif
        Canvas canvas = panel.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.rootCanvas.GetComponent<GraphicRaycaster>() == null)
            canvas.rootCanvas.gameObject.AddComponent<GraphicRaycaster>();
        messageText.raycastTarget = false;
        foreach (Button button in new[] { yesButton, noButton })
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.raycastTarget = false;
            if (button.targetGraphic != null) button.targetGraphic.raycastTarget = true;
        }
    }
    void Begin()
    {
        if (!open) { previousLock = Cursor.lockState; previousVisible = Cursor.visible; }
        open = true;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }
    public void Show(string text, Action yes, Action no)
        => ShowChoice(text, "Yes", "No", yes, no, null);
    public void ShowChoice(string text, string first, string second, Action yes, Action no, Action cancel)
    {
        if (open) return;
        Begin(); answering = true;
        message = text; yesLabel = first; noLabel = second;
        onYes = yes; onNo = no; onCancel = cancel;
        RefreshObjects();
    }
    public void ShowReply(string text, float seconds, Action done)
    {
        if (open) return;
        Begin(); answering = false; message = text; replyDone = done;
        RefreshObjects();
        replyRoutine = StartCoroutine(ReplyRoutine(Mathf.Max(1f, seconds)));
    }
    IEnumerator ReplyRoutine(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        replyRoutine = null;
        FinishReply();
    }
    void LateUpdate()
    {
        if (!open) return;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        if (answering)
        {
            if (GameInput.Pressed(KeyCode.Y) || GameInput.Pressed(KeyCode.Alpha1)) ChooseYes();
            else if (GameInput.Pressed(KeyCode.N) || GameInput.Pressed(KeyCode.Alpha2)) ChooseNo();
            else if (GameInput.Pressed(KeyCode.Escape)) Cancel();
        }
        else if (GameInput.Pressed(KeyCode.Space) || GameInput.Pressed(KeyCode.Escape)) FinishReply();
    }
    void ChooseYes() { if (open && answering) Complete(onYes); }
    void ChooseNo() { if (open && answering) Complete(onNo); }
    public void Cancel()
    {
        if (!open) return;
        if (!answering) { FinishReply(); return; }
        Complete(onCancel);
    }
    void FinishReply() { if (open && !answering) Complete(replyDone); }
    void Complete(Action callback)
    {
        if (replyRoutine != null) { StopCoroutine(replyRoutine); replyRoutine = null; }
        open = false; answering = false;
        onYes = onNo = onCancel = replyDone = null;
        HideObjects();
        Cursor.lockState = previousLock; Cursor.visible = previousVisible;
        callback?.Invoke();
    }
    void HideObjects()
    {
        if (panel != null) panel.SetActive(false);
        if (messageText != null) messageText.gameObject.SetActive(false);
        if (yesButton != null) yesButton.gameObject.SetActive(false);
        if (noButton != null) noButton.gameObject.SetActive(false);
    }
    void RefreshObjects()
    {
        if (!Wired) return;
        panel.SetActive(true); messageText.gameObject.SetActive(true); messageText.text = message;
        foreach (CanvasGroup group in panel.GetComponentsInParent<CanvasGroup>(true))
        { group.interactable = true; group.blocksRaycasts = true; group.alpha = 1; }
        SetButton(yesButton, "[1 / Y] " + yesLabel);
        SetButton(noButton, "[2 / N] " + noLabel);
    }
    void SetButton(Button button, string label)
    {
        button.gameObject.SetActive(answering); button.interactable = true;
        TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) tmp.text = label;
        else { Text old = button.GetComponentInChildren<Text>(true); if (old != null) old.text = label; }
    }
    void OnGUI()
    {
        if (!open) return;
        if (Wired)
        {
            GUI.Label(new Rect(20, Screen.height - 30, Screen.width - 40, 26), answering ? "1 / Y: first choice | 2 / N: second choice | Esc: leave" : "Space: continue");
            return;
        }
        float width = Mathf.Min(680, Screen.width - 24);
        var style = new GUIStyle(GUI.skin.label) { fontSize = 20, wordWrap = true };
        GUILayout.BeginArea(new Rect((Screen.width - width) / 2, Mathf.Max(12, Screen.height / 2 - 170), width, 340), GUI.skin.box);
        GUILayout.Label(message, style); GUILayout.FlexibleSpace();
        if (answering)
        {
            if (GUILayout.Button("[1 / Y] " + yesLabel, GUILayout.Height(52))) ChooseYes();
            if (GUILayout.Button("[2 / N] " + noLabel, GUILayout.Height(52))) ChooseNo();
            if (GUILayout.Button("Leave conversation [Esc]", GUILayout.Height(30))) Cancel();
        }
        else if (GUILayout.Button("Continue [Space]", GUILayout.Height(44))) FinishReply();
        GUILayout.EndArea();
    }
    void OnDisable()
    {
        if (open) Complete(answering ? onCancel : replyDone);
    }
    void OnDestroy()
    {
        if (yesButton != null) yesButton.onClick.RemoveListener(ChooseYes);
        if (noButton != null) noButton.onClick.RemoveListener(ChooseNo);
        if (Instance == this) Instance = null;
    }
}
