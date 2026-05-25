using UnityEngine;
using System.Collections.Generic;

public partial class TribeMember : MonoBehaviour
{
    void GatherTask()
{
    if (targetResource == null) 
    {
        currentTask = 0;
        return;
    }
    
    int weight = targetResource.weightPerUnit;

    // Check if we have space BEFORE gathering
    if (currentTotalWeight + weight <= maxWeightCapacity)
    {
        int gathered = targetResource.Gather(1);
        
        if (gathered > 0)
        {
            UpdateInventory(targetResource.type, gathered, weight * gathered);
            GainXP(5 * gathered);
            
            // Check if we're at capacity
            if (currentTotalWeight >= maxWeightCapacity) 
            {
                targetResource = null;
                GoHome();
            }
        }
        
        // Check if resource still exists (might have been destroyed by Gather())
        if (targetResource == null)
        {
            currentTask = 0;
        }
    }
    else
    {
        // We are full, stop gathering and go home
        GoHome();
    }
}
public void GoGetWeapon()
{
    // Only warriors without a weapon should go
    if (currentJobType != UnitType.Warrior || HasValidWeapon())
        return;

    // Mark that we need a weapon (if not already marked)
    if (!needsWeapon)
        needsWeapon = true;

    // Prevent spamming the order while already en route
    if (hasRequestedWeapon)
        return;

    // Move to base – GoHome() calls SetIntent to the Base
    GoHome();
    hasRequestedWeapon = true;
}
        void UpdateInventory(ResourceNode.ResourceType type, int amt, int weight) 
        { 
            if (inventory.ContainsKey(type)) 
                inventory[type] += amt; 
            else 
                inventory.Add(type, amt); 
                
            currentTotalWeight += weight; 
        }

    void GoHome()
    {
        GameObject baseStation = GameObject.FindWithTag("Base");
        
        if (baseStation != null)
        {
            // This will automatically handle the transition from travel to deposit
            SetIntent(baseStation.transform.position, baseStation, false);
            // Task will be set to 3 (DEPOSITING) automatically upon arrival
        }
        else
        {
            Debug.LogWarning(name + " can't find a Base to go home to!");
            SetTask(0);
        }
    }
}