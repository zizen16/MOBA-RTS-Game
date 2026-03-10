using System.Collections.Generic;
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
        if (worldState.aiWorkerCount < 2)
            priority += 20f;
        else if (worldState.aiWorkerCount < desiredWorkerCount)
            priority += 10f;

        if (worldState.aiGold < 200)
            priority += 15f;
        else if (worldState.aiGold < 500)
            priority += 5f;

        // Building priorities
        if (worldState.aiPylonCount < 1)
            priority += 12f; // High priority for first pylon
        else if (worldState.aiPylonCount < desiredPylonCount)
            priority += 8f;

        if (worldState.aiTowerCount < 2)
            priority += 10f; // Need some defense
        else if (worldState.aiTowerCount < desiredTowerCount)
            priority += 6f;

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

    public ExpansionGoal()
    {
        goalName = "Expansion";
        basePriority = 8f;
    }

    public override float CalculatePriority(AIWorldState worldState)
    {
        float priority = basePriority;

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

