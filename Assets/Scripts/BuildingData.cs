using UnityEngine;

[CreateAssetMenu(fileName = "NewBuilding", menuName = "RTS/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Basic Info")]
    public string buildingName = "New Building";
    public Sprite icon;

    [Header("Prefabs")]
    public GameObject constructionPrefab;
    public GameObject finishedPrefab;

    [Header("Construction")]
    public int goldCost = 200;
    public float buildTime = 10f;

    [Header("Stats")]
    public float maxHealth = 1000f;
    public int populationProvided = 5;

    [Header("Prerequisites")]
    public BuildingData[] prerequisiteBuildings; // Buildings that must be constructed before this one can be built
}
