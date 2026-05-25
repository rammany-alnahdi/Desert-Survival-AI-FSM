using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class WeaponData
{
    public string weaponName;
    public GameObject weaponPrefab; // Visual object to attach to hand
    public float damageMultiplier = 1f;
    public float range = 1.5f; // Attack range
    public float attackSpeed = 1f;
    public bool isRanged = false;
    public GameObject projectilePrefab; // For ranged weapons
    public ResourceNode.ResourceType requiredResource; // What resource is consumed
    public int resourceCost = 1; // How many needed from storage
    public int requiredLevel = 1; // Minimum level to equip this weapon
}

[CreateAssetMenu(fileName = "WeaponDatabase", menuName = "TribeWars/WeaponDatabase")]
public class WeaponDatabase : ScriptableObject
{
    public WeaponData[] weapons;
    
    // Helper method to get weapon by name
    public WeaponData GetWeaponByName(string name)
    {
        foreach (var weapon in weapons)
        {
            if (weapon.weaponName == name)
                return weapon;
        }
        return null;
    }
}