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

    public Material builder;
    private string shaderPropertyName = "_Slider";
    
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
        // 1. Safety Check
        if (builder == null)
        {
            Debug.LogWarning("Builder Material is missing! Cannot update shader.");
            return;
        }

        if (currentState == WorkerState.MovingToBuild && HasArrived())
        {
            currentState = WorkerState.Building;
            buildTimer = 0;
            // Optional: Ensure the shader starts at the "empty" value
            builder.SetFloat(shaderPropertyName, -1.0f); 
        }
        else if (currentState == WorkerState.Building)
        {
            buildTimer += Time.deltaTime;

            // 2. Map the timer to the Shader's 1.5 target
            // This calculates progress from 0 to 1.5 based on buildTime
            float progress = (buildTimer / currentBuildingData.buildTime) * 1.5f;
            
            // 3. Send the value to the Material
            builder.SetFloat(shaderPropertyName, progress);

            if (progress >= 1.5f)
            {
                FinishBuilding();
                // Optional: Snap it exactly to 1.5 at the end
                builder.SetFloat(shaderPropertyName, 1.5f); 
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
                AIManager.Instance.OnBuildingCompleted(currentBuildingData);
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
