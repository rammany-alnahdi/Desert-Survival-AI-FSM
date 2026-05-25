using UnityEngine;
using System.Collections; // Add this namespace

public partial class TribeMember
{
    public void PerformMovement(Vector3 destination, float speed)
    {
        // 1. CALCULATE DIRECTION
        Vector3 moveDir = (destination - transform.position).normalized;
        
        // 2. APPLY SEPARATION (Crucial for group fights)
        Vector3 separation = GetSeparationForce(); 
        Vector3 finalMove = (moveDir + separation).normalized;

        // 3. APPLY POSITION
        // We manually push the Transform. 
        transform.position += finalMove * speed * Time.deltaTime;
    }
    public TribeMember targetEnemy;
    public void SetCombatTarget(TribeMember enemy)
    {
        targetEnemy = enemy;
        targetObject = enemy.gameObject; 
    }
   private void PerformCombatRotation(float speed)
    {
        Vector3 dirToEnemy = (targetEnemy.transform.position - transform.position).normalized;
        dirToEnemy.y = 0; // Flatten

        if (dirToEnemy != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(dirToEnemy);

            // Apply Twist (Set by Animation Event)
            if (isMidSwing)
            {
                lookRot *= Quaternion.Euler(0, currentTwistAngle, 0);
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * speed);
        }
    }
    private void HandleChaseMovement()
    {

        targetPosition = targetEnemy.transform.position;
        Vector3 moveDir = (targetPosition - transform.position).normalized;
        Vector3 separation = GetSeparationForce();
        Vector3 finalMove = (moveDir + separation).normalized;

        transform.position += finalMove * currentMoveSpeed * Time.deltaTime;
        LookAt(targetPosition);
    }

   private float GetRangeForStyle(CombatStyle style)
    {
        switch (style)
        {
            case CombatStyle.Unarmed: return 0.7f;
            case CombatStyle.Spear: return 2.5f; // Spears reach far
            case CombatStyle.Axe: return 1.2f;
            default: return 1.0f;
        }
    }

    // --- COMBAT LOOP ---

   public void HandleCombatLogic()
    {
        // A. Validation
        if (targetEnemy == null || targetEnemy.isDead)
        {
            targetEnemy = null;
            SetTask(0);
            return;
        }

        // B. Global Cooldown Timer (Always runs)
        //actionCooldown += Time.deltaTime; already exest in main file update before play HandleCombatLogic if task 5

        // C. Pass control to the Decision Brain
        ResolveCombatState();
        
    }

    // -----------------------------------------------------------------------
    // 2. THE DECISION BRAIN (Range Check -> Style Select)
    // -----------------------------------------------------------------------
    private void ResolveCombatState()
    {
        // A. GET RANGE BASED ON STYLE
        float requiredRange = GetRangeForStyle(currentCombatStyle);
        float dist = Vector3.Distance(transform.position, targetEnemy.transform.position);

        // B. CHECK DISTANCE
        if (dist > requiredRange && !isMidSwing && !isanimationPosition)
        {
            // CHASE MODE
            HandleChaseMovement(); 
            isMoving = true;
        }
        else
        {
            // ATTACK MODE (In Range)
            isMoving = false;

            // 1. Lock Physics/NavMesh
            UnityEngine.AI.NavMeshAgent nav = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (nav != null) { nav.updateRotation = false;nav.updatePosition = false; nav.isStopped = true; }
            if (isanimationPosition)
            {

                // B. Move Legs (Backward)
                PerformMovement(animationPosition, speadofanimationposition);

                // C. Rotate Body (Strafe - Keep looking at enemy) 

                // D. ARRIVAL CHECK (The missing piece!)
                float distToTarget = Vector3.Distance(transform.position, animationPosition);
                
                // If we are close (0.2m) OR if we are stuck
                if (distToTarget < 0.2f)
                {
                    isanimationPosition = false; // Stop moving
                }

                // E. STOP HERE. Do not execute Chase or Attack logic while retreating.
                return; 
            }
            if(isMidSwing)
            PerformCombatRotation(currentTwistAngle); 

            // 2. MOVEMENT: Move towards the retreat position
            

          

            // 2. Delegate to Specific Weapon Logic
            switch (currentCombatStyle)
            {
                case CombatStyle.Unarmed:
                    HandleUnarmedCombat();
                    break;
                
                case CombatStyle.Spear:
                    // HandleSpearCombat(); // Add later
                    break;

                case CombatStyle.Axe:
                    // HandleAxeCombat(); // Add later
                    break;
            }
            
        }
    }
    float attackRadius;

    public void PerformHitboxCheck(Transform hitOrigin, float radius, float damage)
    {
        // 1. DETERMINE CENTER
        // If the animation didn't provide a transform, fallback to chest
        Vector3 center = (hitOrigin != null) ? hitOrigin.position : (transform.position + transform.forward + Vector3.up);

        // 2. PHYSICS CHECK
        int layerMask = LayerMask.GetMask("HideableObject", "VisionSource");
        Collider[] hits = Physics.OverlapSphere(center, radius, layerMask);

        foreach (var hit in hits)
        {
            TribeMember enemy = hit.GetComponent<TribeMember>();

            // Validation
            if (enemy == null || enemy == this || enemy.isDead) continue;
            if (enemy.CompareTag(gameObject.tag)) continue; 

            // 3. APPLY DAMAGE
            // No angle math needed because the sphere IS on the weapon/hand.
            enemy.TakeDamage(damage, this);
            
            hasHitThisSwing = true;
            Debug.Log($"Hit {enemy.name} for {damage}!");
            
            // Optional: Break to hit only 1 target per swing
            break; 
        }
    }

    // GIZMOS: Update to verify the variable radius
    void OnDrawGizmosSelected()
    {
        if (weaponHandAnchorleft != null)
        {
            Gizmos.color = new Color(1, 0, 0, 0.5f);
            Gizmos.DrawSphere(weaponHandAnchorleft.position, attackRadius);
        }
    }
}