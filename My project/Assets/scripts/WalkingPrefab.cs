using UnityEngine;

/// Put this on the prefab that gets spawned.
/// Also give the prefab a Collider and a Rigidbody (tick Is Kinematic)
/// so the trigger zone can detect it.
public class WalkingPrefab : MonoBehaviour
{
    public float speed = 2f;
    public float walkDistance = 10f;   // overwritten by the spawner

    Vector3 startPos;
    bool walking = true;

    void Start() => startPos = transform.position;

    void Update()
    {
        if (!walking) return;

        transform.position += transform.forward * speed * Time.deltaTime;

        if (Vector3.Distance(startPos, transform.position) >= walkDistance)
            walking = false;   // reached the end of its walk
    }

    public void Pause()  => walking = false;
    public void Resume() => walking = true;
}
