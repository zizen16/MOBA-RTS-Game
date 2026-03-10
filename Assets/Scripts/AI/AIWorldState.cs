using System.Collections.Generic;
using UnityEngine;
public class AIWorldState : MonoBehaviour // STEP 19: Create the AIWorldState class that will hold all relevant information about the current state of the game from the AI's perspective. 
{
    //STEP 20: Add properties to track the AI's current resources, population, worker count, and other relevant information that actions and goals will use to make decisions.
    public int aiGold;
    public int aiCurrentPopulation;
    public int aiMaxPopulation;
    public int aiWorkerCount;
    // counts of specific worker roles for more nuanced decision making
    public int aiBuilderCount;
    public int aiLooterCount;

    public bool aiHasCommandCenter;
    public int aiIdleWorkerCount;
    public int aiGatheringWorkerCount;
    // idle counts broken out by role
    public int aiIdleBuilderCount;
    public int aiIdleLooterCount;

    public int availableGoldResources;

    // Building counts
    public int aiTowerCount;
    public int aiPylonCount;
    public int aiTrainerCount;
    public int aiBarracksCount;

    // Combat unit counts
    public int aiCombatUnitCount;
    public int aiIdleCombatUnitCount;

    // Enemy detection
    public bool hasNearbyEnemies; // True if player or neutral units are detected near AI combat units
    public int nearbyEnemyCount; // Number of detected nearby enemies
    public float detectionRadius = 40f; // Radius to scan for enemies around AI units

    //STEP 21: Implement a method to update the world state based on the current game conditions. This method will be called by the AIManager each frame or whenever significant changes occur in the game.
    AIManager aiManager;
    void Awake()
    {
        aiManager = GetComponent<AIManager>();
    }
    public void UpdateState() //STEP 25: NEXT Go To GOAPGoal.cs
    {
        UpdateAIState();
        UpdateGameState();
    }
    //STEP 22: Add helper methods to check for specific conditions, such as whether the AI has a certain building, enough resources, or idle workers available. These will be used by actions to check their preconditions.
    void UpdateAIState()
    {
        if (aiManager == null) return;

        aiGold = aiManager.currentGold;
        aiCurrentPopulation = aiManager.currentPopulation;
        aiMaxPopulation = aiManager.maxPopulation;
        aiWorkerCount = aiManager.GetWorkerCount();

        aiHasCommandCenter = HasBuildingOfType<CommandCenter>(); 

        aiTowerCount = 0;
        aiPylonCount = 0;
        aiTrainerCount = 0;
        aiBarracksCount = 0;
        foreach (var building in aiManager.GetAllBuildings())
        {
            if (building.GetComponent<Tower>() != null) aiTowerCount++;
            if (building.GetComponent<Pylon>() != null) aiPylonCount++;
            if (building.GetComponent<Trainer>() != null) aiTrainerCount++;
            if (building.GetComponent<Barracks>() != null) aiBarracksCount++;
        }

        aiIdleWorkerCount = 0;
        aiGatheringWorkerCount = 0;
        aiBuilderCount = aiLooterCount = 0;
        aiIdleBuilderCount = aiIdleLooterCount = 0;

        foreach (var worker in aiManager.GetAllWorkers())
        {
            if (worker == null) continue;

            // track general idle/gathering counts
            switch (worker.currentState)
            {
                case Worker.WorkerState.Idle:
                    aiIdleWorkerCount++;
                    break;
                case Worker.WorkerState.MovingToResource:
                case Worker.WorkerState.Gathering:
                case Worker.WorkerState.ReturningToBase:
                    aiGatheringWorkerCount++;
                    break;
            }

            // count specific roles
            if (worker is Looter)
            {
                aiLooterCount++;
                if (worker.currentState == Worker.WorkerState.Idle)
                    aiIdleLooterCount++;
            }
            else if (worker is Builder)
            {
                aiBuilderCount++;
                if (worker.currentState == Worker.WorkerState.Idle)
                    aiIdleBuilderCount++;
            }
        }

        // Count combat units
        aiCombatUnitCount = aiManager.GetCombatUnitCount();
        aiIdleCombatUnitCount = aiManager.GetIdleCombatUnits().Count;

        // Detect nearby enemies
        DetectNearbyEnemies();
    }
    void UpdateGameState()//STEP 24: Implement the UpdateGameState() method to gather information about the current game state, such as counting available resources on the map, which will be used by actions to check their preconditions and calculate utility.
    {
        GoldResource[] resources = FindObjectsByType<GoldResource>(FindObjectsSortMode.None);
        availableGoldResources = 0;
        foreach (var resource in resources)
        {
            if (resource != null && resource.HasGold())
                availableGoldResources++;
        }
    }
    bool HasBuildingOfType<T>() where T : Component // STEP 23: Implement the HasBuildingOfType<T>() method to check if the AI has a specific type of building, which will be used by actions to check their preconditions.
    {
        foreach (var building in aiManager.GetAllBuildings())
        {
            if (building != null && building.GetComponent<T>() != null)
            {
                return true;
            }
        }
        return false;
    }

    void DetectNearbyEnemies()
    {
        hasNearbyEnemies = false;
        nearbyEnemyCount = 0;

        // Get all AI combat units
        List<CombatUnit> allCombatUnits = aiManager.GetCombatUnits();
        if (allCombatUnits.Count == 0) return;

        // Check for enemies near any AI combat unit
        foreach (var aiUnit in allCombatUnits)
        {
            if (aiUnit == null || !aiUnit.gameObject.activeInHierarchy) continue;

            // Scan for player and neutral units nearby
            Collider[] nearbyObjects = Physics.OverlapSphere(aiUnit.transform.position, detectionRadius, aiUnit.enemyLayer);
            if (nearbyObjects.Length > 0)
            {
                hasNearbyEnemies = true;
                nearbyEnemyCount += nearbyObjects.Length;
            }
        }
    }
}