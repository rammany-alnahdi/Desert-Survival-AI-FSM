using UnityEngine;
using System.Collections.Generic;

public partial  class BaseStation : MonoBehaviour
{
    [Header("Base Storage")]
    public Dictionary<ResourceNode.ResourceType, int> storage = new Dictionary<ResourceNode.ResourceType, int>();
    public int maxStoragePerResource = 1000; // Max amount of each resource the base can hold
    
    [Header("Weapon System")]
    public WeaponDatabase weaponDatabase; // Reference to your weapon database asset
    public Dictionary<WeaponData, int> weaponStorage = new Dictionary<WeaponData, int>();
    
    [Header("Visuals")]
    public Transform depositPoint; // Where units stand to deposit
    public GameObject storageIndicator; // Optional: visual indicator of storage level
     private List<WeaponCraftOrder> craftingQueue = new List<WeaponCraftOrder>();
    void Start()
    {
        // Initialize storage for all resource types
        foreach (ResourceNode.ResourceType type in System.Enum.GetValues(typeof(ResourceNode.ResourceType)))
        {
            storage[type] = 100;
        }
        
        // Initialize weapon storage
        InitializeWeaponStorage();

        WeaponData spear = weaponDatabase.GetWeaponByName("Stone Spear");
        if (spear != null)
        {
            weaponStorage[spear] = 5; // Start with 5 spears
        }
    }
    
    void InitializeWeaponStorage()
    {
        // You can pre-populate with some weapons
        if (weaponDatabase != null)
        {
            foreach (var weapon in weaponDatabase.weapons)
            {
                // Start with 0 of each weapon, or set initial values
                weaponStorage[weapon] = 0;
            }
            
            // Example: Start with 5 spears and 3 bows
            WeaponData spear = weaponDatabase.GetWeaponByName("Spear");
            
            if (spear != null) weaponStorage[spear] = 5;
            
        }
    }
    
    // Called by TribeMember when they arrive to deposit
    public bool DepositResources(Dictionary<ResourceNode.ResourceType, int> resourcesToDeposit)
    {
        bool depositedAnything = false;
        
        foreach (var kvp in resourcesToDeposit)
        {
            ResourceNode.ResourceType type = kvp.Key;
            int amount = kvp.Value;
            
            if (amount > 0)
            {
                // Check if we have space
                if (storage[type] + amount <= maxStoragePerResource)
                {
                    storage[type] += amount;
                    depositedAnything = true;
                    Debug.Log($"Base received {amount} {type}. Total {type}: {storage[type]}");
                }
                else
                {
                    // Partial deposit if possible
                    int canTake = maxStoragePerResource - storage[type];
                    if (canTake > 0)
                    {
                        storage[type] += canTake;
                        depositedAnything = true;
                        Debug.Log($"Base received {canTake} {type} (partial, storage full). Total {type}: {storage[type]}");
                    }
                    else
                    {
                        Debug.Log($"Base storage full for {type}!");
                    }
                }
            }
        }
        
        // Update visual indicator if exists
        UpdateStorageIndicator();
        
        return depositedAnything;
    }
    
    // Updated EquipWarrior method
public bool EquipWarrior(TribeMember warrior, WeaponData weapon)
{
    if (warrior == null || weapon == null) return false;
    
    // Check if warrior is actually a warrior
    if (warrior.currentJobType != TribeMember.UnitType.Warrior)
    {
        Debug.Log("Only warriors can equip weapons");
        return false;
    }
    
    // Check if weapon exists in storage
    if (!weaponStorage.ContainsKey(weapon) || weaponStorage[weapon] <= 0)
    {
        Debug.Log($"No {weapon.weaponName} available in storage");
        return false;
    }
    
    // Check level requirement
    if (warrior.level < weapon.requiredLevel)
    {
        Debug.Log($"{warrior.name} needs level {weapon.requiredLevel} to equip {weapon.weaponName}");
        return false;
    }
    
    // Check resource cost
    if (storage.ContainsKey(weapon.requiredResource) && 
        storage[weapon.requiredResource] < weapon.resourceCost)
    {
        Debug.Log($"Not enough {weapon.requiredResource} to craft {weapon.weaponName}");
        return false;
    }
    
    // Deduct resources if needed
    if (weapon.resourceCost > 0)
    {
        if (!WithdrawResources(weapon.requiredResource, weapon.resourceCost))
        {
            return false;
        }
    }
    
    // Remove weapon from storage
    weaponStorage[weapon]--;
    
    // Unequip current weapon and return to storage
    if (warrior.equippedWeapon != null)
    {
        if (weaponStorage.ContainsKey(warrior.equippedWeapon))
        {
            weaponStorage[warrior.equippedWeapon]++;
        }
        else
        {
            weaponStorage[warrior.equippedWeapon] = 1;
        }
    }
    
    return true;
}

    
    // Return weapon to storage
    public void ReturnWeaponToStorage(WeaponData weapon)
    {
        if (weapon == null) return;
        
        if (weaponStorage.ContainsKey(weapon))
        {
            weaponStorage[weapon]++;
        }
        else
        {
            weaponStorage[weapon] = 1;
        }
    }
    
    // Add weapon to storage (crafting or finding)
    public void AddWeaponToStorage(WeaponData weapon, int amount = 1)
    {
        if (weapon == null) return;
        
        if (weaponStorage.ContainsKey(weapon))
        {
            weaponStorage[weapon] += amount;
        }
        else
        {
            weaponStorage[weapon] = amount;
        }
        
        Debug.Log($"Added {amount} {weapon.weaponName} to storage");
        UpdateStorageIndicator();
    }
    
    // Get list of available weapons for a warrior's level
    public List<WeaponData> GetAvailableWeapons(int warriorLevel)
    {
        List<WeaponData> available = new List<WeaponData>();
        
        foreach (var kvp in weaponStorage)
        {
            WeaponData weapon = kvp.Key;
            int count = kvp.Value;
            
            if (count > 0 && weapon.requiredLevel <= warriorLevel)
            {
                // Also check if we have the required resource
                bool hasResource = storage.ContainsKey(weapon.requiredResource) && 
                                  storage[weapon.requiredResource] >= weapon.resourceCost;
                
                if (hasResource)
                {
                    available.Add(weapon);
                }
            }
        }
        
        return available;
    }
    
    void UpdateStorageIndicator()
    {
        if (storageIndicator != null)
        {
            // Example: Scale indicator based on total storage percentage
            float totalPercentage = GetTotalStoragePercentage();
            storageIndicator.transform.localScale = Vector3.one * (0.5f + totalPercentage * 0.5f);
        }
    }
    
    public float GetTotalStoragePercentage()
    {
        int totalStored = 0;
        int maxTotal = storage.Count * maxStoragePerResource;
        
        foreach (var amount in storage.Values)
        {
            totalStored += amount;
        }
        
        return (float)totalStored / maxTotal;
    }
    
    // Get a specific resource amount
    public int GetResourceAmount(ResourceNode.ResourceType type)
    {
        return storage.ContainsKey(type) ? storage[type] : 0;
    }
    
    // Remove resources from storage (for building/crafting)
    public bool WithdrawResources(ResourceNode.ResourceType type, int amount)
    {
        if (storage.ContainsKey(type) && storage[type] >= amount)
        {
            storage[type] -= amount;
            UpdateStorageIndicator();
            return true;
        }
        return false;
    }
    
    // UI method to show weapon storage
    public string GetWeaponStorageInfo()
    {
        string info = "Weapon Storage:\n";
        foreach (var kvp in weaponStorage)
        {
            if (kvp.Value > 0)
            {
                info += $"{kvp.Key.weaponName}: {kvp.Value} (Req. Level: {kvp.Key.requiredLevel})\n";
            }
        }
        return info;
    }
    
    // GUI for debugging (optional)
    void OnGUI()
    {
        if (Camera.main != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            if (screenPos.z > 0)
            {
                GUI.Label(new Rect(screenPos.x, Screen.height - screenPos.y, 200, 100), "Base Storage:");
                foreach (var kvp in storage)
                {
                    GUI.Label(new Rect(screenPos.x, Screen.height - screenPos.y + 20 + (int)kvp.Key * 20, 200, 20), 
                              $"{kvp.Key}: {kvp.Value}/{maxStoragePerResource}");
                }
                
                // Show weapon storage
                GUI.Label(new Rect(screenPos.x, Screen.height - screenPos.y + 100, 200, 100), GetWeaponStorageInfo());
            }
        }
    }
}