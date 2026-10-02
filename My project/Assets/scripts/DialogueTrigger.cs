using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// Put this on an empty GameObject with a BoxCollider (tick Is Trigger),
/// placed in front of where the prefabs stop walking.
[RequireComponent(typeof(Collider))]
public class DialogueTrigger : MonoBehaviour
{
    [Header("UI (assign from your Canvas)")]
    public GameObject panel;           // the text box panel (starts hidden)
    public TMP_Text messageText;
    public Button yesButton;
    public Button noButton;

    [Header("Dialogue")]
    [TextArea] public string message = "Do you want to continue?";
    public bool destroyWalkerAfterAnswer = true;

    [Header("Events (hook up your own logic)")]
    public UnityEvent<GameObject> onYes;
    public UnityEvent<GameObject> onNo;

    readonly Queue<WalkingPrefab> waiting = new Queue<WalkingPrefab>();
    WalkingPrefab current;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        panel.SetActive(false);
        yesButton.onClick.AddListener(() => Answer(true));
        noButton.onClick.AddListener(() => Answer(false));
    }

    void OnTriggerEnter(Collider other)
    {
        WalkingPrefab walker = other.GetComponentInParent<WalkingPrefab>();
        if (walker == null) return;

        walker.Pause();
        waiting.Enqueue(walker);
        if (current == null) ShowNext();
    }

    void ShowNext()
    {
        if (waiting.Count == 0) { current = null; return; }

        current = waiting.Dequeue();
        messageText.text = message;
        panel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Answer(bool yes)
    {
        GameObject walkerObj = current != null ? current.gameObject : null;

        if (yes) onYes.Invoke(walkerObj);
        else     onNo.Invoke(walkerObj);

        if (walkerObj != null && destroyWalkerAfterAnswer) Destroy(walkerObj);

        panel.SetActive(false);
        current = null;
        ShowNext();   // next walker in line, if any
    }
}
