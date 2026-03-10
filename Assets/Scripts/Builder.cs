using UnityEngine;

public class Builder : Worker
{
    public BuildingData[] buildableBuildingData;
    BuildingData currentBuildingData;
    Vector3 buildPosition;
    GameObject buildingUnderConstruction;

    float buildTimer;

    public bool isExploring = false;
    public BuildingData explorationBuildingData;
    
    protected virtual void Update()
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

        // Exploration logic
        if (isExploring && currentState == WorkerState.Idle)
        {
            isExploring = false;
            if (ShouldBuildPylonHere())
            {
                GameObject construction = Instantiate(explorationBuildingData.constructionPrefab, transform.position, Quaternion.identity);
                AssignBuildingTask(transform.position, explorationBuildingData, construction);
            }
        }
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
        GameObject finishedBuilding = Instantiate(currentBuildingData.finishedPrefab, buildPosition, buildingUnderConstruction.transform.rotation);
        BaseBuilding bb = finishedBuilding.GetComponent<BaseBuilding>();
        if (bb != null)
        {
            bb.isAIOwned = isEnemyUnit;
            
            // Register the completed building with AIManager if AI-owned
            if (isEnemyUnit && AIManager.Instance != null)
            {
                AIManager.Instance.RegisterAIBuilding(bb);
                if (currentBuildingData != null && !AIManager.Instance.completedBuildings.Contains(currentBuildingData))
                {
                    AIManager.Instance.completedBuildings.Add(currentBuildingData);
                    AIManager.Instance.AddPopulationFromBuildings(currentBuildingData.populationProvided);
                    Debug.Log($"[Builder] Completed building: {currentBuildingData.buildingName}");
                }
            }
        }
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

    bool ShouldBuildPylonHere()
    {
        CommandCenter cc = AIManager.Instance.FindCommandCenter();
        if (cc == null) return false;

        float dist = Vector3.Distance(transform.position, cc.transform.position);
        if (dist < 50f) return false; // too close to base

        // check no AI buildings within 20 units
        foreach (var building in AIManager.Instance.GetAllBuildings())
        {
            if (Vector3.Distance(transform.position, building.transform.position) < 20f)
                return false;
        }

        return true;
    }
}
