using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class CowInteractable : MonoBehaviour
{
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E;
    [TextArea] public string[] messages = { "Moo! Fancy a chat between classes?" };
    public bool pickOnceAtSpawn = false;
    public string[] yesReplies = { "See you at the cafe. Moo!" };
    public string[] noReplies = { "No worries. Have a good school day!" };
    public float replyDuration = 3f;
    public GameObject promptObject;
    [Tooltip("Optional slap gag for turning down an invitation. Existing prefab values are preserved.")]
    public bool slapOnNo = false;
    public UnityEvent onYes, onNo;
    [Header("Dating gameplay")]
    public string cowName;
    [Range(-1, 2), Tooltip("-1 gives this cow a random personality; 0 study, 1 music, 2 cafe.")]
    public int personality = -1;
    [Min(0.5f)] public float interactionDistance = 3f;
    [Min(0)] public float conversationCooldown = 2f;
    public bool randomizeNames = true;
    static readonly List<CowInteractable> cows = new List<CowInteractable>();
    static readonly string[] names = { "Milla", "Helmi", "Aino", "Onni", "Meri", "Toivo", "Sisu", "Lumi" };
    WalkingPrefab walker;
    Collider zone;
    string chosenMessage;
    int stage; // 0 introduction, 1 learned interest, 2 date accepted
    bool talking;
    float nextTalk;
    public string DisplayName => string.IsNullOrWhiteSpace(cowName) ? "Cow" : cowName;
    public string Status => stage == 2 ? "Date booked!" : stage == 1 ? "Ask about a cafe date" : "Get to know this cow";
    void Awake()
    {
        zone = GetComponent<Collider>(); zone.isTrigger = true;
        walker = GetComponentInParent<WalkingPrefab>();
        if (string.IsNullOrWhiteSpace(cowName)) cowName = randomizeNames ? names[Random.Range(0, names.Length)] : "Cow";
        if (personality < 0) personality = Random.Range(0, 3);
        personality = Mathf.Clamp(personality, 0, 2);
        chosenMessage = RandomLine(messages, "Moo! Nice to meet you.");
        SetPrompt(false);
    }
    void OnEnable() { if (!cows.Contains(this)) cows.Add(this); }
    void OnDisable()
    {
        cows.Remove(this); SetPrompt(false);
        if (talking && DialogueUI.Instance != null) DialogueUI.Instance.Cancel();
        if (walker != null) walker.Resume();
    }
    public static CowInteractable FindFocused()
    {
        Camera camera = Camera.main;
        if (camera == null) return null;
        CowInteractable best = null;
        float bestScore = float.MaxValue;
        foreach (var cow in cows)
        {
            if (cow == null || cow.talking || Time.time < cow.nextTalk || cow.zone == null || !cow.zone.enabled) continue;
            Vector3 point = cow.zone.bounds.center;
            Vector3 offset = point - camera.transform.position;
            float distance = Vector3.Distance(camera.transform.position, cow.zone.ClosestPoint(camera.transform.position));
            float angle = Vector3.Angle(camera.transform.forward, offset);
            if (distance > cow.interactionDistance || angle > 38) continue;
            // A wall or another cow between the camera and target blocks talking.
            bool blocked = false;
            foreach (var hit in Physics.RaycastAll(camera.transform.position, offset.normalized, offset.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<FirstPersonController>() != null) continue;
                Transform cowRoot = cow.walker != null ? cow.walker.transform : cow.transform;
                if (hit.transform == cowRoot || hit.transform.IsChildOf(cowRoot)) continue;
                blocked = true; break;
            }
            if (blocked) continue;
            float score = angle + distance * 3;
            if (score < bestScore) { best = cow; bestScore = score; }
        }
        return best;
    }
    void LateUpdate() => SetPrompt(!CowDatingGame.Blocked && FindFocused() == this);
    public void Interact()
    {
        if (talking || CowDatingGame.Blocked || Time.time < nextTalk) return;
        if (DialogueUI.Instance == null) new GameObject("Dialogue UI (fallback)").AddComponent<DialogueUI>();
        talking = true; if (walker != null) walker.Pause(); SetPrompt(false);
        if (stage == 2)
        {
            DialogueUI.Instance.ShowReply(DisplayName + ": Our cafe date is booked. See you after class!", replyDuration, Finish);
            return;
        }
        if (stage == 0)
        {
            string intro = pickOnceAtSpawn ? chosenMessage : RandomLine(messages, "Moo! Nice to meet you.");
            string interest = personality == 0 ? "I've got a business presentation tomorrow. I'm a little nervous." :
                personality == 1 ? "I love music. Every school day needs a good playlist." : "A cafe break is my favorite part of the day. Fresh hay snacks are a bonus.";
            DialogueUI.Instance.ShowChoice(DisplayName + ": " + intro + "\n\n" + interest,
                "Ask about their interests", "Say hello and leave", Introduce, () => Reply("Nice meeting you. Come back when you have time!"), Finish);
        }
        else
        {
            bool firstGood = personality != 1;
            string first = personality == 0 ? "Offer to practice the presentation together" : personality == 1 ? "Talk only about yourself" : "Suggest a cafe break and hay snacks";
            string second = personality == 0 ? "Tell them studying is boring" : personality == 1 ? "Ask to share playlists over a cafe break" : "Rush them and ignore their interests";
            DialogueUI.Instance.ShowChoice(DisplayName + ": Got a plan for after class?",
                first, second, () => DateAnswer(firstGood), () => DateAnswer(!firstGood), Finish);
        }
    }
    void Introduce()
    {
        if (this == null) return;
        stage = 1;
        string hint = personality == 0 ? "I'd love someone to practice my presentation with." : personality == 1 ? "Ask me about music! Maybe we could swap playlists." : "Let's keep it relaxed. A cafe break and snacks sounds perfect.";
        Reply(hint + "\nTalk to me again to suggest a date.");
    }
    void DateAnswer(bool good)
    {
        if (this == null) return;
        if (good)
        {
            stage = 2;
            if (CowDatingGame.Instance != null) CowDatingGame.Instance.RecordDate(DisplayName);
            onYes?.Invoke();
            if (this == null || !isActiveAndEnabled) return;
            Reply(RandomLine(yesReplies, "It's a date! See you after class."));
        }
        else
        {
            onNo?.Invoke();
            if (this == null || !isActiveAndEnabled) return;
            if (slapOnNo && SlapHand.Instance != null) SlapHand.Instance.Play(null, Rejection);
            else Rejection();
        }
    }
    void Rejection()
    {
        if (this == null || !isActiveAndEnabled) return;
        Reply(RandomLine(noReplies, "That doesn't sound like my kind of date.") + "\nTry a plan that matches my interests.");
    }
    void Reply(string line)
    {
        if (this == null || !isActiveAndEnabled) return;
        if (DialogueUI.Instance != null) DialogueUI.Instance.ShowReply(DisplayName + ": " + line, Mathf.Max(replyDuration, line.Length / 28f), Finish);
        else Finish();
    }
    void Finish()
    {
        if (this == null) return;
        talking = false; nextTalk = Time.time + conversationCooldown;
        if (walker != null) walker.Resume();
    }
    static string RandomLine(string[] pool, string fallback)
    {
        if (pool == null || pool.Length == 0) return fallback;
        string line = pool[Random.Range(0, pool.Length)];
        return string.IsNullOrWhiteSpace(line) ? fallback : line;
    }
    void SetPrompt(bool value) { if (promptObject != null && promptObject != gameObject) promptObject.SetActive(value); }
}
