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
    public int aiHeroCount;

    public bool aiHasCommandCenter;
    public int aiIdleWorkerCount;
    public int aiGatheringWorkerCount;
    // idle counts broken out by role
    public int aiIdleBuilderCount;
    public int aiIdleLooterCount;
    public int aiIdleHeroCount;

    public int availableGoldResources;

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

        aiIdleWorkerCount = 0;
        aiGatheringWorkerCount = 0;
        aiBuilderCount = aiLooterCount = aiHeroCount = 0;
        aiIdleBuilderCount = aiIdleLooterCount = aiIdleHeroCount = 0;

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
            if (worker is HeroUnit)
            {
                aiHeroCount++;
                if (worker.currentState == Worker.WorkerState.Idle)
                    aiIdleHeroCount++;
            }
            else if (worker is Looter)
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
}