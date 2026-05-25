using UnityEngine;
using System.Collections.Generic;

public partial class TribeCommandSystem : MonoBehaviour
{
    private void GiveMoveOrder()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 5000f))
            return;

        GameObject hitObj = hit.collider.gameObject;
        
        // 1. ANIMAL CHECK (Camels & Hawks)
        Camel hitCamel = hitObj.GetComponent<Camel>();
        Hawk hitHawk = hitObj.GetComponent<Hawk>();

        // Handle Camel Taming
        if (hitCamel != null )
        {
            HandleAnimalTaming(hitCamel.gameObject, TribeMember.UnitType.Herder, "camel");
            return; 
        }

        // Handle Hawk Taming
        if (hitHawk != null)
        {
            HandleAnimalTaming(hitHawk.gameObject, TribeMember.UnitType.Scout, "hawk");
            return;
        }

        // 2. ENEMY UNIT CLICKED?
        TribeMember hitUnit = hitObj.GetComponent<TribeMember>();
        if (hitUnit != null && IsEnemy(selectedUnits[0], hitUnit))
        {
            foreach (TribeMember unit in selectedUnits)
            {
                if (unit != null && !unit.isDead)
                {
                    unit.SetIntent(hitUnit.transform.position, hitUnit.gameObject, true);
                }
            }
            return; 
        }

        // 3. RESOURCE / BASE / GROUND ORDER
        bool isResource = hitObj.CompareTag("Resources") || 
                         hitObj.CompareTag("WaterSource") || 
                         hitObj.CompareTag("Base");

        for (int i = 0; i < selectedUnits.Count; i++)
        {
            Vector3 center = isResource ? hitObj.transform.position : hit.point;
            Vector3 destination = GetFormationPosition(center, i, selectedUnits.Count, isResource);

            selectedUnits[i].SetIntent(
                destination,
                isResource ? hitObj : null,
                true
            );
        }
    }

    /// <summary>
    /// Helper to find a specific specialist in the selection to go tame an animal.
    /// </summary>
    private void HandleAnimalTaming(GameObject animalObj, TribeMember.UnitType requiredJob, string animalName)
    {
        bool specialistFound = false;

        foreach (TribeMember unit in selectedUnits)
        {
            if (unit != null && !unit.isDead)
            {
                if (unit.currentJobType == requiredJob)
                {
                    // Task 6 is the universal "Tame" task for Herders and Scouts
                    unit.SetIntent(animalObj.transform.position, animalObj, true);
                    Debug.Log($"{unit.name} ({requiredJob}) is going to tame the {animalName}!");
                    specialistFound = true;
                    break; // Only need one specialist to start the job
                }
            }
        }

        if (!specialistFound)
        {
            Debug.Log($"You need a {requiredJob} in your selection to tame this {animalName}!");
        }
    }

    private Vector3 GetFormationPosition(Vector3 center, int index, int totalCount, bool isResource)
    {
        if (isResource)
        {
            float angle = index * (2 * Mathf.PI / 8); 
            float radius = (index >= 8) ? 4.0f : 2.5f;
            return center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
        }
        else
        {
            int cols = Mathf.CeilToInt(Mathf.Sqrt(totalCount));
            float xOffset = (index % cols) * formationSpacing;
            float zOffset = (index / cols) * formationSpacing;
            float centeringX = (cols - 1) * formationSpacing * 0.5f;
            float centeringZ = (totalCount / cols) * formationSpacing * 0.5f;
            return center + new Vector3(xOffset - centeringX, 0, zOffset - centeringZ);
        }
    }

    private bool IsEnemy(TribeMember myUnit, TribeMember otherUnit)
    {
        if (myUnit == null || otherUnit == null) return false;
        if (myUnit == otherUnit) return false;
        return myUnit.tag != otherUnit.tag;   
    }
}