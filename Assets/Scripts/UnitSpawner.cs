using System.Collections;
using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    [Header("Spawner Info")]
    public UnitData[] spawnableUnits;
    public Transform spawnPoint;

    class UnitQueue
    {
        public int queued = 0;
        public float remaining = 0;
        public bool isTraining = false;
        public Coroutine coroutine = null;
    }
    UnitQueue[] unitQueues;
    void Awake()
    {
        EnsureQueueMatchUnits();
    }
    void EnsureQueueMatchUnits()
    {
        int len = spawnableUnits != null ? spawnableUnits.Length : 0;
        if (unitQueues == null || unitQueues.Length != len)
        {
            var newArr = new UnitQueue[len];
            if (unitQueues != null)
            {
                for (int i = 0; i < Mathf.Min(len, unitQueues.Length); i++)
                {
                    newArr[i] = unitQueues[i];
                }
            }
            unitQueues = newArr;
        }
    }
    public void EnqueueUnit(int unitIndex)
    {
        if (unitIndex < 0 || spawnableUnits == null || unitIndex >= spawnableUnits.Length)
        {
            Debug.LogError("Invalid unit index enqueued: " + unitIndex);
            return;
        }
        var data = spawnableUnits[unitIndex];
        if (data == null)
        {
            Debug.LogError("No UnitData found at index: " + unitIndex);
            return;
        }
        if (!PlayerManager.Instance.HasEnoughGold(data.goldCost))
        {
            Debug.LogWarning("Not enough gold to train unit: " + data.unitName);
            return;
        }
        if (!PlayerManager.Instance.HasPopulationForUnit(data.populationCost))
        {
            Debug.LogWarning("Not enough population capacity to train unit: " + data.unitName);
            return;
        }
        PlayerManager.Instance.SpendGold(data.goldCost);
        PlayerManager.Instance.RegisterUnit(data);

        EnsureQueueMatchUnits();
        if (unitQueues[unitIndex] == null)
        {
            unitQueues[unitIndex] = new UnitQueue();
        }
        var queue = unitQueues[unitIndex];
        queue.queued++;
        if (!queue.isTraining)
        {
            queue.coroutine = StartCoroutine(TrainUnitCoroutine(unitIndex));
        }
    }
    IEnumerator TrainUnitCoroutine(int unitIndex)
    {
        var queue = unitQueues[unitIndex];
        var data = spawnableUnits[unitIndex];
        queue.isTraining = true;

        while (queue.queued > 0)
        {
            float remaining = data.trainTime;
            queue.remaining = remaining;
            while (queue.remaining > 0)
            {
                remaining -= Time.deltaTime;
                queue.remaining = Mathf.Max(0, remaining);
                yield return null;
            }
            SpawnUnit(data); // spawn the unit
            queue.queued--;
            queue.remaining = 0;
            yield return null;// small frame delay
        }
        queue.isTraining = false;
        queue.coroutine = null;
    }
    public int GetQueuedCount(int unitIndex)
    {
        if (unitIndex < 0 || unitQueues == null || unitIndex >= unitQueues.Length) return 0;
        var q = unitQueues[unitIndex];
        return q != null ? q.queued : 0;
    }
    public float GetRemainingTime(int unitIndex)
    {
        if (unitIndex < 0 || unitQueues == null || unitIndex >= unitQueues.Length) return 0f;
        var q = unitQueues[unitIndex];
        return q != null ? q.remaining : 0f;
    }
    public GameObject SpawnUnit(UnitData data)
    {
        if (data == null || data.unitPrefab == null)
        {
            Debug.LogError("Invalid UnitData or prefab when spawning unit.");
            return null;
        }
        GameObject unitInstance = Instantiate(data.unitPrefab, spawnPoint.position, Quaternion.identity);
        return unitInstance;
    }
    void OnDrawGizmosSelected()
    {
        // Draw spawn point
        Transform point = spawnPoint != null ? spawnPoint : transform;
        Gizmos.DrawWireSphere(point.position, 0.5f);
        Gizmos.DrawLine(point.position, point.position + point.forward * 2f);
    }
}