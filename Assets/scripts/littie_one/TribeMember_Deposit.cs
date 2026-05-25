using UnityEngine;

public partial class TribeMember
{
    // Task 3: DEPOSITING LOGIC
    void DepositTask()
    {
        if (targetObject == null || !targetObject.CompareTag("Base"))
        {
            Debug.Log("DepositTask: No base to deposit to!");
            SetTask(0);
            return;
        }
        
        BaseStation baseStation = targetObject.GetComponent<BaseStation>();
        if (baseStation == null)
        {
            Debug.Log("DepositTask: No BaseStation component found!");
            SetTask(0);
            return;
        }
        if (currentJobType == UnitType.Warrior && needsWeapon)
        {
            bool gotWeapon = baseStation.TryEquipWarrior(this);
            if (gotWeapon)
            {
                needsWeapon = false;
                hasRequestedWeapon = false;
                Debug.Log($"{name} got weapon after depositing!");
            }
        }
        // Check if we have anything to deposit
        if (inventory.Count == 0 || currentTotalWeight == 0)
        {
            
            Debug.Log("Nothing to deposit!");
            SetTask(0);
            return;
        }
        
        // Try to deposit all resources
        bool success = baseStation.DepositResources(inventory);
        
        if (success)
        {
            // Clear inventory and weight
            inventory.Clear();
            currentTotalWeight = 0;
            
            Debug.Log("Successfully deposited all resources!");
            
            // Gain XP for depositing (optional)
            GainXP(10);
            
            // Go idle after depositing
            SetTask(0);
        }
        else
        {
            Debug.Log("Failed to deposit resources (base storage might be full)");
            
            // If base is full, we should probably keep resources and go idle
            // Or maybe wander around waiting for storage space
            SetTask(0);
        }
    }
}