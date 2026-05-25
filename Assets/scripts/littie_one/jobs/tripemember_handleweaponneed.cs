using UnityEngine;
using System.Collections.Generic;

public partial class TribeMember : MonoBehaviour
{
void CheckWeaponNeeds()
{
    // Only for warriors who have no weapon AND haven't already requested one
    if (currentJobType == UnitType.Warrior && equippedWeapon == null && !hasRequestedWeapon)
    {
        needsWeapon = true;
        GoGetWeapon();   // will set hasRequestedWeapon = true
    }
}

public void GoToBase()
{
    if (hasRequestedWeapon) return;

    GameObject baseStation = GameObject.FindWithTag("Base");
    if (baseStation != null)
    {
        hasRequestedWeapon = true;
        SetIntent(baseStation.transform.position, baseStation, false);
        Debug.Log($"{name} heading to base");
    }
}


}
