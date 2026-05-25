using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct VisibleObject
{
    public GameObject obj;
    public string tag;
    public float distance;
}

public class TribeVisionSensor : MonoBehaviour
{
    [Header("Vision Shape")]
    public Transform headTransform;
    public Vector3 eyeRotationOffset; 
    public float viewRadius = 300f;
    [Range(0, 180)] public float viewAngle = 120f;

    [Header("Layers")]
    public LayerMask targetLayer;   
    public LayerMask obstacleLayer; 

    [Header("Output")]
    public List<VisibleObject> visibleItems = new List<VisibleObject>();
    private Collider[] results = new Collider[20]; 
    
    public Vector3 GetEyeForward()
    {
        return headTransform.rotation * Quaternion.Euler(eyeRotationOffset) * Vector3.forward;
    }

    void Start()
    {
        InvokeRepeating("UpdateVisionList", 0f, 0.2f);
    }

   void UpdateVisionList()
    {
        visibleItems.Clear();
        if (headTransform == null) return;

        Vector3 eyeForward = GetEyeForward();
        // Use the buffer to avoid Garbage Collection
        int count = Physics.OverlapSphereNonAlloc(headTransform.position, viewRadius, results, targetLayer);

        for (int i = 0; i < count; i++)
        {
            if (results[i] == null) continue;
            GameObject foundObj = results[i].gameObject;

            // 1. Don't see yourself
            if (foundObj == gameObject) continue; 
            
            // 2. Terrain check (Optional, but safe)
            if (foundObj.name.Contains("Terrain")) continue;

            // --- FIX: AIM AT CENTER, NOT FEET ---
            // We assume units are about 2m tall, so we look 1m up.
            // For resources (which might be small), you might want a smaller offset or check the collider bounds center.
            Vector3 targetCenter = foundObj.transform.position + Vector3.up * 1.0f; 
            
            Vector3 dirToTarget = (targetCenter - headTransform.position).normalized;
            float dist = Vector3.Distance(headTransform.position, targetCenter);

            // 3. Angle Check
            if (Vector3.Angle(eyeForward, dirToTarget) < viewAngle / 2)
            {
                // 4. Raycast Check (The Obstacle Check)
                if (!Physics.Raycast(headTransform.position, dirToTarget, dist, obstacleLayer))
                {
                    // SUCCESS: We can see it!
                    VisibleObject item = new VisibleObject {
                        obj = foundObj,
                        tag = foundObj.tag,
                        distance = dist
                    };
                    visibleItems.Add(item);

                    // DEBUG: Green line = I see you
                    Debug.DrawLine(headTransform.position, targetCenter, Color.green, 0.2f);
                }
                else
                {
                    // FAILURE: Something is blocking the view
                    // DEBUG: Red line = Blocked
                    Debug.DrawLine(headTransform.position, targetCenter, Color.red, 0.2f);
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        if (headTransform == null) return;

        Vector3 eyeForward = GetEyeForward();
        
        Gizmos.color = Color.white;
        Quaternion leftRot = Quaternion.AngleAxis(-viewAngle / 2, headTransform.up);
        Quaternion rightRot = Quaternion.AngleAxis(viewAngle / 2, headTransform.up);
        
        Gizmos.DrawRay(headTransform.position, leftRot * eyeForward * viewRadius);
        Gizmos.DrawRay(headTransform.position, rightRot * eyeForward * viewRadius);
        Gizmos.DrawLine(headTransform.position + (leftRot * eyeForward * viewRadius), 
                        headTransform.position + (rightRot * eyeForward * viewRadius));

        Gizmos.color = Color.cyan;
        foreach (var item in visibleItems)
        {
            if (item.obj != null) Gizmos.DrawLine(headTransform.position, item.obj.transform.position);
        }
    }
}