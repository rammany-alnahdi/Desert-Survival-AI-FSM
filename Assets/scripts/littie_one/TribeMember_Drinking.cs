using UnityEngine;

public partial class TribeMember
{
    void DrinkTask()
    {
        if (targetResource == null) 
        {
            Debug.Log("DrinkTask: targetResource is null!");
            currentTask = 0;
            return;
        }
        
        // Make sure it's actually water
        if (targetResource.type != ResourceNode.ResourceType.water)
        {
            Debug.Log($"DrinkTask: targetResource is not water, it's {targetResource.type}");
            currentTask = 0;
            return;
        }
        
        int gathered = targetResource.Gather(1);
        
        if (gathered > 0)
        {
            // Increase hydration by 30 points per unit of water
            hydration += 30f * gathered;
            if (hydration > 100f) hydration = 100f;
            
            Debug.Log($"Drank water! Hydration now: {hydration}");
            
            // Check if we're fully hydrated
            if (hydration >= 100f)
            {
                Debug.Log("Fully hydrated, stopping drinking");
                targetResource = null;
                currentTask = 0;
            }
            // Otherwise, stay in Task 4 (keep drinking)
        }
        else
        {
            // Water source depleted
            Debug.Log("Water source depleted!");
            targetResource = null;
            currentTask = 0;
        }
    }
}