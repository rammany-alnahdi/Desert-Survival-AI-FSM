using UnityEngine;

public partial class TribeMember
{
    /*
        // Call this when Distance to target is > 30f, or if the unit gets stuck
    private Vector3 FindBestLandmark(Vector3 finalTarget)
    {
        // 1. The Big Scan (e.g., 100 meter radius)
        // Make sure you created a Layer called "NavLandmark" in Unity!
        int landmarkLayer = LayerMask.GetMask("NavLandmark");
        Collider[] landmarks = Physics.OverlapSphere(transform.position, 100f, landmarkLayer);

        Collider bestLandmark = null;
        float bestScore = Mathf.Infinity;

        // 2. Loop through everything we found
        foreach (Collider col in landmarks)
        {
            // Filter: Only care about objects tagged with "enter"
            if (col.CompareTag("enter"))
            {
                // --- GETTING POSITION AND SIZE ---
                // col.bounds.center gives you the exact middle of the 3D box
                // col.bounds.size gives you the Width(x), Height(y), and Depth(z)
                Vector3 landmarkPos = col.bounds.center; 

                // 3. The Math: Distance from Me -> Door -> Target
                float distToDoor = Vector3.Distance(transform.position, landmarkPos);
                float distDoorToTarget = Vector3.Distance(landmarkPos, finalTarget);
                
                // Total travel distance
                float totalScore = distToDoor + distDoorToTarget;

                // 4. Save the winner
                if (totalScore < bestScore)
                {
                    bestScore = totalScore;
                    bestLandmark = col;
                }
            }
        }

        // 5. Return the result
        if (bestLandmark != null)
        {
            // Return the exact center of the door/gate!
            // We flatten the Y axis so the unit doesn't try to fly up into the air
            Vector3 targetGate = bestLandmark.bounds.center;
            targetGate.y = transform.position.y; 
            
            Debug.Log($"{name} found a gate! Rerouting...");
            return targetGate;
        }

        // Failsafe: If no doors are found, just walk straight to the target
        return finalTarget; 
    }
    */
    private Collider currentObstacle; // Remembers what we are currently dodging
   // UPDATE: Now accepts the path we are currently walking (intendedDest)
    public void CheckObstacleAvoidance(Collider obstacleCollider, Vector3 intendedDest)
    {
        // If we are already super close to our final target, just ignore the obstacle
        if (Vector3.Distance(transform.position, targetPosition) < 2.5f) return;

        // Use intendedDest (which might be the dodge point) instead of targetPosition
        Vector3 meToTarget = intendedDest - transform.position;
        Vector3 meToObstacle = obstacleCollider.transform.position - transform.position;

        float distToTarget = meToTarget.magnitude;
        float distToObstacle = meToObstacle.magnitude;

        if (distToObstacle > distToTarget) return; 

        float angle = Vector3.Angle(meToTarget, meToObstacle);
        if (angle > 20f) return; 

        // CALCULATE POINTS (Left vs Right)
        float objectSize = obstacleCollider.bounds.extents.magnitude;
        float bypassDistance = objectSize + 1.0f; 

        Vector3 rightDir = Vector3.Cross(Vector3.up, meToTarget.normalized).normalized;
        Vector3 leftDir = -rightDir;

        Vector3 pointRight = obstacleCollider.transform.position + (rightDir * bypassDistance);
        Vector3 pointLeft = obstacleCollider.transform.position + (leftDir * bypassDistance);

        pointRight.y = transform.position.y;
        pointLeft.y = transform.position.y;

        // "WALL OF BUILDINGS" CHECK
        int obstacleMask = LayerMask.GetMask("HideableObject", "VisionSource");
        float checkRadius = 0.5f; 

        int attempts = 0;
        while (Physics.CheckSphere(pointRight, checkRadius, obstacleMask) && attempts < 3)
        {
            pointRight += rightDir * 2.0f; 
            attempts++;
        }

        attempts = 0;
        while (Physics.CheckSphere(pointLeft, checkRadius, obstacleMask) && attempts < 3)
        {
            pointLeft += leftDir * 2.0f; 
            attempts++;
        }

        // WHICH IS FASTER? (Always compare against the FINAL targetPosition here)
        float distRightToTarget = Vector3.Distance(pointRight, targetPosition);
        float distLeftToTarget = Vector3.Distance(pointLeft, targetPosition);

        if (distRightToTarget < distLeftToTarget)
        {
            avoidanceTarget = pointRight;
        }
        else
        {
            avoidanceTarget = pointLeft;
        }

        // LOCK IN THE NEW DODGE
        isAvoidingObstacle = true;
        currentObstacle = obstacleCollider; // <--- NEW: Remember what we are dodging!
    }

    // UPDATE: Now takes the intended destination and keeps scanning
    private void ProcessVisionForObstacles(Vector3 intendedDest)
    {
        int hideableLayerID = LayerMask.NameToLayer("HideableObject");
        int VisionSourceID = LayerMask.NameToLayer("VisionSource");
        
        // ONLY return if animating. We DO NOT return if avoiding anymore!
        if (isanimationPosition) return;

        TribeVisionSensor visionSensor = GetComponentInChildren<TribeVisionSensor>();
        if (visionSensor == null || visionSensor.visibleItems.Count == 0) return;

        foreach (VisibleObject item in visionSensor.visibleItems)
        {
            if (item.obj == null) continue;
            if (item.obj == targetObject) continue;

            if (item.obj.layer == hideableLayerID || item.obj.layer == VisionSourceID)
            {
                Collider col = item.obj.GetComponent<Collider>();
                if (col != null)
                {
                    // NEW: Ignore the object we are ALREADY dodging to prevent jitter
                    if (isAvoidingObstacle && col == currentObstacle) continue;

                    // Run the math against our current path!
                    CheckObstacleAvoidance(col, intendedDest);
                    
                    if (isAvoidingObstacle && col == currentObstacle) 
                    {
                        break; // We found a new object and recalculated, stop the loop.
                    }
                }
            }
        }
    }
}