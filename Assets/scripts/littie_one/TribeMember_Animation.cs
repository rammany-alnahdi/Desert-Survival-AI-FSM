using UnityEngine;
using System.Collections.Generic;

public partial class TribeMember : MonoBehaviour
{
   
        void UpdateAnimatorParameters()
    {
        if (anim != null)
    {
        float multiplier = 1f / actionRate; 
        anim.SetFloat("AttackSpeedMult", multiplier);
    }
        if (anim == null) return;
        anim.SetFloat("Speed", isMoving ? 1f : 0f);
        anim.SetBool("IsGathering", currentTask == 2);
        anim.SetBool("IsDrinking", currentTask == 4);
        // Optional: Show "Heavy" walk if carrying a lot
        anim.SetBool("IsCarryingHeavy", currentTotalWeight > maxWeightCapacity * 0.5f);
    }
}