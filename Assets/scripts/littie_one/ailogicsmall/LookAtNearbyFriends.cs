using UnityEngine;

public partial class TribeMember
{
    // Add this logic at the end of your AutoScanForNeeds() function 
    // before the final "return false;"
    
    bool LookAtNearbyFriends()
    {
        // 1. Find all colliders within 25 meters
        Collider[] nearby = Physics.OverlapSphere(transform.position, 25f);
        
        foreach (var col in nearby)
        {
            // 2. Check if it's a teammate (same tag) and not me
            if (col.CompareTag(gameObject.tag) && col.gameObject != gameObject)
            {
                TribeMember friend = col.GetComponent<TribeMember>();
                
                // 3. If the friend has a target and is working/fighting
                if (friend != null && friend.targetObject != null && !friend.isDead)
                {
                    // Case A: Join the fight (Friend is in Combat Task 5)
                    if (friend.currentTask == 5 && currentJobType == UnitType.Warrior)
                    {
                        TribeMember enemy = friend.targetObject.GetComponent<TribeMember>();
                        if (enemy != null && !enemy.isDead)
                        {
                            Debug.Log($"{name}: Joining friend {friend.name} to fight {enemy.name}!");
                            SetCombatTarget(enemy);
                            SetTask(5);
                            return true;
                        }
                    }
                    
                    // Case B: Join the gathering (Friend is in Gather Task 2)
                    // Only join if we have space in our inventory
                    if (friend.currentTask == 2 && currentTotalWeight < maxWeightCapacity)
                    {
                        Debug.Log($"{name}: Joining friend {friend.name} to gather {friend.targetObject.name}!");
                        // Copy the friend's target and move there
                        SetIntent(friend.targetObject.transform.position, friend.targetObject, false);
                        return true;
                    }

                    // Case C: Join the drink (Friend is in Drink Task 4)
                    if (friend.currentTask == 4 && hydration < 80f)
                    {
                        SetIntent(friend.targetObject.transform.position, friend.targetObject, false);
                        return true;
                    }
                }
            }
        }
        return false;
    }
}