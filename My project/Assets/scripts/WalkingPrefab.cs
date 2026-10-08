using UnityEngine;

public class WalkingPrefab : MonoBehaviour
{
    public float speed = 2f;
    public float walkDistance = 10f;
    [Tooltip("Waiting time at the end before leaving. Zero keeps the cow in the scene.")]
    public float endWaitTime = 25f;
    Vector3 startPos, direction;
    bool paused, reachedEnd;
    float waited;
    void Start() { startPos = transform.position; direction = transform.forward; }
    void Update()
    {
        if (paused || CowDatingGame.Paused || (CowDatingGame.Instance != null && CowDatingGame.Blocked && !DialogueUI.IsOpen && !SlapHand.IsPlaying)) return;
        if (reachedEnd)
        {
            if (endWaitTime > 0 && (waited += Time.deltaTime) >= endWaitTime) Destroy(gameObject);
            return;
        }
        float traveled = Vector3.Dot(transform.position - startPos, direction);
        float step = Mathf.Min(Mathf.Max(0, speed) * Time.deltaTime, Mathf.Max(0, walkDistance - traveled));
        transform.position += direction * step;
        if (traveled + step >= Mathf.Max(0, walkDistance)) reachedEnd = true;
    }
    public void Pause() => paused = true;
    public void Resume() => paused = false;
}
