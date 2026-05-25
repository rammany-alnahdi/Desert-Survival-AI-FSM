using UnityEngine;
using System.Collections.Generic;

public partial class TribeMember : MonoBehaviour
{
        void GainXP(int amount)
    {
        experiencePoints += amount;
        // Level up threshold: Level * 50 (e.g., 50, 100, 150)
        if (experiencePoints >= level * 50)
        {
            level++;
            experiencePoints = 0;
            
            // Level Up Rewards
            actionRate *= 0.95f; // Gather faster
            baseMoveSpeed += 0.2f; // Walk faster
            attackPower += 1f;
            
            Debug.Log(name + " leveled up to " + level + "!");
            // Optional: Play a particle effect here if you have one
        }
    }

   
    // --- JOBS & STATS ---
   // In TribeMember_Stats.cs - UPDATE the AssignJob function:
public void AssignJob(UnitType newJob)
{
    // --- Reset flags for the new job ---
    // Clear any pending weapon request (we'll set a fresh one)
    needsWeapon = false;
    needToReturnWeapon = false;
    hasRequestedWeapon = false;

    currentJobType = newJob;

    switch (newJob)
    {
        case UnitType.Herder:
            attackPower = 4f; defense = 2f; waterDrainRate = 0.5f; baseMoveSpeed = 4f; maxWeightCapacity = 30;
            // If holding a weapon, return it to base
            if (HasValidWeapon())
            {
                needToReturnWeapon = true;
                GoHome();                // walk to base
                hasRequestedWeapon = true;
            }
            break;

        case UnitType.Warrior:
            attackPower = 8f; defense = 6f; waterDrainRate = 1.5f; baseMoveSpeed = 5.5f; maxWeightCapacity = 20;
            // Warriors need weapons
            if (!HasValidWeapon())
            {
                needsWeapon = true;
                GoGetWeapon();           // walk to base and request one
                // hasRequestedWeapon set inside GoGetWeapon()
            }
            break;

        case UnitType.Scout:
            attackPower = 5f; defense = 4f; waterDrainRate = 1.2f; baseMoveSpeed = 9f; maxWeightCapacity = 10;
            // Scouts fight unarmed – drop any weapon
            if (!HasValidWeapon())
            {
                needToReturnWeapon = true;
                GoHome();
                hasRequestedWeapon = true;
            }
            break;

        case UnitType.Merchant:
            attackPower = 3f; defense = 5f; waterDrainRate = 1.0f; baseMoveSpeed = 4.5f; maxWeightCapacity = 80;
            // Merchants don't fight – drop weapon
            if (HasValidWeapon())
            {
                needToReturnWeapon = true;
                GoHome();
                hasRequestedWeapon = true;
            }
            break;
    }
}




// NEW: Update animator based on job

    void UpdateMovementSpeed()
    {
        float levelSpeedBonus = 1f + (level * 0.02f);
        // Slow down if carrying heavy items
        float weightRatio = (float)currentTotalWeight / maxWeightCapacity;
        currentMoveSpeed = Mathf.Lerp(baseMoveSpeed, baseMoveSpeed * 0.3f, weightRatio) * levelSpeedBonus;
    }

    // --- HEALTH & PHYSICS ---
    public void TakeDamage(float dmg, TribeMember attacker = null)
{
    health -= Mathf.Max(dmg - defense, 0.001f);
    
    // 1. Check Death
    if (health <= 0 && !isDead) 
    {
        Die();
        return;
    }

    // 2. RETALIATION LOGIC
    // If I'm alive and I know who hit me...
    if (attacker != null && !isDead)
    {
        // ...and I'm not already locked in a duel with someone else
        if (currentTask != 5 || targetEnemy == null)
        {
            Debug.Log($"{name}: Ouch! {attacker.name} attacked me! Engaging!");
            
            // TARGET THEM
            SetCombatTarget(attacker);
            
            // SWITCH TO COMBAT MODE
            SetTask(5); 
        }
    }
}   
}