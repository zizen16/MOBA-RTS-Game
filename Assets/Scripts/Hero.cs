using UnityEngine;

public class Hero : BaseUnit
{
    public enum HeroState {Idle, MovingToBuild, Building}
    public HeroState currentState = HeroState.Idle;
    public BuildingData[] buildableBuildingData;
    public bool isPlacing; // True when construction preview is following cursor

    BuildingData currentBuildingData;
    Vector3 buildPosition;
    GameObject buildingUnderConstruction;
    float buildTimer;

    void Update()
    {
        if (currentState == HeroState.MovingToBuild && HasArrived())
        {
            currentState = HeroState.Building;
            buildTimer = 0;
        }
        else if (currentState == HeroState.Building)
        {
            buildTimer += Time.deltaTime;
            if (buildTimer >= currentBuildingData.buildTime)
            {
                //Finish
                FinishBuilding();
            }
        }
    }
    public bool CanBeMoved() => currentState != HeroState.Building;
    bool HasArrived()
    {
        if (agent.pathPending) return false;
        return agent.remainingDistance <= agent.stoppingDistance;
    }
    public void AssignBuildingTask(Vector3 position, BuildingData data, GameObject constructionInstance)
    {
        buildPosition = position;
        currentBuildingData = data;
        buildingUnderConstruction = constructionInstance;

        buildingUnderConstruction.transform.position = buildPosition;
        currentState = HeroState.MovingToBuild;
        isPlacing = false;
        base.MoveTo(position);
    }
    void FinishBuilding()
    {
        Instantiate(currentBuildingData.finishedPrefab, buildPosition, buildingUnderConstruction.transform.rotation);
        Destroy(buildingUnderConstruction);
        buildingUnderConstruction = null;
        currentBuildingData = null;
        buildTimer = 0;
        currentState = HeroState.Idle;
    }
    public void CancelConstruction()
    {
        if (buildingUnderConstruction != null)
        {
            Destroy(buildingUnderConstruction);
            buildingUnderConstruction = null;
        }
        currentBuildingData = null;
        buildTimer = 0;
        isPlacing = false;
        currentState = HeroState.Idle;
    }
}
