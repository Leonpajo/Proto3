using UnityEngine;

/// Put this on the cow (the spawned prefab).
public class WalkingPrefab : MonoBehaviour
{
    public float speed = 2f;
    public float walkDistance = 10f;   // overwritten by the spawner

    Vector3 startPos;
    bool paused;
    bool reachedEnd;

    void Start() => startPos = transform.position;

    void Update()
    {
        if (paused || reachedEnd) return;

        transform.position += transform.forward * speed * Time.deltaTime;

        if (Vector3.Distance(startPos, transform.position) >= walkDistance)
            reachedEnd = true;
    }

    public void Pause() => paused = true;    // stop while being talked to
    public void Resume() => paused = false;   // carries on (unless it already finished its walk)
}