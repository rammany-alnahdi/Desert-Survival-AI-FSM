using UnityEngine;
using System.Collections;

public partial class TribeMember
{
    // Weapon-related methods
    public void EquipWeapon(WeaponData weapon)
    {
        if (isDead) return;
        
        if (currentJobType != UnitType.Warrior && weapon != null)
        {
            Debug.Log($"{name} is not a warrior, cannot equip weapon");
            return;
        }
        
        // Unequip current weapon first
        UnequipWeapon();
        
        equippedWeapon = weapon;
        
        if (!HasValidWeapon())
        {
            currentCombatStyle = CombatStyle.Unarmed;
            return;
        }
        
        // Instantiate weapon visual
        if (weaponHandAnchorright != null)
        {
            currentWeaponObject = Instantiate(
                weapon.weaponPrefab,
                weaponHandAnchorright.position,
                weaponHandAnchorright.rotation,
                weaponHandAnchorright
            );
            
            // Adjust position/rotation if needed
            currentWeaponObject.transform.localPosition = Vector3.zero;
            currentWeaponObject.transform.localRotation = Quaternion.identity;
            
            // Get weapon collider for melee weapons
            if (!weapon.isRanged)
            {
                weaponCollider = currentWeaponObject.GetComponent<Collider>();
                if (weaponCollider != null)
                {
                    weaponCollider.isTrigger = true;
                    weaponCollider.enabled = false; // Only enable during attacks
                }
            }
        }
        
        // Update combat style based on weapon
        UpdateCombatStyleFromWeapon(weapon);
        
        Debug.Log($"{name} equipped {weapon.weaponName}");
    }
    
    private void UpdateCombatStyleFromWeapon(WeaponData weapon)
    {
        // Simple mapping - you can expand this
        if (weapon.isRanged)
        {
            currentCombatStyle = CombatStyle.Ranged;
        }
        else if (weapon.weaponName.Contains("Spear"))
        {
            currentCombatStyle = CombatStyle.Spear;
        }
        else if (weapon.weaponName.Contains("Axe"))
        {
            currentCombatStyle = CombatStyle.Axe;
        }
        else
        {
            currentCombatStyle = CombatStyle.Melee;
        }
    }
    
    public void UnequipWeapon()
    {
        if (currentWeaponObject != null)
        {
            Destroy(currentWeaponObject);
            currentWeaponObject = null;
        }
        
        if (weaponCollider != null)
        {
            weaponCollider = null;
        }
        
        equippedWeapon = null;
        currentCombatStyle = CombatStyle.Unarmed;
    }
    
   
    
    private BaseStation FindBaseStation()
    {
        GameObject baseObj = GameObject.FindGameObjectWithTag("Base");
        if (baseObj != null)
        {
            return baseObj.GetComponent<BaseStation>();
        }
        return null;
    }
    
    private void FindAndEquipAvailableWeapon()
{
    BaseStation baseStation = FindBaseStation();
    if (baseStation == null || baseStation.weaponDatabase == null)
        return;

    foreach (WeaponData weapon in baseStation.weaponDatabase.weapons)
    {
        if (weapon.requiredLevel <= level)
        {
            if (baseStation.EquipWarrior(this, weapon))   // EquipWarrior calls warrior.EquipWeapon()
            {
                // EquipWeapon already sets equippedWeapon and visual
                break;
            }
        }
    }
}
    

}

// WeaponHitDetector for melee weapons
public class WeaponHitDetector : MonoBehaviour
{
    private TribeMember owner;
    private float lastHitTime = 0f;
    private float hitCooldown = 0.5f;
    
    public void Initialize(TribeMember owner)
    {
        this.owner = owner;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (Time.time - lastHitTime < hitCooldown) return;
        
        TribeMember target = other.GetComponent<TribeMember>();
        if (target != null && target != owner && !target.isDead)
        {
            // Check if owner is attacking
            if (owner != null && owner.currentTask == 5) // Combat task
            {
                float damage = owner.attackPower * (owner.equippedWeapon != null ? 
                    owner.equippedWeapon.damageMultiplier : 1f);
                target.TakeDamage(damage);
                lastHitTime = Time.time;
                
                Debug.Log($"{owner.name} hit {target.name} for {damage} damage");
            }
        }
    }
}