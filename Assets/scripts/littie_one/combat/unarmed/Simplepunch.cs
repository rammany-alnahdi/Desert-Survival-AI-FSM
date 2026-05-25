using UnityEngine;
using System.Collections; // Add this namespace

public partial class TribeMember
{
    
   public void AnimEvent_StartSwing()
    {
        
        isMidSwing = true; // Tell HandleCombatLogic to apply the offset
        currentTwistAngle = 45f;
        hasHitThisSwing = false; // Reset hit flag for new swing
    }

    public void AnimEvent_Hit()
    {
        if (!hasHitThisSwing)
        {   attackRadius=0.2f;
            // Pass the current values dynamically!
            // Range: 1.5f, Angle: 60f, Damage: 20f, Twist: currentTwistAngle
            PerformHitboxCheck(weaponHandAnchorleft,attackRadius,20f);
            
        }
    }
    
    public void AnimEvent_EndHit()
    {
        currentTwistAngle = 0f;
        isMidSwing = false;
        
        if (targetEnemy != null)
        {
            // 1. Calculate the Target Position
            Vector3 pushDir = (transform.position - targetEnemy.transform.position).normalized;
            animationPosition = transform.position + (pushDir * 5.0f); // 5 meters is plenty
        }
        else
        {
            // Fallback
            animationPosition = transform.position - transform.forward * 5.0f;
        }
        speadofanimationposition = 10f;
        // 2. ENABLE THE FLAG
        isanimationPosition = false;
    }

    private void Simplepunch()
    {
        

        // B. DECIDE ANIMATION
        if (actionCooldown >= actionRate)
        {
            actionCooldown = 0;
            if (anim != null)
            {
                // Logic to pick animation (Random punch, combo, etc.)
                // For now, simple trigger:
                float animSpeed = 1f / Mathf.Max(0.1f, actionRate);
                anim.SetFloat("AttackSpeedMult", animSpeed);
                anim.SetTrigger("Attack"); // This fires AnimEvent_StartSwing -> AnimEvent_Hit
            }
        }
    }
   
}