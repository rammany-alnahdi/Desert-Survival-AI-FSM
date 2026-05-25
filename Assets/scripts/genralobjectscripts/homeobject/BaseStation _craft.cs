using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class WeaponCraftOrder
{
    public WeaponData weapon;
    public int amount;
    public float timeRemaining;
}

public partial class BaseStation : MonoBehaviour
{
    void CheckAutoCraftWeapons(ResourceNode.ResourceType depositedType, int depositedAmount)
    {
        if (weaponDatabase == null) return;
        
        foreach (WeaponData weapon in weaponDatabase.weapons)
        {
            // If this weapon needs the deposited resource type
            if (weapon.requiredResource == depositedType)
            {
                // Check if we have enough to craft one
                if (storage[depositedType] >= weapon.resourceCost)
                {
                    // Check if we're low on this weapon
                    int currentStock = weaponStorage.ContainsKey(weapon) ? 
                                      weaponStorage[weapon] : 0;
                    
                    if (currentStock < 5) // Only craft if we have less than 5
                    {
                        CraftWeapon(weapon, 1);
                    }
                }
            }
        }
    }
    
    public bool CraftWeapon(WeaponData weapon, int amount = 1)
    {
        if (weapon == null) return false;
        
        int totalCost = weapon.resourceCost * amount;
        
        // Check resources
        if (!HasEnoughResources(weapon.requiredResource, totalCost))
        {
            Debug.Log($"Not enough {weapon.requiredResource} to craft {weapon.weaponName}");
            return false;
        }
        
        // Deduct resources
        if (!WithdrawResources(weapon.requiredResource, totalCost))
        {
            return false;
        }
        
        // Add to inventory
        if (weaponStorage.ContainsKey(weapon))
        {
            weaponStorage[weapon] += amount;
        }
        else
        {
            weaponStorage[weapon] = amount;
        }
        
        Debug.Log($"Crafted {amount} {weapon.weaponName}(s)");
        return true;
    }
    
    bool HasEnoughResources(ResourceNode.ResourceType type, int amount)
    {
        return storage.ContainsKey(type) && storage[type] >= amount;
    }
    
    // Check if we have a specific weapon in stock
    public bool HasWeaponInStock(string weaponName)
    {
        // Find the weapon data first
        WeaponData weapon = GetWeaponData(weaponName);
        if (weapon == null) return false;
        
        return weaponStorage.ContainsKey(weapon) && weaponStorage[weapon] > 0;
    }
    
    public bool HasWeaponInStock(WeaponData weapon)
    {
        if (weapon == null) return false;
        return weaponStorage.ContainsKey(weapon) && weaponStorage[weapon] > 0;
    }
    
    // Get weapon from database by name
    public WeaponData GetWeaponData(string weaponName)
    {
        if (weaponDatabase == null) return null;
        return weaponDatabase.GetWeaponByName(weaponName);
    }
    
    // Try to equip a warrior with a weapon
    public bool TryEquipWarrior(TribeMember warrior, string weaponName = "")
    {
        if (warrior.currentJobType != TribeMember.UnitType.Warrior)
        {
            Debug.Log($"{warrior.name} is not a warrior, cannot equip weapon");
            return false;
        }
        
        WeaponData weaponToEquip = null;
        
        // If specific weapon requested
        if (!string.IsNullOrEmpty(weaponName))
        {
            weaponToEquip = GetWeaponData(weaponName);
        }
        
        // If no specific weapon or not found, find best available
        if (weaponToEquip == null)
        {
            weaponToEquip = FindBestAvailableWeapon(warrior.level);
        }
        
        if (weaponToEquip == null)
        {
            Debug.Log("No weapons available to equip");
            return false;
        }
        
        // Check if warrior meets requirements
        if (warrior.level < weaponToEquip.requiredLevel)
        {
            Debug.Log($"{warrior.name} needs level {weaponToEquip.requiredLevel} for {weaponToEquip.weaponName}");
            return false;
        }
        
        // Check stock
        if (!HasWeaponInStock(weaponToEquip))
        {
            Debug.Log($"No {weaponToEquip.weaponName} in stock");
            return false;
        }
        
        // Equip the weapon
        return EquipWarriorWithWeapon(warrior, weaponToEquip);
    }
    
    bool EquipWarriorWithWeapon(TribeMember warrior, WeaponData weapon)
    {
        // Remove from inventory
        weaponStorage[weapon]--;
        
        // Unequip current weapon first (return to inventory)
        if (warrior.equippedWeapon != null)
        {
            WeaponData oldWeapon = warrior.equippedWeapon;
            if (weaponStorage.ContainsKey(oldWeapon))
            {
                weaponStorage[oldWeapon]++;
            }
            else
            {
                weaponStorage[oldWeapon] = 1;
            }
        }
        
        // Equip new weapon
        warrior.EquipWeapon(weapon);
        
        Debug.Log($"Equipped {warrior.name} with {weapon.weaponName}");
        Debug.Log($"Remaining {weapon.weaponName} in stock: {weaponStorage[weapon]}");
        
        return true;
    }
    
    WeaponData FindBestAvailableWeapon(int warriorLevel)
    {
        if (weaponDatabase == null) return null;
        
        WeaponData bestWeapon = null;
        float bestScore = 0;
        
        foreach (WeaponData weapon in weaponDatabase.weapons)
        {
            // Check level requirement and stock
            if (warriorLevel >= weapon.requiredLevel && HasWeaponInStock(weapon))
            {
                // Score: damage + range bonus
                float score = weapon.damageMultiplier + (weapon.range * 0.1f);
                if (weapon.isRanged) score *= 1.3f; // Ranged bonus
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestWeapon = weapon;
                }
            }
        }
        
        return bestWeapon;
    }
    
    // Get current weapon stock info for UI/debug
    public string GetWeaponStockInfo()
    {
        string info = "Weapon Stock:\n";
        foreach (var kvp in weaponStorage)
        {
            info += $"{kvp.Key.weaponName}: {kvp.Value}\n";
        }
        return info;
    }
    
    // Warrior comes to base to get weapon
    public void RequestWeaponForWarrior(TribeMember warrior)
    {
        if (TryEquipWarrior(warrior))
        {
            Debug.Log($"{warrior.name} successfully equipped at base");
        }
        else
        {
            Debug.Log($"{warrior.name} could not get weapon at base");
        }
    }
}