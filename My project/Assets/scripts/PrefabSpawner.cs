using System.Collections;
using UnityEngine;

/// Put this on an empty GameObject. That object's position = spawn point,
/// and its blue arrow (forward) = the direction the prefabs walk.
public class PrefabSpawner : MonoBehaviour
{
    [Header("Spawning")]
    public WalkingPrefab prefab;          // prefab must have the WalkingPrefab script
    public float spawnInterval = 3f;      // seconds between spawns
    public int maxSpawned = 0;            // 0 = unlimited

    [Header("Walking")]
    public float walkDistance = 10f;      // how far they walk (adjustable)
    public float walkSpeed = 2f;

    int spawnedCount;

    void Start() => StartCoroutine(SpawnLoop());

    IEnumerator SpawnLoop()
    {
        while (maxSpawned == 0 || spawnedCount < maxSpawned)
        {
            Spawn();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void Spawn()
    {
        WalkingPrefab w = Instantiate(prefab, transform.position, transform.rotation);
        w.walkDistance = walkDistance;
        w.speed = walkSpeed;
        spawnedCount++;
    }

    // Shows the walk path in the Scene view so you can place the trigger at the end
    void OnDrawGizmos()
    {
        Vector3 end = transform.position + transform.forward * walkDistance;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, end);
        Gizmos.DrawWireSphere(end, 0.3f);
    }
}
