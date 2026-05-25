using UnityEngine;
using System.Collections.Generic;

public partial  class TribeCommandSystem : MonoBehaviour
{
    [Header("Unit Control Keys")]
    public KeyCode[] jobKeys = new KeyCode[]
    {
        KeyCode.Alpha1, // Herder
        KeyCode.Alpha2, // Warrior
        KeyCode.Alpha3, // Scout
        KeyCode.Alpha4  // Merchant
    };
    public KeyCode equipWeaponKey = KeyCode.E;

    // Optional: auto-find base for GoGetWeapon()
    private BaseStation cachedBase;

    // We'll hook this into Update() – see note below
    public void HandleUnitControls()
{   
    // Press 'B' to test building placement
    if (Input.GetKeyDown(KeyCode.B))
    {
        StartBuildingMode();
    }
    if (selectedUnits.Count == 0) return;

    // --- 1. Job switching (1-4) ---
    for (int i = 0; i < jobKeys.Length; i++)
    {
        if (Input.GetKeyDown(jobKeys[i]))
        {
            TribeMember.UnitType newJob = (TribeMember.UnitType)i;
            foreach (TribeMember unit in selectedUnits)
            {
                if (unit == null || unit.isDead) continue;
                unit.AssignJob(newJob);
                // DO NOT deselect here!
            }
            break;
        }
    }

    // --- 2. Equip weapon (E) ---
    if (Input.GetKeyDown(equipWeaponKey))
    {
        foreach (TribeMember unit in selectedUnits)
        {
            if (unit == null || unit.isDead) continue;
            if (unit.currentJobType == TribeMember.UnitType.Warrior && !unit.HasValidWeapon())
            {
                unit.GoGetWeapon();
            }
        }
    }
}

}