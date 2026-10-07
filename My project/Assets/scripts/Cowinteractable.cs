using UnityEngine;
using UnityEngine.Events;

/// Put this on a CHILD object of the cow that has a BoxCollider (Is Trigger ticked),
/// positioned/sized in front of the cow. The cow (parent) needs WalkingPrefab.
[RequireComponent(typeof(Collider))]
public class CowInteractable : MonoBehaviour
{
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E;   // (new Input System: fixed to E)

    [TextArea] public string message = "Do you want to continue?";
    public GameObject promptObject;           // optional "Press E" text/icon, child of the cow

    public UnityEvent onYes;
    public UnityEvent onNo;

    WalkingPrefab walker;
    bool playerInRange;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        walker = GetComponentInParent<WalkingPrefab>();
        SetPrompt(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInRange = true;
        SetPrompt(!DialogueUI.IsOpen);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInRange = false;
        SetPrompt(false);
    }

    void Update()
    {
        if (playerInRange && !DialogueUI.IsOpen && InteractPressed())
            Interact();
    }

    void Interact()
    {
        if (walker != null) walker.Pause();   // cow stops while you talk
        SetPrompt(false);
        DialogueUI.Instance.Show(message, () => Answer(true), () => Answer(false));
    }

    void Answer(bool yes)
    {
        if (yes) onYes.Invoke(); else onNo.Invoke();
        if (walker != null) walker.Resume();  // cow walks again
        SetPrompt(playerInRange);
    }

    void SetPrompt(bool on)
    {
        if (promptObject != null) promptObject.SetActive(on);
    }

    bool InteractPressed()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        return UnityEngine.InputSystem.Keyboard.current != null
            && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(interactKey);
#endif
    }
}