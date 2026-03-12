using UnityEngine;
using System.Collections.Generic;

public class GOAPPlanner : MonoBehaviour // STEP 48: Create the GOAPPlanner class that will be responsible for evaluating available actions and selecting the best one based on the current world state and defined goals.
{
    [Header("Unit Data References")]
    public UnitData builderUnitData;
    public UnitData looterUnitData;
    public UnitData meleeUnitData;
    public UnitData rangeUnitData;
    public UnitData tankerUnitData;
    public UnitData siegerUnitData;
    public UnitData heroUnitData;

    [Header("Building Data References")]
    public BuildingData towerData;
    public BuildingData pylonData;
    public BuildingData barracksData;
    public BuildingData trainerData;
    
    List<GOAPGoal> goals = new List<GOAPGoal>();
    List<GOAPAction> availableActions = new List<GOAPAction>();

    void Start()
    {
        InitializeGoals();
        InitializeActions();
    }
    void InitializeGoals() //STEP 49: In the InitializeGoals() method, add instances of the specific goals you want your AI to pursue, such as EconomyGoal, MilitaryGoal, etc.
     // Each goal will have its own logic for calculating priority and checking if it's satisfied, which will influence the AI's decision-making process.
    {
        goals.Clear();
        // Add goals in order of importance. Economy is the highest priority for this simple AI.
        goals.Add(new EconomyGoal());
        goals.Add(new MilitaryGoal());
        goals.Add(new ExpansionGoal());
    }
    void InitializeActions()//STEP 50: In the InitializeActions() method, add instances of the specific actions that the AI can take, such as TrainBuilderAction, TrainLooterAction, and GatherResourceAction.
     // These actions will have their own preconditions, execution logic, and utility calculations that will be evaluated when the AI is deciding what to do.
    {
        availableActions.Clear();
        // Add available actions for role-specific workers
        if (builderUnitData != null)
            availableActions.Add(new TrainBuilderAction(builderUnitData));
        if (looterUnitData != null)
            availableActions.Add(new TrainLooterAction(looterUnitData));

        // Train combat units
        if (meleeUnitData != null)
            availableActions.Add(new TrainMeleeAction(meleeUnitData));
        if (rangeUnitData != null)
            availableActions.Add(new TrainRangeAction(rangeUnitData));
        if (tankerUnitData != null)
            availableActions.Add(new TrainTankerAction(tankerUnitData));
        if (siegerUnitData != null)
            availableActions.Add(new TrainSiegerAction(siegerUnitData));
        
        // Gather resources - prefer using looters
        availableActions.Add(new GatherResourceAction(WorkerRole.Looter));

        // Build structures
        if (towerData != null)
            availableActions.Add(new BuildTowerAction(towerData));
        if (pylonData != null)
        {
            availableActions.Add(new BuildPylonAction(pylonData));
            availableActions.Add(new BuilderExploreAndBuildPylonAction(pylonData));
        }
        if (barracksData != null)
            availableActions.Add(new BuildBarracksAction(barracksData));
        if (trainerData != null)
            availableActions.Add(new BuildTrainerAction(trainerData));

        // Combat unit actions
        availableActions.Add(new RoamWithCombatUnitsAction());
        availableActions.Add(new DefendBaseAction());
        availableActions.Add(new AttackPlayerBaseAction());
        
        // Expansion action
        if (pylonData != null && towerData != null)
            availableActions.Add(new ExpandBaseAction(pylonData, towerData));

        // Hero unit actions
        availableActions.Add(new HeroUseBarrageSkillAction());
        availableActions.Add(new HeroUseHealingSkillAction());
        availableActions.Add(new HeroUseDamageBuffSkillAction());
        availableActions.Add(new HeroUseSpeedBoostSkillAction());
        availableActions.Add(new HeroAttackPlayerBaseAction());
        
    }
    public GOAPAction GetBestAction(AIWorldState currentState)//STEP 51: Implement the GetBestAction() method to evaluate all available actions against the current world state and return the one with the highest utility score that also meets its preconditions.
     // This method will be called by the AIManager when it's time for the AI to make a decision about what action to take next.
     // The utility score will be influenced by both the action's inherent utility and how well it helps achieve the current highest-priority goal.
     // The method should also check if any actions are currently running and not complete, in which case it should return null to allow those actions to finish before starting new ones.
    {
        foreach (var action in availableActions)
        {
            if (action.isRunning && !action.IsComplete())// If an action is currently running and not complete, we should wait for it to finish before starting a new one.
            {
                return null;
            }
        }
        GOAPAction bestAction = null;
        float bestScore = float.MinValue;
        // Evaluate each action
        foreach (var action in availableActions)
        {
            if (!action.CheckPreconditions(currentState))
            {
                Debug.Log("Action " + action.actionName + " failed preconditions.");
                continue;// If an action doesn't meet its preconditions, we can't consider it for execution, so we skip it and move on to the next LOOP.
            }

            float utility = action.CalculateUtility(currentState);
            float goalBonus = CalculateGoalAlignment(action, currentState); // STEP 52: Call a method to calculate how well this action aligns with the current highest-priority goal.
            float totalScore = utility + goalBonus;
            // Track highest scoring action
            if (totalScore > bestScore)
            {
                bestScore = totalScore;
                bestAction = action;
            }
        }
        if (bestAction != null)
            Debug.Log("Best action selected: " + bestAction.actionName + " with score: " + bestScore);
        else
            Debug.Log("No valid actions found.");
        return bestAction; //STEP 53: Go to AIManager.cs to see how the selected action is executed and how the GOAPPlanner is integrated into the overall AI decision-making process.
    }

    float CalculateGoalAlignment(GOAPAction action, AIWorldState currentState) //STEP 52: Implement the CalculateGoalAlignment() method to determine how well a given action helps achieve the current highest-priority goal. 
     // This can be done by checking which goal is currently the most important (highest priority) and then giving a bonus to actions that directly contribute to achieving that goal.
     // For example, if the highest-priority goal is EconomyGoal, then actions like GatherResourceAction and TrainWorkerAction would receive a higher bonus, while actions unrelated to economy would receive little to no bonus.
     // This encourages the AI to choose actions that are not only good on their own but also strategically aligned with its current objectives.
    {
        float bonus = 0f;
        // Find the highest priority goal that is NOT satisfied yet
        GOAPGoal highestPriorityGoal = null;
        float highestPriority = float.MinValue;
        foreach (var goal in goals)
        {
            // Skip goals that are already satisfied
            if (goal.IsSatisfied(currentState)) continue;

            float priority = goal.CalculatePriority(currentState); // Get the priority of this goal based on the current world state
             // We want to find the highest priority goal that is not yet satisfied, as that will be the one we want to align our actions with.
             // This allows the AI to dynamically shift its focus as conditions change in the game, always striving to achieve the most important unmet goal.
             // For example, if the EconomyGoal becomes unsatisfied due to low resources, it will become the highest priority and actions that help achieve it will get a bonus.
             // If later on the MilitaryGoal becomes more important due to an enemy attack, then actions that help achieve that goal will get a bonus instead.
             // This creates a dynamic and responsive AI that can adapt its behavior based on the evolving game state and its own objectives.
             // It also allows for emergent behavior, where the AI might choose unexpected but effective actions based on how they align with its goals in a given situation.
             // Overall, this method is crucial for ensuring that the AI's decision-making process is not just about individual action utility, but also about strategic alignment with its current priorities.
             // This is what makes GOAP a powerful tool for creating intelligent and adaptable AI in games.
             // By carefully designing your goals and actions, you can create an AI that behaves in a believable and effective way, providing a challenging and engaging experience for players.
             // Remember to test and tweak your goals, actions, and utility calculations to achieve the desired behavior from your AI. It's often an iterative process to get everything working just right.

            if (priority > highestPriority) // We want to find the goal with the highest priority that is not yet satisfied, as that will be the one we want to align our actions with.
            {
                highestPriority = priority;
                highestPriorityGoal = goal;
            }
        }

        if (highestPriorityGoal == null) return bonus;
        // Give bonus based on which goal is active and which action helps it
        if (highestPriorityGoal is EconomyGoal)
        {
            // Gathering directly increases gold income - prioritize looters
            if (action is GatherResourceAction)
                bonus += highestPriority * 0.5f;   // 50% of goal priority as bonus

            // Training workers based on role
            if (action is TrainBuilderAction)
                bonus += highestPriority * 0.2f;   // 20% of goal priority
            
            if (action is TrainLooterAction)
                bonus += highestPriority * 0.3f;   // 30% of goal priority - boosters for economy

            // Training combat units for defense
            if (action is TrainMeleeAction || action is TrainRangeAction || action is TrainTankerAction || action is TrainSiegerAction)
                bonus += highestPriority * 0.25f;   // 25% for military units

            // Building structures for expansion
            if (action is BuildTowerAction || action is BuildPylonAction || action is BuilderExploreAndBuildPylonAction || action is BuildBarracksAction || action is BuildTrainerAction)
                bonus += highestPriority * 0.4f;   // 40% for building
        }
        else if (highestPriorityGoal is ExpansionGoal)
        {
            // Exploration and building are key for expansion
            if (action is BuilderExploreAndBuildPylonAction || action is BuildPylonAction)
                bonus += highestPriority * 0.6f;   // High bonus for pylon building

            if (action is BuildTowerAction)
                bonus += highestPriority * 0.5f;   // Towers for securing territory

            if (action is BuildBarracksAction || action is BuildTrainerAction)
                bonus += highestPriority * 0.4f;   // Military buildings for expansion

            // Training builders helps expansion
            if (action is TrainBuilderAction)
                bonus += highestPriority * 0.3f;
        }
        return bonus;
    }
}