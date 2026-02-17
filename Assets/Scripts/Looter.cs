using UnityEngine;

public class Looter : Worker
{
    GoldResource targetResource;
    CommandCenter targetCommandCenter;
    int carriedGold = 0;
    float gatherTimer = 0f;
    void Update()
    {
        
        if (currentState == WorkerState.MovingToResource && HasArrived())
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
    public bool CanBeMoved() => currentState != WorkerState.Gathering;
    bool HasArrived()
    {
        if (agent.pathPending) return false;
        return agent.remainingDistance <= agent.stoppingDistance;
    }
    
    public void AssignGatheringTask(GoldResource resource)
    {
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
