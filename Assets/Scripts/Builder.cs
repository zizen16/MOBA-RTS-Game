using UnityEngine;

public class Builder : Worker
{
    public BuildingData[] buildableBuildingData;
    BuildingData currentBuildingData;
    Vector3 buildPosition;
    GameObject buildingUnderConstruction;

    float buildTimer;
    
    void Update()
    {
        if (currentState == WorkerState.MovingToBuild && HasArrived())
        {
            currentState = WorkerState.Building;
            buildTimer = 0;
        }
        else if (currentState == WorkerState.Building)
        {
            buildTimer += Time.deltaTime;
            if (buildTimer >= currentBuildingData.buildTime)
            {
                //Finish
                FinishBuilding();
            }
        }
    }
    public bool CanBeMoved() => currentState != WorkerState.Building;
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
        currentState = WorkerState.MovingToBuild;
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
        currentState = WorkerState.Idle;
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
        currentState = WorkerState.Idle;
    }
}
