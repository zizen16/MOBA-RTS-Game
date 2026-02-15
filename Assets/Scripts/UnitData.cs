using UnityEngine;
[CreateAssetMenu(fileName = "NewUnit", menuName = "RTS/Unit Data")]

public class UnitData : ScriptableObject
{
    [Header("Basic Info")]
    public string unitName = "New Unit";
    public Sprite icon;

    [Header("Prefab")]
    public GameObject unitPrefab;

    [Header("Stats")]
    public float maxHealth = 100f;

    [Header("Cost and Training")]
    public int goldCost = 50;
    public float trainTime = 5f;
    public int populationCost = 1;

}