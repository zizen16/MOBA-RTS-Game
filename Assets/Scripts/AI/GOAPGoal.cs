using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class GOAPGoal //STEP 26: Create the GOAPGoal class that will represent the AI's goals and their priorities.
// Each goal will have a method to calculate its current priority based on the world state and a method to check if the goal is currently satisfied.
{
    public string goalName;
    public float basePriority = 1f;

    public virtual float CalculatePriority(AIWorldState worldState) //STEP 27: Implement the CalculatePriority() method to determine how important this goal is based on the current world state.
    // This will allow the AI to dynamically prioritize different goals as conditions change in the game.
    {
        return basePriority;
    }
    public virtual bool IsSatisfied(AIWorldState worldState) // STEP 28: Implement the IsSatisfied() method to check if the goal's conditions are currently met in the world state.
    // This will be used by the AI to determine if it needs to take actions to achieve this goal or if it can consider it already accomplished.
    {
        return false;
    }

}
//Goal: Build up workers and gather resources.
public class EconomyGoal : GOAPGoal //STEP 29: Create specific goal classes that inherit from GOAPGoal,
// such as EconomyGoal, MilitaryGoal, etc., and implement their CalculatePriority() and IsSatisfied() methods based on the specific conditions relevant to those goals.
{
    public int desiredWorkerCount = 5;
    public int desiredGold = 1000;
    public int desiredPylonCount = 3;
    public int desiredTowerCount = 5;
    public int minTrainersForExpansion = 2;  // Minimum trainers before deprioritizing barracks
    public int minBarracksForExpansion = 1;  // Minimum barracks before focusing on expansion

    public EconomyGoal() // STEP 30: In the constructor, set the goal name and base priority.
    // The base priority can be adjusted based on how important this goal is compared to others.
    {
        goalName = "Economy";
        basePriority = 10f;
    }

    public override float CalculatePriority(AIWorldState worldState) //STEP 31: In the CalculatePriority() method,
    // add logic to increase the priority if the current worker count is below the desired count, or if gold is low.
    // You can also add penalties if certain conditions are met, such as if the population cap is reached.
    {
        float priority = basePriority;

        // CRITICAL: If gold resources are depleted, drastically deprioritize economy gathering
        if (worldState.availableGoldResources <= 0)
        {
            priority -= 50f; // Very low priority when resources depleted
            return priority; // Skip all other bonuses
        }
        else if (worldState.availableGoldResources <= 1)
        {
            priority -= 30f; // Still low priority when running out
        }

        if (worldState.aiWorkerCount < 2)
            priority += 20f;
        else if (worldState.aiWorkerCount < desiredWorkerCount)
            priority += 10f;

        if (worldState.aiGold < 200)
            priority += 15f;
        else if (worldState.aiGold < 500)
            priority += 5f;

        // Building priorities - adjust based on production facility availability
        if (worldState.aiPylonCount < 1)
            priority += 12f; // High priority for first pylon
        else if (worldState.aiPylonCount < desiredPylonCount)
            priority += 8f;

        if (worldState.aiTowerCount < 2)
            priority += 10f; // Need some defense
        else if (worldState.aiTowerCount < desiredTowerCount)
            priority += 6f;

        // Barracks priority - crucial for population capacity, but deprioritize if we have enough
        bool hasEnoughProductionFacilities = worldState.aiTrainerCount >= minTrainersForExpansion && 
                                             worldState.aiBarracksCount >= minBarracksForExpansion;
        
        if (worldState.aiBarracksCount < 1)
            priority += 15f; // Very high priority for first barracks
        else if (worldState.aiBarracksCount < 3 && !hasEnoughProductionFacilities)
            priority += 10f; // High priority for additional barracks only if we don't have enough yet

        //Penalty 
        if (worldState.aiCurrentPopulation >= worldState.aiMaxPopulation) // If population cap is reached, deprioritize economy since we can't train more workers until we build more housing.
            priority -= 5f;
        //Bonus
        if (worldState.aiIdleWorkerCount > 0) // If we have idle workers, increase priority to encourage using them for gathering instead of training more.
            priority += 8f;
        return priority;
    }
    public override bool IsSatisfied(AIWorldState worldState) //STEP 32: In the IsSatisfied() method. NEXT go to GOAPAction.cs to implement actions that will help achieve this goal, such as TrainWorkerAction and GatherResourceAction.
    {
        return worldState.aiWorkerCount >= desiredWorkerCount && worldState.aiGold >= desiredGold &&
               worldState.aiPylonCount >= desiredPylonCount && worldState.aiTowerCount >= desiredTowerCount;
    }
}

//Goal: Expand territory by building pylons and towers
public class ExpansionGoal : GOAPGoal
{
    public int desiredPylonCount = 3;
    public int desiredTowerCount = 10;
    public int minTrainersForExpansion = 2; // Minimum trainers needed to prioritize expansion
    public int minBarracksForExpansion = 1;  // Minimum barracks needed to prioritize expansion

    public ExpansionGoal()
    {
        goalName = "Expansion";
        basePriority = 8f;
    }

    public override float CalculatePriority(AIWorldState worldState)
    {
        float priority = basePriority;

        // Check if we have enough trainers and barracks to prioritize expansion
        bool hasEnoughProductionFacilities = worldState.aiTrainerCount >= minTrainersForExpansion && 
                                             worldState.aiBarracksCount >= minBarracksForExpansion;

        // High priority for initial expansion
        if (worldState.aiPylonCount < 1)
            priority += 15f;
        else if (worldState.aiPylonCount < desiredPylonCount)
            priority += 10f;

        // Towers for defense and control
        if (worldState.aiTowerCount < 3)
            priority += 12f;
        else if (worldState.aiTowerCount < desiredTowerCount)
            priority += 8f;

        // MAJOR PRIORITY BOOST: If we have enough trainers and barracks, prioritize towers and pylons significantly
        if (hasEnoughProductionFacilities)
        {
            if (worldState.aiPylonCount < desiredPylonCount)
                priority += 20f; // Significant boost to build pylons
            if (worldState.aiTowerCount < desiredTowerCount)
                priority += 18f; // Significant boost to build towers
        }

        // Bonus if we have resources and workers
        if (worldState.aiGold > 300 && worldState.aiIdleBuilderCount > 0)
            priority += 5f;

        return priority;
    }

    public override bool IsSatisfied(AIWorldState worldState)
    {
        return worldState.aiPylonCount >= desiredPylonCount && worldState.aiTowerCount >= desiredTowerCount;
    }
}

//Goal: Build military forces for expansion and aggression
public class MilitaryGoal : GOAPGoal
{
    public int desiredCombatUnitCount = 10;
    public int aggressionThreshold = 8; // Start attacking at this unit count
    public int expansionThreshold = 6;   // Start expanding at this unit count

    public MilitaryGoal()
    {
        goalName = "Military";
        basePriority = 7f;
    }

    public override float CalculatePriority(AIWorldState worldState)
    {
        float priority = basePriority;

        // CRITICAL: When nearby enemies detected, attack becomes top priority
        if (worldState.hasNearbyEnemies)
        {
            priority += 50f; // Massive boost - defending/attacking nearby enemies is critical
            priority += worldState.nearbyEnemyCount * 5f; // Extra boost per enemy detected
            Debug.Log($"[GOAP] Nearby enemies detected! Military priority boosted by +50, enemies: {worldState.nearbyEnemyCount}");
        }

        // High priority for initial army
        if (worldState.aiCombatUnitCount < 3)
            priority += 25f;
        else if (worldState.aiCombatUnitCount < desiredCombatUnitCount)
            priority += 15f;

        // Bonus if expansion threshold is reached
        if (worldState.aiCombatUnitCount >= expansionThreshold)
            priority += 10f; // Trigger expansion actions

        // Bonus if aggression threshold reached
        if (worldState.aiCombatUnitCount >= aggressionThreshold)
            priority += 12f; // Prioritize attack actions

        // Lower priority when low on resources
        if (worldState.aiGold < 200)
            priority -= 20f;

        // CRITICAL: When gold resources are depleted, boost military priority significantly
        // AI should shift focus from economy to military aggression
        if (worldState.availableGoldResources <= 0)
        {
            priority += 40f; // Massive boost when economy fails - shift to military
        }
        else if (worldState.availableGoldResources <= 1)
        {
            priority += 20f; // Significant boost when resources running out
        }

        // Increase priority based on idle combat units (ready for action)
        priority += worldState.aiIdleCombatUnitCount * 2f;

        return priority;
    }

    public override bool IsSatisfied(AIWorldState worldState)
    {
        return worldState.aiCombatUnitCount >= desiredCombatUnitCount;
    }
}

