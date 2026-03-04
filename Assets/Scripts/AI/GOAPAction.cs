using System.Collections.Generic;
using UnityEngine;

public abstract class GOAPAction // STEP 33: Create a base GOAPAction class that defines the structure for all actions,
// including methods for checking preconditions, executing the action, and calculating utility.
//This will be the foundation for all specific actions the AI can take.
{
    public string actionName = "Unnamed Action"; // A human-readable name for the action, useful for debugging and understanding what the AI is doing.
    public float cost = 1f; // The cost of performing this action, which can be used in utility calculations to prefer cheaper actions when possible.
    public bool isRunning = false; // A flag to indicate if the action is currently being executed, which can help prevent the AI from starting multiple actions at once or interrupting an action before it's complete.
    public float cooldown = 0f; // A cooldown time that must pass before this action can be executed again, which can help prevent spamming certain actions and encourage more varied behavior.
    float lastExecutionTime = -999f; // Tracks the last time this action was executed, used in conjunction with cooldown to determine if the action can be performed again.

    public virtual bool CheckPreconditions(AIWorldState worldState) //STEP 34: Implement the CheckPreconditions() method to determine if the action can be performed based on the current world state.
    // This will be overridden by specific actions to check for conditions relevant to those actions, such as having enough resources or the right buildings.
    {
        if (Time.time - lastExecutionTime < cooldown)
            return false;

        return true;
    }
    public virtual bool Execute(AIManager aiManager) //STEP 35: Implement the Execute() method to perform the action.
    // This will be overridden by specific actions to carry out the necessary steps to achieve the action's goal, such as training a unit or gathering resources.
    // The method should return true if the action was successfully started, or false if it failed (e.g., due to changing conditions).
    {
        lastExecutionTime = Time.time;
        return true;
    }
    public virtual bool IsComplete() //STEP 36: Implement the IsComplete() method to determine if the action has finished executing.
    // This is important for actions that take time to complete, such as training a unit, so that the AI can wait for the action to finish before starting another one or re-evaluating its decisions.
    {
        return true;
    }
    public virtual float CalculateUtility(AIWorldState worldState) //STEP 37: Implement the CalculateUtility() method to assign a utility score to this action based on the current world state. 
    //This will allow the AI to evaluate which actions are more beneficial to take in the current situation, and choose the one with the highest utility.
    // The utility can be influenced by factors such as how well the action helps achieve current goals, its cost, and any relevant conditions in the world state.
    {
        return 1f / Mathf.Max(cost, 0.1f);// Basic utility calculation that favors lower-cost actions, but can be overridden by specific actions to provide more complex utility evaluations based on the world state.
    }

}

public enum WorkerRole { Generic, Builder, Looter, Hero }

//Action: Train a worker unit from the Command Center with specific roles.
//Utility: Varies depending on type of worker being requested (builder, looter, hero).
public class TrainWorkerAction : GOAPAction //STEP 38: Create specific action classes that inherit from GOAPAction,
// such as TrainWorkerAction and GatherResourceAction, and implement their CheckPreconditions(), Execute(), IsComplete(), and CalculateUtility() methods based on the specific conditions and effects of those actions.
{
    UnitData workerData; // Reference to the data for the worker unit, which will be used to check costs and train the correct unit when executing the action.
    public WorkerRole role = WorkerRole.Generic;

    public TrainWorkerAction(UnitData data, WorkerRole role = WorkerRole.Generic) //set action name, cost, cooldown, and store reference to worker data in the constructor.
    {
        this.role = role;
        workerData = data;
        switch (role)
        {
            case WorkerRole.Builder:
                actionName = "Train Builder";
                break;
            case WorkerRole.Looter:
                actionName = "Train Looter";
                break;
            default:
                actionName = "Train Worker";
                break;
        }
        cost = 2f;
        cooldown = 3f;
    }
    public override bool CheckPreconditions(AIWorldState worldState)//STEP 39: In the CheckPreconditions() method, add logic to check if the AI has a Command Center, enough gold to train the worker, and population capacity to support another unit.
    {
        if (!base.CheckPreconditions(worldState)) return false;
        if (!worldState.aiHasCommandCenter) return false;
        if (workerData != null && worldState.aiGold < workerData.goldCost) return false;
        if (worldState.aiCurrentPopulation >= worldState.aiMaxPopulation) return false;
        return true;
    }
    public override bool Execute(AIManager aiManager)//STEP 40: In the Execute() method, add logic to command the AIManager to train a worker unit using the provided worker data.
    // Return true if the training command was successfully issued, or false if it failed (e.g., due to changing conditions).
    {
        base.Execute(aiManager);
        if (workerData != null && aiManager.TrainUnitByData(workerData))
        {
            Debug.Log($"AI trained a {role.ToString().ToLower()}.");
            return true;
        }
        return false;
    }
    public override float CalculateUtility(AIWorldState worldState) //STEP 41: In the CalculateUtility() method, add logic to assign a high utility score when the worker count is very low,
    // and decrease the utility as the worker count approaches the desired number.
    //The utility can be influenced by factors such as how well the action helps achieve current goals, its cost, and any relevant conditions in the world state.
    {
        // base utility is cost-based fallback
        float utility = 1f / Mathf.Max(cost, 0.1f);

        switch (role)
        {
            case WorkerRole.Builder:
                utility = 10f;
                if (worldState.aiBuilderCount == 0)
                    utility += 30;
                else if (worldState.aiBuilderCount < 2)
                    utility += 10f;
                // if we already have many idle builders, there's less urgency to train more
                if (worldState.aiIdleBuilderCount > 1)
                    utility -= 10f;
                break;
            case WorkerRole.Looter:
                utility = 15f;
                if (worldState.aiLooterCount == 0)
                    utility += 25;
                else if (worldState.aiLooterCount < 3)
                    utility += 10f;
                if (worldState.aiIdleLooterCount > 1)
                    utility -= 10f;
                break;
            default:
                // generic worker uses the old logic
                utility = 10f;
                if (worldState.aiWorkerCount == 0)
                    utility += 50;
                else if (worldState.aiWorkerCount < 3)
                    utility += 20f;
                if (worldState.aiIdleWorkerCount > 2)
                    utility -= 15f;
                break;
        }

        return utility;
    }

}

//Action: Gather resources with specific worker role preference.
public class GatherResourceAction : GOAPAction //STEP 42: Create the GatherResourceAction class that inherits from GOAPAction and implement its methods to assign an idle worker to gather resources from the nearest available resource node.
{
    public WorkerRole preferredRole = WorkerRole.Looter; // Prefer looters, but fall back to any worker

    public GatherResourceAction(WorkerRole role = WorkerRole.Looter)
    {
        preferredRole = role;
        actionName = "Gather Resources";
        cost = 0.5f;
        cooldown = 1f;
    }
    public override bool CheckPreconditions(AIWorldState worldState) //STEP 43: In the CheckPreconditions() method, add logic to check if there are idle workers available and if there are any resources left on the map to gather.
    {
        if (!base.CheckPreconditions(worldState)) return false;
        if (worldState.aiIdleWorkerCount <= 0) return false;
        if (worldState.availableGoldResources <= 0) return false;
        return true;
    }

    public override bool Execute(AIManager aiManager) //STEP 44: In the Execute() method, add logic to find the nearest available resource node and assign one of the idle workers to gather from it.
    {
        base.Execute(aiManager);

        // Try to get preferred role workers first, then fall back to any idle worker
        List<Worker> idleWorkers = new List<Worker>();
        if (preferredRole == WorkerRole.Looter)
            idleWorkers = aiManager.GetIdleLooters();
        else if (preferredRole == WorkerRole.Builder)
            idleWorkers = aiManager.GetIdleBuilders();

        // Fall back to any idle worker if preferred role not available
        if (idleWorkers.Count == 0)
            idleWorkers = aiManager.GetIdleWorkers();

        if (idleWorkers.Count == 0) return false;

        GoldResource[] resources = Object.FindObjectsByType<GoldResource>(FindObjectsSortMode.None);
        GoldResource nearestResource = null;
        float nearestDistance = float.MaxValue;
        foreach (var resource in resources)
        {
            if (resource == null || !resource.HasGold()) continue;

            Worker worker = idleWorkers[0];
            float distance = Vector3.Distance(worker.transform.position, resource.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestResource = resource;
            }
        }
        if (nearestResource != null)
        {
            if(idleWorkers[0] is Looter looter)
            {
                looter.AssignGatheringTask(nearestResource);
                Debug.Log("[GOAP Tutorial] Looter assigned to gather resources");
            }
            return true;
        }

        return false;
    }
    public override float CalculateUtility(AIWorldState worldState) //STEP 47: In the CalculateUtility() method, add logic to assign a higher utility score when
    // there are more idle workers available and when gold resources are low, to encourage gathering when it's most needed.
    //GO TO GOAPPlanner.cs for STEP 48 to see how this utility score will be used in the decision-making process.
    {
        float utility = 15f;
        
        // Prioritize based on available workers of preferred type
        if (preferredRole == WorkerRole.Looter)
            utility += worldState.aiIdleLooterCount * 10f;
        else if (preferredRole == WorkerRole.Builder)
            utility += worldState.aiIdleBuilderCount * 10f;
        else
            utility += worldState.aiIdleWorkerCount * 10f;

        if(worldState.aiGold < 200)
        {
            utility += 20f;
        }

        return utility;
    }
}

//Action: Train builders to expand your base
public class TrainBuilderAction : TrainWorkerAction
{
    public TrainBuilderAction(UnitData data) : base(data, WorkerRole.Builder) { }
}

//Action: Train looters to quickly gather resources
public class TrainLooterAction : TrainWorkerAction
{
    public TrainLooterAction(UnitData data) : base(data, WorkerRole.Looter) { }
}
