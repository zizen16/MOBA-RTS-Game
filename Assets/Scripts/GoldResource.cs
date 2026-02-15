using UnityEngine;

public class GoldResource : MonoBehaviour
{
    [Header("Gold Resource Settings")]
    public int goldPerTrip = 100;
    public float gatheringTime = 2f;
    public int totalGold = 10000;

    [Header("Depletable Settings")]
    public bool isDepletable = true;
    public int GatherGold()
    {
        if (isDepletable)
        {
            if (totalGold <= 0)
            {
                return 0; // No gold left to gather
            }
            int gathered = Mathf.Min(goldPerTrip, totalGold);
            totalGold -= gathered;

            if (totalGold <= 0)
            {
                // Optionally, you can add logic here to handle resource depletion (e.g., disable the resource)
            }
            return gathered;
        }
        else
        {
            return goldPerTrip; // Infinite resource
        }
    }
    public bool HasGold()
    {
        if (isDepletable)
        {
            return totalGold > 0;
        }
        else
        {
            return true; // Infinite resource
        }
    }
}
