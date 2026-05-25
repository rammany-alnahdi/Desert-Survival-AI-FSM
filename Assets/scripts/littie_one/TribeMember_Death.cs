using UnityEngine;
using System.Collections.Generic;

public partial class TribeMember : MonoBehaviour
{
        void Die() 
    { 
        if (isDead) return; // Prevent double-triggering
        isDead = true; 

        if (anim != null) 
        {
            anim.SetTrigger("Die");
            // Optional: If you want to ensure it stops immediately
            anim.SetFloat("Speed", 0f); 
        }

        isMoving = false;
        currentTask = 0; // Stop gathering/drinking logic

        // Disable the selection circle so it doesn't stay on the ground
        if (selectionIndicator) selectionIndicator.SetActive(false);

        // Disable the Collider so other units can walk through the body
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Destroy after 5 seconds to let the player see the body
        Destroy(gameObject, 100f); 
    }
}