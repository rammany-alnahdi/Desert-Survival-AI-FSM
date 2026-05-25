using UnityEngine;
using System.Collections.Generic;
public partial class TribeMember
{
    [Header("Herder Stats")]
    public List<Camel> assignedCamels = new List<Camel>();
    public int maxCamels = 5;
    // -----------------------------------------------------------------------------------
    private void HandleTamingTaskCamel() 
    {
        if (targetObject == null) 
        {
            SetTask(0); 
            return;
        }

        // Distance Check
        float distToCamel = Vector3.Distance(transform.position, targetObject.transform.position);
        if (distToCamel > 3.0f) 
        {
            Debug.Log($"{name}: Camel moved away! Chasing it again...");
            SetTask(1); 
            return;    
        }

        Camel targetCamel = targetObject.GetComponent<Camel>();
        if (targetCamel != null)
        {
            // --- CAPACITY CHECK ---
            if (assignedCamels.Count >= maxCamels)
            {
                Debug.Log($"{name} already has {maxCamels} camels! Cannot take any more.");
                SetTask(0);
                return;
            }

            // --- 1. DO WE NEED TO BRIBE THE CAMEL? ---
            // We need food if it's Wild OR if it belongs to an Enemy tribe
            bool isEnemy = targetCamel.gameObject.tag != this.gameObject.tag;
            bool needsFood = targetCamel.isWild || isEnemy;

            if (needsFood)
            {
                if (inventory.ContainsKey(ResourceNode.ResourceType.CamelFood) && inventory[ResourceNode.ResourceType.CamelFood] > 0)
                {
                    // Consume food (And put your animations/sounds here later!)
                    inventory[ResourceNode.ResourceType.CamelFood] -= 1;
                    currentTotalWeight -= 2; 
                    targetCamel.isWild = false;
                    
                    if (isEnemy) Debug.Log($"{name} bribed and RAIDED an enemy camel!");
                    else Debug.Log($"Success! {name} tamed the wild camel!");
                }
                else
                {
                    Debug.Log($"{name} needs Camel Food to do this!");
                    SetTask(0);
                    return; // Stop right here, we failed.
                }
            }
            else
            {
                Debug.Log($"{name} is taking over a friendly camel. No food needed.");
            }

            // --- 2. ERASE OLD MASTER MEMORY ---
            // This cleanly handles both Stealing from Enemies AND Trading with Friends
            if (targetCamel.myMaster != null && targetCamel.myMaster != this)
            {
                targetCamel.myMaster.assignedCamels.Remove(targetCamel);
            }

            // --- 3. THE HANDSHAKE ---
            targetCamel.currentTask = 0; // Tell the camel to stop what it is doing
            targetCamel.gameObject.tag = this.gameObject.tag; // Make it your team's color
            targetCamel.myMaster = this; // Set the new boss
            
            // Only add to the list if it isn't already there
            if (!this.assignedCamels.Contains(targetCamel))
            {
                this.assignedCamels.Add(targetCamel); 
            }
        }
        
        SetTask(0); // All done, return to idle
    }
    
  
  
}
