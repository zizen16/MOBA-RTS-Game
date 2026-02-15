using UnityEngine;
using UnityEngine.AI;

public class Worker : BaseUnit
{
    public enum WorkerState { Idle, MovingToBuild, Building, MovingToResource, Gathering, ReturningToBase }
    public WorkerState currentState = WorkerState.Idle;
    public BuildingData[] buildableBuildingData;
    public bool isPlacing; // True when construction preview is following cursor

    BuildingData currentBuildingData;
    Vector3 buildPosition;
    GameObject buildingUnderConstruction;

    float buildTimer;

    //Gathering variables would go here
    GoldResource targetResource;
    CommandCenter targetCommandCenter;
    int carriedGold = 0;
    float gatherTimer = 0f;
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
        else if (currentState == WorkerState.MovingToResource && HasArrived())
        {
            StartGathering();
        }
        else if (currentState == WorkerState.Gathering)
        {
            gatherTimer += Time.deltaTime;
            if (gatherTimer >= targetResource.gatheringTime)
            {
                FinishGathering();
                gatherTimer = 0;
            }
        }
        else if (currentState == WorkerState.ReturningToBase && HasArrived())
        {
            DepositGold();
        }
    }
    public bool CanBeMoved() => currentState != WorkerState.Building && currentState != WorkerState.Gathering;
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
    public void AssignGatheringTask(GoldResource resource)
    {
        CancelConstruction();
        CancelGathering();
        targetResource = resource;
        if (isEnemyUnit) //STEP 46(A): In the AssignGatheringTask() method, add logic to determine the appropriate command center for the worker to return to after gathering resources, based on whether it's an enemy unit or not.
        {
            //targetCommandCenter = AIManager.Instance.FindCommandCenter(); // For enemy workers, find the AI's command center to return resources to.--------------------------------------------------------------------------------------------------------------------------------------
            //STEP 47 is in GOAPAction.cs
        }
        else
        {
            targetCommandCenter = PlayerManager.Instance.FindCommandCenter();

        }
        if (targetCommandCenter == null)
        {
            Debug.LogWarning("No command Center Found!");
            return;
        }
        currentState = WorkerState.MovingToResource;
        gatherTimer = 0;
        base.MoveTo(targetResource.transform.position);
    }
    void StartGathering()
    {
        if (targetResource == null || !targetResource.HasGold())
        {
            CancelGathering();
            return;
        }
        currentState = WorkerState.Gathering;
        gatherTimer = 0;
    }
    void FinishGathering()
    {
        if (targetResource != null)
        {
            carriedGold = targetResource.GatherGold();
        }
        gatherTimer = 0;
        if (targetCommandCenter != null)
        {
            currentState = WorkerState.ReturningToBase;
            base.MoveTo(targetCommandCenter.transform.position);
        }
        else
        {
            CancelGathering();
        }
    }
    void DepositGold()
    {
        if (carriedGold > 0)
        {
            if (isEnemyUnit) // //STEP 46(B): In the DepositGold() method, add logic to deposit the gathered gold to the appropriate resource pool based on whether it's an enemy unit or not.
            {
                //AIManager.Instance.AddGold(carriedGold);---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
            }
            else
            {
                PlayerManager.Instance.AddGold(carriedGold);
            }

            carriedGold = 0;
        }
        if (targetResource != null && targetResource.HasGold())
        {
            currentState = WorkerState.MovingToResource;
            base.MoveTo(targetResource.transform.position);
        }
        else
        {
            CancelGathering();
        }
    }
    public void CancelGathering()
    {
        targetResource = null;
        targetCommandCenter = null;
        carriedGold = 0;
        gatherTimer = 0;
        if (currentState == WorkerState.MovingToResource || currentState == WorkerState.Gathering || currentState == WorkerState.ReturningToBase)
        {
            currentState = WorkerState.Idle;
        }
    }
}
