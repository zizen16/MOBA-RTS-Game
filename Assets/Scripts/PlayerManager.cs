using UnityEngine;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;
    public int startingGold = 1000;

    public int currentGold = 0;
    public int currentPopulation = 0;
    public int maxPopulation = 0;

    List<BuildingData> completedBuildings = new List<BuildingData>();
    List<BaseBuilding> playerBuildings = new List<BaseBuilding>();//<-- type mo ito
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        currentGold = startingGold;
    }
    //resource management
    public void AddGold(int amount)
    {
        currentGold += amount;
    }
    public bool HasEnoughGold(int amount)
    {
        return currentGold >= amount;
    }
    public bool SpendGold(int amount)
    {
        if (HasEnoughGold(amount))
        {
            currentGold -= amount;
            return true;
        }
        return false;
    }
    public bool HasPopulationForUnit(int populationCost) // to check if there is enough population for a unit
    {
        return (currentPopulation + populationCost) <= maxPopulation;
    }

    //add Nax Population from buildings
    public void AddPopulationFromBuildings(int amount)
    {
        maxPopulation += amount;
    }
    public void RemovePopulationFromBuildings(int amount)
    {
        maxPopulation -= amount;
    }

    //Add current Population when unit is created
    public void RegisterUnit(UnitData unitData)
    {
        currentPopulation += unitData.populationCost;
    }
    public void UnregisterUnit(UnitData unitData)
    {
        currentPopulation -= unitData.populationCost;
    }

    //Record completed buildings
    public void RegisterBuilding(BuildingData buildingData)
    {
        completedBuildings.Add(buildingData);
        AddPopulationFromBuildings(buildingData.populationProvided);
    }
    public void UnregisterBuilding(BuildingData buildingData)
    {
        completedBuildings.Remove(buildingData);
        RemovePopulationFromBuildings(buildingData.populationProvided);
    }
    //Record base building
    public void RegisterBaseBuilding(BaseBuilding building)
    {
        playerBuildings.Add(building);
    }
    public void UnregisterBaseBuilding(BaseBuilding building)
    {
        playerBuildings.Remove(building);
    }
    //building prerequisites
    public bool ArePresrequisitesMet(BuildingData buildingData)
    {
        foreach (var prereq in buildingData.prerequisiteBuildings)
        {
            if (!completedBuildings.Contains(prereq))
            {
                return false;
            }
        }
        return true;
    }
    public bool CanBuild(BuildingData buildingData)
    {
        if (HasEnoughGold(buildingData.goldCost) && ArePresrequisitesMet(buildingData))
        {
            return true;
        }
        return false;
    }
    //Find Command Center
    public CommandCenter FindCommandCenter()
    {
        foreach (var building in playerBuildings)
        {
            if (building is CommandCenter)
            {
                return (CommandCenter)building;
            }
        }
        return null;
    }
}
