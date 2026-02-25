using System.Collections.Generic;
using UnityEngine;

public class NeutralCreepSpawner : MonoBehaviour
{
    public GameObject neutralCreepPrefab;
    public List<GameObject> creepList = new List<GameObject>();
    private GameObject[] creepArray = new GameObject[5];

    public Transform spawnPoint;
    public float spawnInterval = 5f;
    private float lastSpawnTime = 0f;
    void Start()
    {
        //SpawnCreeps();
    }

    // Update is called once per frame
    void Update()
    {
        // make sure we don't keep destroyed creeps in the list
        CleanUpList();

        if (IsEmptyCreeps())
        {
            lastSpawnTime += Time.deltaTime;

            if (lastSpawnTime >= spawnInterval)
            {
                SpawnCreeps();
                lastSpawnTime = 0f;
            }
        }
    }

    void SpawnCreeps()
    {
        for (int i = 0; i < 3; i++)
        {
            creepArray[i] = Instantiate(neutralCreepPrefab, spawnPoint.position, Quaternion.identity);
            creepList.Add(creepArray[i]);
        }
    }

    // removes any entries that are null (destroyed creeps)
    void CleanUpList()
    {
        // RemoveAll handles multiple at once and is safe even if list is already empty
        creepList.RemoveAll(item => item == null);
    }

    /// <summary>
    /// Call this from a creep when it dies/killed to ensure it's removed immediately.
    /// </summary>
    public void RemoveCreep(GameObject creep)
    {
        if (creepList.Remove(creep))
        {
            // optionally log or perform additional logic
            Debug.Log("Creep removed from spawner list: " + creep.name);
        }
    }
    bool IsEmptyCreeps()
    {
        CleanUpList();
        return creepList.Count == 0;
    }
}
