// ============================================================================
// AIManager.cs
// ============================================================================
// Central AI Controller for the GOAP Tutorial.
// Same structure as original AIManager but with only relevant methods.
// ============================================================================

using UnityEngine;
using System.Collections.Generic;
using System.Collections;


public class AIManager : MonoBehaviour//STEP 1: Create the AIManager class that will control the AI's behavior and decision-making.
{
    //STEP 2: Implement a Singleton pattern for easy access across the project.
    // ====================================================================
    // Singleton Pattern 
    // ====================================================================
    public static AIManager Instance;

    //STEP 3: Define resource and state variables to track the AI's current situation.
    // ====================================================================
    // Resource and State Variables 
    // ====================================================================
    [Header("Starting Resources")]
    public int startingGold = 1000;

    [Header("Current State")]
    public int currentGold = 0;
    public int currentPopulation = 0;
    public int maxPopulation = 0;

    [Header("AI Configuration")]
    public float decisionInterval = 2f;
    public bool enableAI = true;

    [Header("Initial AI Buildings")]
    public GameObject[] startingBuildingObjects;


    // ====================================================================
    // STEP 4: Create lists to track the AI's buildings, units, and workers.
    // ====================================================================
    public List<BuildingData> completedBuildings = new List<BuildingData>();
    List<BaseBuilding> aiBuildings = new List<BaseBuilding>();
    List<BaseUnit> aiUnits = new List<BaseUnit>();
    List<Worker> aiWorkers = new List<Worker>();
    List<UnitSpawner> aiSpawners = new List<UnitSpawner>();



    // ====================================================================
    // Component References
    // ====================================================================
    GOAPPlanner planner; // STEP 58: Add a reference to the GOAPPlanner component, which will be responsible for evaluating available actions and selecting the best one based on the current world state and defined goals.
    AIWorldState worldState; // STEP 59: Add a reference to the AIWorldState component, which will hold all relevant information about the current state of the game from the AI's perspective, and will be updated regularly to reflect changes in the game world.

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        currentGold = startingGold;

        planner = GetComponent<GOAPPlanner>();
        if (planner == null) planner = gameObject.AddComponent<GOAPPlanner>(); //STEP 60: Ensure the GOAPPlanner component is attached to the same GameObject as the AIManager, and if not, add it automatically.

        worldState = GetComponent<AIWorldState>();
        if (worldState == null) worldState = gameObject.AddComponent<AIWorldState>(); //STEP 61: Ensure the AIWorldState component is attached to the same GameObject as the AIManager, and if not, add it automatically.
    }

    void Start()
    {
        DiscoverStartingBuildings(); //STEP 57: Call the DiscoverStartingBuildings() method at the start of the game to ensure the AI's initial buildings are registered and accounted for in the world state from the very beginning.

        if (enableAI)
        {
            InvokeRepeating(nameof(MakeDecision), 1f, decisionInterval); //STEP 55: Set up a repeating timer to call the MakeDecision() method at regular intervals, allowing the AI to continuously evaluate its situation and make decisions.
        }
    }

    // ====================================================================
    // DiscoverStartingBuildings 
    // ====================================================================
    void DiscoverStartingBuildings() // STEP 56: Implement the DiscoverStartingBuildings() method to find and register the AI's initial buildings at the start of the game,
    // and to scan for any additional buildings in the vicinity of the AI's spawn area to ensure all relevant buildings are tracked from the beginning.
    {
        if (startingBuildingObjects != null)
        {
            foreach (var buildingObj in startingBuildingObjects)
            {
                if (buildingObj != null)
                {
                    BaseBuilding building = buildingObj.GetComponent<BaseBuilding>();
                    if (building != null)
                    {
                        building.isAIOwned = true;
                        RegisterAIBuilding(building);

                        if (building.buildingData != null && !completedBuildings.Contains(building.buildingData))
                        {
                            completedBuildings.Add(building.buildingData);
                            AddPopulationFromBuildings(building.buildingData.populationProvided);
                        }

                        Debug.Log("[GOAP Tutorial] Registered starting building: " + building.name);
                    }
                }
            }
        }
    }

    // ====================================================================
    // MakeDecision - The AI Decision Loop 
    // ====================================================================
    void MakeDecision() //STEP 54: Implement the MakeDecision() method to update the world state, use the GOAPPlanner to select the best action based on the current world state, and execute that action.
    {
        if (!enableAI) return;

        worldState.UpdateState();

        GOAPAction nextAction = planner.GetBestAction(worldState);

        if (nextAction != null)
        {
            Debug.Log("[GOAP Tutorial] Executing action: " + nextAction.actionName);
            nextAction.Execute(this);
        }
    }

    // ====================================================================
    // Resource Methods 
    // ====================================================================

    public bool HasEnoughGold(int amount) //STEP 5: chech has enough gold, spend gold, add gold (if needed).
    {
        return currentGold >= amount;
    }

    public bool SpendGold(int amount)//STEP 6: SpendGold (if needed).
    {
        if (HasEnoughGold(amount))
        {
            currentGold -= amount;
            return true;
        }
        return false;
    }

    public void AddGold(int amount) //STEP 6: AddGold (if needed).
    {
        currentGold += amount;
    }

    public bool HasPopulationForUnit(int populationCost)//STEP 7: Check if the AI has enough population capacity to train a unit.
    {
        return (currentPopulation + populationCost) <= maxPopulation;
    }

    // ====================================================================
    // Population Management Methods 
    // ====================================================================

    public void AddPopulationFromBuildings(int amount) //STEP 8: Add population from buildings, register/unregister units to track population usage.
    {
        maxPopulation += amount;
    }

    public void RegisterUnit(UnitData unitData)//STEP 9: RegisterUnit and UnregisterUnit to track population usage when training/destroying units.
    {
        currentPopulation += unitData.populationCost;
    }

    public void RegisterAIBuilding(BaseBuilding building)//STEP 10: RegisterAIBuilding to track the AI's buildings and spawners.
    {
        if (!aiBuildings.Contains(building))
        {
            aiBuildings.Add(building);

            UnitSpawner spawner = building.GetComponent<UnitSpawner>();
            if (spawner != null && !aiSpawners.Contains(spawner))
            {
                aiSpawners.Add(spawner);
            }
        }
    }

    public void UnregisterAIBuilding(BaseBuilding building)
    {
        if (aiBuildings.Contains(building))
        {
            aiBuildings.Remove(building);

            UnitSpawner spawner = building.GetComponent<UnitSpawner>();
            if (spawner != null && aiSpawners.Contains(spawner))
            {
                aiSpawners.Remove(spawner);
            }
        }
    }

    public List<BaseBuilding> GetAllBuildings() => new List<BaseBuilding>(aiBuildings);//STEP 11: GetAllBuildings to return a list of the AI's current buildings.

    // ====================================================================
    // Unit Management Methods 
    // ====================================================================
    public void RegisterAIUnit(BaseUnit unit)
    {
        if (!aiUnits.Contains(unit))
        {
            aiUnits.Add(unit);

            if (unit is Worker worker)
            {
                aiWorkers.Add(worker);
            }
        }
    }
    public void UnregisterAIUnit(UnitData unitData)
    {
        BaseUnit unitToRemove = null;
        foreach (var unit in aiUnits)
        {
            if (unit != null && unit.unitData == unitData)
            {
                unitToRemove = unit;
                break;
            }
        }

        if (unitToRemove != null)
        {
            aiUnits.Remove(unitToRemove);

            if (unitToRemove is Worker worker)
            {
                aiWorkers.Remove(worker);
            }
        }
    }


    public List<Worker> GetIdleWorkers() //STEP 12: GetIdleWorkers to return a list of the AI's currently idle workers.
    {
        List<Worker> idle = new List<Worker>();
        foreach (var worker in aiWorkers)
        {
            if (worker != null && worker.currentState == Worker.WorkerState.Idle)
            {
                idle.Add(worker);
            }
        }
        return idle;
    }


    public List<Worker> GetAllWorkers() => new List<Worker>(aiWorkers);//STEP 13: GetAllWorkers to return a list of all the AI's workers for state tracking in the world state.

    public int GetWorkerCount() => aiWorkers.Count; //STEP 14: GetWorkerCount to return the total number of workers the AI currently has.

    // new helpers to categorize workers by role
    public int GetBuilderCount()
    {
        int count = 0;
        foreach (var w in aiWorkers)
            if (w is Builder && !(w is HeroUnit))
                count++;
        return count;
    }
    public int GetLooterCount()
    {
        int count = 0;
        foreach (var w in aiWorkers)
            if (w is Looter)
                count++;
        return count;
    }
    public int GetHeroCount()
    {
        int count = 0;
        foreach (var w in aiWorkers)
            if (w is HeroUnit)
                count++;
        return count;
    }

    public List<Worker> GetIdleBuilders()
    {
        List<Worker> idle = new List<Worker>();
        foreach (var w in aiWorkers)
            if ((w is Builder && !(w is HeroUnit)) && w.currentState == Worker.WorkerState.Idle)
                idle.Add(w);
        return idle;
    }
    public List<Worker> GetIdleLooters()
    {
        List<Worker> idle = new List<Worker>();
        foreach (var w in aiWorkers)
            if (w is Looter && w.currentState == Worker.WorkerState.Idle)
                idle.Add(w);
        return idle;
    }
    public List<Worker> GetIdleHeroes()
    {
        List<Worker> idle = new List<Worker>();
        foreach (var w in aiWorkers)
            if (w is HeroUnit && w.currentState == Worker.WorkerState.Idle)
                idle.Add(w);
        return idle;
    }

    // ====================================================================
    // Helper Methods 
    // ====================================================================

    public CommandCenter FindCommandCenter()//STEP 15: FindCommandCenter to locate the AI's Command Center for returning resources.
    {
        foreach (var building in aiBuildings)
        {
            CommandCenter cc = building.GetComponent<CommandCenter>();
            if (cc != null) return cc;
        }
        return null;
    }

    // ====================================================================
    // Unit Training Methods 
    // ====================================================================


    public bool TrainUnitByData(UnitData unitData)//STEP 16: TrainUnitByData method that searches through the AI's spawners to find one that can train the specified unit data, then calls TrainUnit to start training.
    {
        if (unitData == null) return false;

        foreach (var spawner in aiSpawners)
        {
            if (spawner == null) continue;

            for (int i = 0; i < spawner.spawnableUnits.Length; i++)
            {
                if (spawner.spawnableUnits[i] == unitData)
                {
                    return TrainUnit(spawner, i);
                }
            }
        }

        Debug.LogWarning("[GOAP Tutorial] No spawner found that can train " + unitData.unitName);
        return false;
    }
    public bool TrainUnit(UnitSpawner spawner, int unitIndex)//STEP 17: TrainUnit method that takes a spawner and unit index to train a unit, checking resources and population before starting the training process.

    {
        if (spawner == null || spawner.spawnableUnits == null) return false;
        if (unitIndex < 0 || unitIndex >= spawner.spawnableUnits.Length) return false;

        UnitData data = spawner.spawnableUnits[unitIndex];
        if (data == null) return false;

        if (!HasEnoughGold(data.goldCost))
        {
            Debug.LogWarning("[GOAP Tutorial] Not enough gold to train " + data.unitName);
            return false;
        }

        if (!HasPopulationForUnit(data.populationCost))
        {
            Debug.LogWarning("[GOAP Tutorial] Not enough population capacity to train " + data.unitName);
            return false;
        }

        SpendGold(data.goldCost);
        RegisterUnit(data);

        StartCoroutine(TrainUnitCoroutine(spawner, data));
        return true;
    }

    IEnumerator TrainUnitCoroutine(UnitSpawner spawner, UnitData data)//STEP 18: TrainUnitCoroutine to handle the actual training time delay and unit instantiation after training is complete. NEXT GO TO AIWORLDSTATE FOR STEP 19
    {
        Debug.Log("[GOAP Tutorial] Started training " + data.unitName + " (Time: " + data.trainTime + "s)");

        yield return new WaitForSeconds(data.trainTime);

        if (spawner != null && spawner.spawnPoint != null)
        {
            Vector3 spawnPos = spawner.spawnPoint.position;
            Quaternion spawnRot = spawner.spawnPoint.rotation;
            GameObject unitObj = Instantiate(data.unitPrefab, spawnPos, spawnRot);

            BaseUnit unit = unitObj.GetComponent<BaseUnit>();
            if (unit != null)
            {
                unit.isEnemyUnit = true;
                RegisterAIUnit(unit);
                Debug.Log("[GOAP Tutorial] Finished training " + data.unitName);
            }
        }
    }

}
