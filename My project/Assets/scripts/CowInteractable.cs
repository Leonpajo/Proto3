using UnityEngine;
using UnityEngine.Events;

/// Put this on a CHILD object of the cow that has a BoxCollider (Is Trigger ticked),
/// positioned/sized in front of the cow. The cow (parent) needs WalkingPrefab.
[RequireComponent(typeof(Collider))]
public class CowInteractable : MonoBehaviour
{
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E;   // (new Input System: fixed to E)

    [Tooltip("One of these is picked at random.")]
    [TextArea]
    public string[] messages =
    {
        "Moo? Care to go on a date?",
        "This cow has taken a liking to you. Accept?",
        "Psst... want to share some fresh hay with me?",
        "Moo... is it me, or is it getting warmer in here?",
        "I've been told I have a very nice pair of horns. Want to see?",
        "I know a great meadow. Just you, me, and a lot of clover.",
        "Don't tell the other cows, but you're my favorite human.",
        "You're udderly charming. Dinner?",
        "Come here often? I'm usually in the field, chewing."
    };
    [Tooltip("ON: each cow keeps one line. OFF: a new random line every time you talk to it.")]
    public bool pickOnceAtSpawn = false;

    [Header("Replies after answering")]
    [Tooltip("One of these is shown (no buttons) after you press Yes. Empty = no reply.")]
    public string[] yesReplies =
    {
        "Moo-velous! Meet me by the water trough at sunset.",
        "Yes! I'll bring the fresh hay.",
        "You've made this cow very happy. Moo!"
    };
    [Tooltip("One of these is shown (no buttons) after you press No. Empty = no reply.")]
    public string[] noReplies =
    {
        "Moo... I understand. Plenty of cows in the field.",
        "Oh. That's... udderly disappointing.",
        "Fair enough. I'll go back to chewing."
    };
    public float replyDuration = 1.5f;
    public GameObject promptObject;           // optional "Press E" text/icon, child of the cow

    public UnityEvent onYes;
    public UnityEvent onNo;

    WalkingPrefab walker;
    bool playerInRange;
    string chosenMessage;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        walker = GetComponentInParent<WalkingPrefab>();
        SetPrompt(false);
        if (pickOnceAtSpawn) chosenMessage = RandomMessage();
    }

    string RandomMessage()
    {
        if (messages == null || messages.Length == 0) return "...";
        return messages[Random.Range(0, messages.Length)];
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
        DialogueUI.Instance.Show(pickOnceAtSpawn ? chosenMessage : RandomMessage(), () => Answer(true), () => Answer(false));
    }

    void Answer(bool yes)
    {
        if (yes) onYes.Invoke(); else onNo.Invoke();

        string[] pool = yes ? yesReplies : noReplies;
        if (pool != null && pool.Length > 0)
        {
            string reply = pool[Random.Range(0, pool.Length)];
            DialogueUI.Instance.ShowReply(reply, replyDuration, Finish);   // cow stays stopped meanwhile
        }
        else Finish();
    }

    void Finish()
    {
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