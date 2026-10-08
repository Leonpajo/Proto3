using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabSpawner : MonoBehaviour
{
    public WalkingPrefab prefab;
    [Min(0.2f)] public float spawnInterval = 6f;
    [Tooltip("Total spawn limit; zero keeps spawning as cows leave.")]
    public int maxSpawned = 0;
    [Min(1)] public int maxActive = 6;
    [Min(0)] public float minimumSpacing = 2f;
    public float walkDistance = 10f;
    public float walkSpeed = 1.2f;
    public float endWaitTime = 25f;
    int spawnedCount;
    readonly List<WalkingPrefab> active = new List<WalkingPrefab>();
    void Start()
    {
        if (prefab == null) { Debug.LogError("PrefabSpawner: assign a cow prefab with WalkingPrefab.", this); return; }
        StartCoroutine(SpawnLoop());
    }
    IEnumerator SpawnLoop()
    {
        while (maxSpawned <= 0 || spawnedCount < maxSpawned)
        {
            active.RemoveAll(cow => cow == null);
            bool clear = true;
            foreach (var cow in active) if (Vector3.Distance(cow.transform.position, transform.position) < minimumSpacing) { clear = false; break; }
            if (!CowDatingGame.Blocked && active.Count < Mathf.Max(1, maxActive) && clear)
            {
                WalkingPrefab cow = Instantiate(prefab, transform.position, transform.rotation);
                cow.walkDistance = walkDistance; cow.speed = walkSpeed; cow.endWaitTime = endWaitTime;
                active.Add(cow); spawnedCount++;
            }
            yield return new WaitForSeconds(Mathf.Max(0.2f, spawnInterval));
        }
    }
    void OnDrawGizmos()
    {
        Vector3 end = transform.position + transform.forward * walkDistance;
        Gizmos.color = Color.green; Gizmos.DrawLine(transform.position, end); Gizmos.DrawWireSphere(end, 0.3f);
    }
}
