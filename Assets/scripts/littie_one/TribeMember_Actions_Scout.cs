using UnityEngine;
using System.Collections.Generic;
public partial class TribeMember
{
    private void HandleTamingTaskHawk() 
    {
        if (targetObject == null) 
        {
            SetTask(0); 
            return;
        }

        // Distance Check (Made slightly larger to account for the bird flying above the head)
        float distToHawk = Vector3.Distance(transform.position, targetObject.transform.position);
        if (distToHawk > 4.0f) 
        {
            Debug.Log($"{name}: Hawk moved away! Chasing it again...");
            SetTask(1); 
            return;    
        }

        Hawk targetHawk = targetObject.GetComponent<Hawk>();
        if (targetHawk != null)
        {
            // --- 1. DO WE NEED TO BRIBE THE HAWK? ---
            bool isEnemy = targetHawk.gameObject.tag != this.gameObject.tag;
            bool needsFood = targetHawk.isWild || isEnemy;

            if (needsFood)
            {
                // Check for regular Food (Meat) to feed the Hawk
                if (inventory.ContainsKey(ResourceNode.ResourceType.Food) && inventory[ResourceNode.ResourceType.Food] > 0)
                {
                    // Consume food
                    inventory[ResourceNode.ResourceType.Food] -= 1;
                    currentTotalWeight -= 1; // Assuming meat weight is 1
                    targetHawk.isWild = false;
                    
                    if (isEnemy) Debug.Log($"{name} bribed and RAIDED an enemy Hawk!");
                    else Debug.Log($"Success! {name} tamed the wild Hawk!");
                }
                else
                {
                    Debug.Log($"{name} needs Food (Meat) to do this!");
                    SetTask(0);
                    return; // Stop right here, we failed.
                }
            }
            else
            {
                Debug.Log($"{name} is taking over a friendly Hawk. No food needed.");
            }

            // --- 2. THE HANDSHAKE ---
            targetHawk.gameObject.tag = this.gameObject.tag; // Make it your team's color
            targetHawk.myMaster = this; // Set the Scout as the boss
            
            // Tell the Hawk to start flying in a circle around the Scout!
            targetHawk.currentTask = 4; 
        }
        
        SetTask(0); // All done, return to idle
    }
  
}
