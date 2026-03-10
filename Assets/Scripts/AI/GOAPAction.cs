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

public enum WorkerRole { Generic, Builder, Looter }

//Action: Train a worker unit from the Command Center with specific roles.
//Utility: Varies depending on type of worker being requested (builder, looter).
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
        // Stop gathering if resources depleted - fail preconditions
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

//Action: Train melee units for close combat
public class TrainMeleeAction : GOAPAction
{
    UnitData meleeData;

    public TrainMeleeAction(UnitData data)
    {
        meleeData = data;
        actionName = "Train Melee Unit";
        cost = 3f;
        cooldown = 4f;
    }

    public override bool CheckPreconditions(AIWorldState worldState)
    {
        if (!base.CheckPreconditions(worldState)) return false;
        if (meleeData == null || worldState.aiGold < meleeData.goldCost) return false;
        if (worldState.aiCurrentPopulation >= worldState.aiMaxPopulation) return false;
        // Assume requires trainer building (barracks no longer used for training)
        bool hasTrainer = false;
        foreach (var building in AIManager.Instance.GetAllBuildings())
        {
            if (building.GetComponent<Trainer>() != null)
            {
                hasTrainer = true;
                break;
            }
        }
        if (!hasTrainer) return false;
        return true;
    }

    public override bool Execute(AIManager aiManager)
    {
        base.Execute(aiManager);
        // melee units now trained at trainer buildings instead of barracks
        if (meleeData != null && aiManager.TrainUnitFromBuilding<Trainer>(meleeData))
        {
            Debug.Log($"AI trained a melee unit at a Trainer.");
            return true;
        }
        return false;
    }

    public override float CalculateUtility(AIWorldState worldState)
    {
        float utility = 25f; // Base utility for military units

        return utility;
    }
}

//Action: Train range units for ranged attacks
public class TrainRangeAction : GOAPAction
{
    UnitData rangeData;

    public TrainRangeAction(UnitData data)
    {
        rangeData = data;
        actionName = "Train Range Unit";
        cost = 3f;
        cooldown = 4f;
    }

    public override bool CheckPreconditions(AIWorldState worldState)
    {
        if (!base.CheckPreconditions(worldState)) return false;
        if (rangeData == null || worldState.aiGold < rangeData.goldCost) return false;
        if (worldState.aiCurrentPopulation >= worldState.aiMaxPopulation) return false;
        // Assume requires trainer building (barracks no longer used for training)
        bool hasTrainer = false;
        foreach (var building in AIManager.Instance.GetAllBuildings())
        {
            if (building.GetComponent<Trainer>() != null)
            {
                hasTrainer = true;
                break;
            }
        }
        if (!hasTrainer) return false;
        return true;
    }

    public override bool Execute(AIManager aiManager)
    {
        base.Execute(aiManager);
        if (rangeData != null && aiManager.TrainUnitFromBuilding<Trainer>(rangeData))
        {
            Debug.Log($"AI trained a range unit at a Trainer.");
            return true;
        }
        return false;
    }

    public override float CalculateUtility(AIWorldState worldState)
    {
        float utility = 25f; // Base utility for military units

        // trainers available => encourage training
        if (worldState.aiTrainerCount > 0) utility += 30f;

        return utility;
    }
}

//Action: Train tanker units for tanking damage
public class TrainTankerAction : GOAPAction
{
    UnitData tankerData;

    public TrainTankerAction(UnitData data)
    {
        tankerData = data;
        actionName = "Train Tanker Unit";
        cost = 4f;
        cooldown = 5f;
    }

    public override bool CheckPreconditions(AIWorldState worldState)
    {
        if (!base.CheckPreconditions(worldState)) return false;
        if (tankerData == null || worldState.aiGold < tankerData.goldCost) return false;
        if (worldState.aiCurrentPopulation >= worldState.aiMaxPopulation) return false;
        // Assume requires trainer
        bool hasTrainer = false;
        foreach (var building in AIManager.Instance.GetAllBuildings())
        {
            if (building.GetComponent<Trainer>() != null)
            {
                hasTrainer = true;
                break;
            }
        }
        if (!hasTrainer) return false;
        return true;
    }

    public override bool Execute(AIManager aiManager)
    {
        base.Execute(aiManager);
        if (tankerData != null && aiManager.TrainUnitFromBuilding<Trainer>(tankerData))
        {
            Debug.Log($"AI trained a tanker unit at a Trainer.");
            return true;
        }
        return false;
    }

    public override float CalculateUtility(AIWorldState worldState)
    {
        float utility = 20f; // Base utility for advanced units

        // trainers available => encourage training
        if (worldState.aiTrainerCount > 0) utility += 30f;

        return utility;
    }
}

//Action: Train sieger units for siege capabilities
public class TrainSiegerAction : GOAPAction
{
    UnitData siegerData;

    public TrainSiegerAction(UnitData data)
    {
        siegerData = data;
        actionName = "Train Sieger Unit";
        cost = 5f;
        cooldown = 6f;
    }

    public override bool CheckPreconditions(AIWorldState worldState)
    {
        if (!base.CheckPreconditions(worldState)) return false;
        if (siegerData == null || worldState.aiGold < siegerData.goldCost) return false;
        if (worldState.aiCurrentPopulation >= worldState.aiMaxPopulation) return false;
        // Assume requires trainer
        bool hasTrainer = false;
        foreach (var building in AIManager.Instance.GetAllBuildings())
        {
            if (building.GetComponent<Trainer>() != null)
            {
                hasTrainer = true;
                break;
            }
        }
        if (!hasTrainer) return false;
        return true;
    }

    public override bool Execute(AIManager aiManager)
    {
        base.Execute(aiManager);
        if (siegerData != null && aiManager.TrainUnitFromBuilding<Trainer>(siegerData))
        {
            Debug.Log($"AI trained a sieger unit at a Trainer.");
            return true;
        }
        return false;
    }

    public override float CalculateUtility(AIWorldState worldState)
    {
        float utility = 15f; // Base utility for siege units

        // Higher when enemy buildings detected or for late game
        if (worldState.aiTowerCount >= 3) utility += 20f;

        // extra push if trainers exist
        if (worldState.aiTrainerCount > 0) utility += 30f;

        return utility;
    }
}

//Action: Build a tower for defense
public class BuildTowerAction : GOAPAction
{
    BuildingData towerData;

    public BuildTowerAction(BuildingData data)
    {
        towerData = data;
        actionName = "Build Tower";
        cost = 3f;
        cooldown = 5f;
    }

    public override bool CheckPreconditions(AIWorldState worldState)
    {
        if (!base.CheckPreconditions(worldState)) return false;
        if (towerData == null || worldState.aiGold < towerData.goldCost) return false;
        if (worldState.aiIdleBuilderCount <= 0) return false;
        // Check tower limit: max 5 towers per pylon or command center
        if (!CanBuildTower()) return false;
        // Assume no prerequisites for tower
        return true;
    }

    public override bool Execute(AIManager aiManager)
    {
        base.Execute(aiManager);

        // Find the best position: near the anchor (pylon or CC) with the fewest towers
        BaseBuilding bestAnchor = FindBestAnchorForTower();
        Vector3 buildPos;
        if (bestAnchor != null)
        {
            buildPos = bestAnchor.transform.position + new Vector3(Random.Range(-10f, 10f), 0, Random.Range(-10f, 10f));
        }
        else
        {
            // Fallback to near command center
            CommandCenter cc = aiManager.FindCommandCenter();
            buildPos = cc != null ? cc.transform.position + new Vector3(Random.Range(-10f, 10f), 0, Random.Range(-10f, 10f)) : Vector3.zero;
        }

        return aiManager.BuildBuilding(towerData, buildPos);
    }

    public override float CalculateUtility(AIWorldState worldState)
    {
        float utility = 20f; // Base utility for building defense

        // Higher if low on towers
        if (worldState.aiTowerCount < 2) utility += 30f;

        return utility;
    }

    bool CanBuildTower()
    {
        const float radius = 30f; // Radius around pylons and command centers
        var buildings = AIManager.Instance.GetAllBuildings();

        // Get all pylons and command centers
        List<BaseBuilding> anchors = new List<BaseBuilding>();
        foreach (var b in buildings)
        {
            if (b.GetComponent<CommandCenter>() != null || b.GetComponent<Pylon>() != null)
            {
                anchors.Add(b);
            }
        }

        // For each anchor, count towers within radius
        foreach (var anchor in anchors)
        {
            int towerCount = 0;
            foreach (var b in buildings)
            {
                if (b.GetComponent<Tower>() != null)
                {
                    if (Vector3.Distance(anchor.transform.position, b.transform.position) <= radius)
                    {
                        towerCount++;
                    }
                }
            }
            if (towerCount >= 5) return false; // Limit reached for this cluster
        }

        return true; // Can build, as no cluster has 5+ towers
    }

    BaseBuilding FindBestAnchorForTower()
    {
        const float radius = 30f;
        var buildings = AIManager.Instance.GetAllBuildings();

        List<BaseBuilding> anchors = new List<BaseBuilding>();
        foreach (var b in buildings)
        {
            if (b.GetComponent<CommandCenter>() != null || b.GetComponent<Pylon>() != null)
            {
                anchors.Add(b);
            }
        }

        BaseBuilding bestAnchor = null;
        int minTowers = int.MaxValue;

        foreach (var anchor in anchors)
        {
            int towerCount = 0;
            foreach (var b in buildings)
            {
                if (b.GetComponent<Tower>() != null)
                {
                    if (Vector3.Distance(anchor.transform.position, b.transform.position) <= radius)
                    {
                        towerCount++;
                    }
                }
            }
            if (towerCount < minTowers)
            {
                minTowers = towerCount;
                bestAnchor = anchor;
            }
        }

        return bestAnchor;
    }
}

//Action: Build barracks for training combat units
public class BuildBarracksAction : GOAPAction
{
    BuildingData barracksData;

    public BuildBarracksAction(BuildingData data)
    {
        barracksData = data;
        actionName = "Build Barracks";
        cost = 4f;
        cooldown = 8f;
    }

    public override bool CheckPreconditions(AIWorldState worldState)
    {
        if (!base.CheckPreconditions(worldState)) return false;
        if (barracksData == null || worldState.aiGold < barracksData.goldCost) return false;
        if (worldState.aiIdleBuilderCount <= 0) return false;
        // Assume requires command center
        if (!worldState.aiHasCommandCenter) return false;

        // Check barracks limit
        if (worldState.aiBarracksCount >= 5) return false; // Max 5 barracks

        return true;
    }

    public override bool Execute(AIManager aiManager)
    {
        base.Execute(aiManager);

        // Find position near command center
        CommandCenter cc = aiManager.FindCommandCenter();
        Vector3 buildPos = cc != null ? cc.transform.position + new Vector3(Random.Range(-15f, 15f), 0, Random.Range(-15f, 15f)) : Vector3.zero;

        return aiManager.BuildBuilding(barracksData, buildPos);
    }

    public override float CalculateUtility(AIWorldState worldState)
    {
        float utility = 25f; // High utility for military buildings

        // Higher if low on barracks
        if (worldState.aiBarracksCount < 1) utility += 30f; // Need at least one

        // Bonus if near population cap (lacking population capacity)
        if (worldState.aiMaxPopulation > 0 && worldState.aiCurrentPopulation >= worldState.aiMaxPopulation - 2)
        {
            utility += 40f; // High priority when can't train more units due to pop cap
        }

        // Penalty if too many barracks
        if (worldState.aiBarracksCount >= 5)
        {
            utility -= 50f; // Don't build more than 5
        }

        return utility;
    }
}

//Action: Build trainer for advanced unit training
public class BuildTrainerAction : GOAPAction
{
    BuildingData trainerData;

    public BuildTrainerAction(BuildingData data)
    {
        trainerData = data;
        actionName = "Build Trainer";
        cost = 5f;
        cooldown = 10f;
    }

    public override bool CheckPreconditions(AIWorldState worldState)
    {
        if (!base.CheckPreconditions(worldState)) return false;
        if (trainerData == null || worldState.aiGold < trainerData.goldCost) return false;
        if (worldState.aiIdleBuilderCount <= 0) return false;
        // Assume requires command center
        if (!worldState.aiHasCommandCenter) return false;

        // limit trainer buildings to max 2
        const int maxTrainers = 2;
        if (worldState.aiTrainerCount >= maxTrainers) return false;
        return true;
    }

    public override bool Execute(AIManager aiManager)
    {
        base.Execute(aiManager);

        // Find position near command center
        CommandCenter cc = aiManager.FindCommandCenter();
        Vector3 buildPos = cc != null ? cc.transform.position + new Vector3(Random.Range(-20f, 20f), 0, Random.Range(-20f, 20f)) : Vector3.zero;

        return aiManager.BuildBuilding(trainerData, buildPos);
    }

    public override float CalculateUtility(AIWorldState worldState)
    {
        float utility = 20f; // Utility for advanced training

        // Higher if low on trainers
        int trainerCount = 0;
        int barracksCount = 0;
        foreach (var building in AIManager.Instance.GetAllBuildings())
        {
            if (building.GetComponent<Trainer>() != null) trainerCount++;
            if (building.GetComponent<Barracks>() != null) barracksCount++;
        }
        if (trainerCount < 1) utility += 25f;

        // Give extra bonus if at least one barracks exists (trainers should follow barracks)
        if (barracksCount > 0)
            utility += 30f;

        // if we already hit the limit, heavy penalty
        const int maxTrainers = 2;
        if (trainerCount >= maxTrainers)
            utility -= 50f;

        return utility;
    }
}

//Action: Build a pylon for vision/power
public class BuildPylonAction : GOAPAction
{
    BuildingData pylonData;

    public BuildPylonAction(BuildingData data)
    {
        pylonData = data;
        actionName = "Build Pylon";
        cost = 2f;
        cooldown = 4f;
    }

    public override bool CheckPreconditions(AIWorldState worldState)
    {
        if (!base.CheckPreconditions(worldState)) return false;
        if (pylonData == null || worldState.aiGold < pylonData.goldCost) return false;
        if (worldState.aiIdleBuilderCount <= 0) return false;
        // Assume requires command center
        if (!worldState.aiHasCommandCenter) return false;
        return true;
    }

    public override bool Execute(AIManager aiManager)
    {
        base.Execute(aiManager);

        // Find position near command center
        CommandCenter cc = aiManager.FindCommandCenter();
        Vector3 buildPos = cc != null ? cc.transform.position + new Vector3(Random.Range(-15f, 15f), 0, Random.Range(-15f, 15f)) : Vector3.zero;

        return aiManager.BuildBuilding(pylonData, buildPos);
    }

    public override float CalculateUtility(AIWorldState worldState)
    {
        float utility = 15f; // Base utility for building pylon

        // Higher if low on pylons
        if (worldState.aiPylonCount < 1) utility += 25f;

        return utility;
    }
}

//Action: Builder explores the map and builds pylons in suitable locations
public class BuilderExploreAndBuildPylonAction : GOAPAction
{
    BuildingData pylonData;

    public BuilderExploreAndBuildPylonAction(BuildingData data)
    {
        pylonData = data;
        actionName = "Builder Explore and Build Pylon";
        cost = 3f;
        cooldown = 10f;
    }

    public override bool CheckPreconditions(AIWorldState worldState)
    {
        if (!base.CheckPreconditions(worldState)) return false;
        if (pylonData == null || worldState.aiGold < pylonData.goldCost) return false;
        if (worldState.aiIdleBuilderCount <= 0) return false;
        if (!worldState.aiHasCommandCenter) return false;
        return true;
    }

    public override bool Execute(AIManager aiManager)
    {
        base.Execute(aiManager);
        isRunning = true;

        List<Worker> idleBuilders = aiManager.GetIdleBuilders();
        if (idleBuilders.Count == 0) return false;

        Builder builder = idleBuilders[0] as Builder;
        if (builder == null) return false;

        // Choose a random exploration position
        CommandCenter cc = aiManager.FindCommandCenter();
        Vector3 explorePos;
        if (cc != null)
        {
            // Random position 50-100 units away
            Vector2 randomDir = Random.insideUnitCircle.normalized * Random.Range(50f, 100f);
            explorePos = cc.transform.position + new Vector3(randomDir.x, 0, randomDir.y);
        }
        else
        {
            explorePos = builder.transform.position + new Vector3(Random.Range(-50f, 50f), 0, Random.Range(-50f, 50f));
        }

        builder.explorationBuildingData = pylonData;
        builder.isExploring = true;
        builder.MoveTo(explorePos);

        return true;
    }

    public override bool IsComplete()
    {
        // Check if builder is still exploring or building
        foreach (var w in AIManager.Instance.GetAllWorkers())
        {
            if (w is Builder builder)
            {
                if (builder.isExploring || builder.currentState == Worker.WorkerState.Building || builder.currentState == Worker.WorkerState.MovingToBuild)
                    return false;
            }
        }
        isRunning = false;
        return true;
    }

    public override float CalculateUtility(AIWorldState worldState)
    {
        float utility = 10f; // Base utility for exploration

        // Higher if few pylons and idle builder
        if (worldState.aiPylonCount < 2 && worldState.aiIdleBuilderCount > 0) utility += 20f;

        return utility;
    }
}

//Action: Send combat units to roam the map for early aggression
public class RoamWithCombatUnitsAction : GOAPAction
{
    public RoamWithCombatUnitsAction()
    {
        actionName = "Roam with Combat Units";
        cost = 1f;
        cooldown = 5f;
    }

    public override bool CheckPreconditions(AIWorldState worldState)
    {
        if (!base.CheckPreconditions(worldState)) return false;
        if (worldState.aiIdleCombatUnitCount <= 0) return false;
        return true;
    }

    public override bool Execute(AIManager aiManager)
    {
        base.Execute(aiManager);
        
        List<CombatUnit> idleCombats = aiManager.GetIdleCombatUnits();
        if (idleCombats.Count == 0) return false;

        // Send half of idle combat units to roam in different directions
        int unitsToSend = Mathf.Max(1, idleCombats.Count / 2);
        for (int i = 0; i < unitsToSend && i < idleCombats.Count; i++)
        {
            CombatUnit unit = idleCombats[i];
            if (unit == null) continue;

            // Random roaming position far from base
            CommandCenter cc = aiManager.FindCommandCenter();
            Vector3 roamPos;
            if (cc != null)
            {
                Vector2 randomDir = Random.insideUnitCircle.normalized * Random.Range(70f, 120f);
                roamPos = cc.transform.position + new Vector3(randomDir.x, 0, randomDir.y);
            }
            else
            {
                roamPos = unit.transform.position + new Vector3(Random.Range(-80f, 80f), 0, Random.Range(-80f, 80f));
            }

            // Use AttackMove for roaming combat
            unit.AttackMove(roamPos);
            Debug.Log($"[GOAP] Combat unit roaming to {roamPos}");
        }

        return true;
    }

    public override float CalculateUtility(AIWorldState worldState)
    {
        float utility = 5f; // Low baseline utility

        // CRITICAL: If nearby enemies detected, maximize roaming priority
        if (worldState.hasNearbyEnemies)
            utility += 40f; // Massive boost to engage nearby enemies

        // Higher if we have idle combat units
        utility += worldState.aiIdleCombatUnitCount * 3f;

        // Encourage roaming when we have enough units
        if (worldState.aiCombatUnitCount >= 5) utility += 20f;

        return utility;
    }
}

//Action: Send combat units to defend the AI base
public class DefendBaseAction : GOAPAction
{
    public DefendBaseAction()
    {
        actionName = "Defend Base";
        cost = 2f;
        cooldown = 8f;
    }

    public override bool CheckPreconditions(AIWorldState worldState)
    {
        if (!base.CheckPreconditions(worldState)) return false;
        if (worldState.aiIdleCombatUnitCount <= 0) return false;
        if (!worldState.aiHasCommandCenter) return false;
        return true;
    }

    public override bool Execute(AIManager aiManager)
    {
        base.Execute(aiManager);
        
        List<CombatUnit> idleCombats = aiManager.GetIdleCombatUnits();
        if (idleCombats.Count == 0) return false;

        CommandCenter cc = aiManager.FindCommandCenter();
        if (cc == null) return false;

        // Send combat units to patrol around base
        int unitsToSend = Mathf.Max(1, idleCombats.Count / 3); // Send 1/3 of idle units
        for (int i = 0; i < unitsToSend && i < idleCombats.Count; i++)
        {
            CombatUnit unit = idleCombats[i];
            if (unit == null) continue;

            // Position near base for defense
            Vector3 patrolPos = cc.transform.position + new Vector3(Random.Range(-20f, 20f), 0, Random.Range(-20f, 20f));
            unit.AttackMove(patrolPos);
            Debug.Log($"[GOAP] Combat unit defending base at {patrolPos}");
        }

        return true;
    }

    public override float CalculateUtility(AIWorldState worldState)
    {
        float utility = 15f; // Baseline defensive utility

        // CRITICAL: If nearby enemies detected, maximum defense priority
        if (worldState.hasNearbyEnemies)
            utility += 50f; // Massive boost to defend base against nearby enemies

        // Higher if we have excess idle combat units
        if (worldState.aiIdleCombatUnitCount >= 3) utility += 10f;

        // Lower priority when economy is struggling
        if (worldState.aiGold < 200) utility -= 20f;

        return utility;
    }
}

//Action: Attack the player base with combat units
public class AttackPlayerBaseAction : GOAPAction
{
    public AttackPlayerBaseAction()
    {
        actionName = "Attack Player Base";
        cost = 3f;
        cooldown = 10f;
    }

    public override bool CheckPreconditions(AIWorldState worldState)
    {
        if (!base.CheckPreconditions(worldState)) return false;
        // Only attack if we have significant military force
        if (worldState.aiCombatUnitCount < 8) return false;
        // And some idle units to send
        if (worldState.aiIdleCombatUnitCount <= 0) return false;
        return true;
    }

    public override bool Execute(AIManager aiManager)
    {
        base.Execute(aiManager);
        
        // Get the player's command center
        CommandCenter playerCC = aiManager.FindPlayerCommandCenter();
        if (playerCC == null) return false;

        List<CombatUnit> idleCombats = aiManager.GetIdleCombatUnits();
        if (idleCombats.Count == 0) return false;

        // Send most combat units to attack player base
        int unitsToSend = Mathf.Max(2, (idleCombats.Count * 2) / 3); // Send 2/3 of units
        for (int i = 0; i < unitsToSend && i < idleCombats.Count; i++)
        {
            CombatUnit unit = idleCombats[i];
            if (unit == null) continue;

            // Attack move toward player base
            Vector3 attackPos = playerCC.transform.position;
            unit.AttackMove(attackPos);
            Debug.Log($"[GOAP] Combat unit attacking player base at {attackPos}");
        }

        return true;
    }

    public override float CalculateUtility(AIWorldState worldState)
    {
        float utility = 10f; // Base offensive utility

        // CRITICAL: If nearby enemies detected, prioritize attack
        if (worldState.hasNearbyEnemies)
            utility += 45f; // Massive boost to engage nearby enemies

        // Higher with more combat units
        if (worldState.aiCombatUnitCount >= 10) utility += 15f;
        if (worldState.aiCombatUnitCount >= 15) utility += 25f;

        // Lower priority when economy needs attention
        if (worldState.aiGold < 300) utility -= 30f;

        // Encourage aggression with excess idle units
        utility += worldState.aiIdleCombatUnitCount * 2f;

        return utility;
    }
}

//Action: Expand the base when enough units are trained
public class ExpandBaseAction : GOAPAction
{
    BuildingData pylonData;
    BuildingData towerData;

    public ExpandBaseAction(BuildingData pylon, BuildingData tower)
    {
        pylonData = pylon;
        towerData = tower;
        actionName = "Expand Base";
        cost = 4f;
        cooldown = 12f;
    }

    public override bool CheckPreconditions(AIWorldState worldState)
    {
        if (!base.CheckPreconditions(worldState)) return false;
        // Only expand if we have significant military force
        if (worldState.aiCombatUnitCount < 6) return false;
        // And we have builders available
        if (worldState.aiIdleBuilderCount <= 0) return false;
        // And resources
        if (pylonData != null && worldState.aiGold < pylonData.goldCost) return false;
        if (!worldState.aiHasCommandCenter) return false;
        return true;
    }

    public override bool Execute(AIManager aiManager)
    {
        base.Execute(aiManager);
        isRunning = true;

        // Send builders to expand using pylon and tower
        List<Worker> idleBuilders = aiManager.GetIdleBuilders();
        if (idleBuilders.Count == 0) return false;

        Builder builder = idleBuilders[0] as Builder;
        if (builder == null) return false;

        // Choose an expansion position far from base
        CommandCenter cc = aiManager.FindCommandCenter();
        Vector3 expandPos;
        if (cc != null)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized * Random.Range(60f, 100f);
            expandPos = cc.transform.position + new Vector3(randomDir.x, 0, randomDir.y);
        }
        else
        {
            expandPos = builder.transform.position + new Vector3(Random.Range(-50f, 50f), 0, Random.Range(-50f, 50f));
        }

        // Set builder to explore and build pylon
        builder.explorationBuildingData = pylonData;
        builder.isExploring = true;
        builder.MoveTo(expandPos);

        Debug.Log($"[GOAP] Expanding base to {expandPos} with builder");
        return true;
    }

    public override bool IsComplete()
    {
        // Action complete when builder finishes
        foreach (var w in AIManager.Instance.GetAllWorkers())
        {
            if (w is Builder builder)
            {
                if (builder.isExploring || builder.currentState == Worker.WorkerState.Building || builder.currentState == Worker.WorkerState.MovingToBuild)
                    return false;
            }
        }
        isRunning = false;
        return true;
    }

    public override float CalculateUtility(AIWorldState worldState)
    {
        float utility = 12f; // Base expansion utility

        // Higher priority when we have lots of combat units
        if (worldState.aiCombatUnitCount >= 8) utility += 20f;
        if (worldState.aiCombatUnitCount >= 12) utility += 30f;

        // Higher if we have idle builders and low pylon count
        if (worldState.aiPylonCount < 3 && worldState.aiIdleBuilderCount > 0) utility += 15f;

        // Lower priority if low on resources
        if (worldState.aiGold < 250) utility -= 25f;

        return utility;
    }
}
