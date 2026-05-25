using UnityEngine;

public partial class TribeMember
{

    
   // In TribeMember_Systems.cs

void HandleSensorsAndSurvival()
{
    // 1. Physical Needs (Always ticking)
    hydration -= waterDrainRate * Time.deltaTime;
    if (hydration <= 0) 
    { 
        hydration = 0; 
        TakeDamage(0.002f * Time.deltaTime); 
    }

    // 2. SKIP RULES (When should we be totally blind?)
    // - If dead
    // - If Player commanded (we obey player orders blindly)
    // - If already in Combat (Task 5 handles its own targeting)
    if (isDead || isPlayerCommanded || currentTask == 5) return;


    // 3. THE SENSOR PULSE (Check every 0.5 seconds to save performance)
    scanTimer += Time.deltaTime;
    if (scanTimer > 0.5f)
    {
        scanTimer = 0f;

        // A. Are we busy doing something?
        bool isBusy = (currentTask != 0);

        // B. Run the scan
        // If Busy -> True (Check Danger Only)
        // If Idle -> False (Check Everything)
        bool foundThreat = AutoScanForNeeds(isBusy);

        // C. If we are Idle and found nothing, check friends or look around
        if (!isBusy && !foundThreat)
        {
            bool foundFriendActivity = LookAtNearbyFriends();
            
            if (!foundFriendActivity)
            {
                // Random idle look logic
                idleScanDelay -= 0.5f;
                if (idleScanDelay <= 0)
                {
                    idleScanDelay = Random.Range(3f, 8f);
                    LookAroundRandomly();
                }
            }
        }
    }
}

    // Helper to check the TribeVisionSensor visibleItems list
   
    // Reuse array to save memory
   private Collider[] allyResults = new Collider[20]; 
   bool AutoScanForNeeds(bool checkDangerOnly)
    {
        TribeVisionSensor vision = GetComponent<TribeVisionSensor>();
        if (vision == null || vision.visibleItems.Count == 0) return false;

        // --- 1. ALWAYS CHECK FOR ENEMIES (High Priority) ---
        string targetTag = gameObject.CompareTag("Player") ? "Enemy" : "Player";
        TribeMember closestEnemy = null;
        float closestDist = float.MaxValue;
        int visibleEnemyCount = 0;

        foreach (var item in vision.visibleItems)
        {
            if (item.obj == null) continue;

            // Strict Enemy Check
            if (item.obj.CompareTag(targetTag))
            {
                visibleEnemyCount++;
                float d = Vector3.Distance(transform.position, item.obj.transform.position);
                if (d < closestDist)
                {
                    closestDist = d;
                    closestEnemy = item.obj.GetComponent<TribeMember>();
                }
            }
        }

        if (closestEnemy != null && !closestEnemy.isDead)
        {
            // WARRIOR: Fight
            if (currentJobType == UnitType.Warrior)
            {
                // If I am already fighting THIS guy, don't reset logic
                if (currentTask == 5 && targetEnemy == closestEnemy) return false;

                if (visibleEnemyCount <= 2)
                {
                    Debug.Log($"{name}: Interrupted task to engage {closestEnemy.name}!");
                    SetCombatTarget(closestEnemy);
                    SetTask(5);
                    return true;
                }
                else
                {
                     // Too many enemies? Retreat!
                     RunAwayFrom(closestEnemy);
                     return true;
                }
            }
            // VILLAGER: Flee
            else
            {
                Debug.Log($"{name}: Interrupted task to FLEE from {closestEnemy.name}!");
                RunAwayFrom(closestEnemy);
                return true;
            }
        }

        // --- IF WE ONLY WANT DANGER CHECKS, STOP HERE ---
        if (checkDangerOnly) return false;

        // --- 2. SURVIVAL (Water) ---
        if (hydration < 30f)
        {
            foreach (var item in vision.visibleItems)
            {
                if (item.tag == "WaterSource")
                {
                    SetIntent(item.obj.transform.position, item.obj, false); 
                    return true; 
                }
            }
        }

        // --- 3. GATHERING (Resources) ---
        if (currentTotalWeight < maxWeightCapacity)
        {
            foreach (var item in vision.visibleItems)
            {
                if (item.tag == "Resources")
                {
                    SetIntent(item.obj.transform.position, item.obj, false); 
                    return true;
                }
            }
        }

        return false;
    }

    // Helper to run away
    void RunAwayFrom(TribeMember enemy)
    {
        Vector3 runDir = (transform.position - enemy.transform.position).normalized;
        Vector3 runToPos = transform.position + (runDir * 15f);
        SetIntent(runToPos, null, false); // Use Task 1 to run
    }


    void LookAroundRandomly()
{
    Vector3 randomDir = transform.position + (Random.insideUnitSphere * 5f);
    randomDir.y = transform.position.y;
    
    // TRUE means "This is an AI Command" (Player didn't click this)
    SetIntent(randomDir, null, false); 
}

    void StopMovement()
    {
        isMoving = false;
        targetResource = null;
        currentTask = 0;
        if (anim != null) anim.SetFloat("Speed", 0f);
    }
}