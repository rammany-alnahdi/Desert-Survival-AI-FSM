using UnityEngine;

public class Hawk : MonoBehaviour
{
    [Header("Hawk State")]
    public bool isWild = true; 
    public TribeMember myMaster; // The Scout that tamed this hawk
    public float flySpeed = 12.0f;
    
    [Header("Orbit Settings")]
    public float orbitHeight = 20f;  // How high above the scout it flies
    public float orbitRadius = 15f;  // How wide the circle is
    private float orbitAngle = 0f;   // Used for the math

    [Header("Navigation")]
    public Vector3 targetPosition;
    public GameObject targetObject;
    public int currentTask = 0; // 0: Idle, 1: Wander, 2: Flee, 3: Eat, 4: Orbiting Master

    private TribeVisionSensor visionSensor;
    private float scanTimer = 0f;
    private Vector3 lastDangerPosition;

    void Start()
    {
        visionSensor = GetComponentInChildren<TribeVisionSensor>();
        targetPosition = transform.position;
    }

    void Update()
    {
        
        HandleBrain();
        
        if (currentTask != 0)
        {
            HandleMovement();
        }
    }

    // Notice: NO SnapToGround() function! We want it in the sky.

    // -----------------------------------------------------------------------
    // 1. THE BRAIN
    // -----------------------------------------------------------------------
    private void HandleBrain()
    {
        scanTimer += Time.deltaTime;
        if (scanTimer >= 0.2f) 
        {
            scanTimer = 0f;

            // --- 1. SHARED VISION ---
            if (visionSensor != null && visionSensor.visibleItems.Count > 0)
            {
                foreach (var item in visionSensor.visibleItems)
                {
                    if (item.obj == null) continue;

                    // RULE 1: WILD HAWKS FLEE FROM HUMANS
                    if (isWild && item.obj.layer == LayerMask.NameToLayer("VisionSource"))
                    {
                        TribeMember human = item.obj.GetComponent<TribeMember>();
                        if (human != null && !human.isDead)
                        {
                            // --- NEW: THE LURE MECHANIC ---
                            // Scouts are the only ones who can tame Hawks (Task 6)
                            if (human.currentJobType == TribeMember.UnitType.Scout && human.currentTask == 6)
                            {
                                // Fly down to the Scout's arm! (2 meters above their feet)
                                Vector3 armPosition = human.transform.position + new Vector3(0, 2f, 0);
                                
                                // Set Intent to Travel (Task 1) towards the Scout
                                SetIntent(armPosition, human.gameObject, 1); 
                                Debug.Log("Hawk sees the Scout's meat and is flying down!");
                                return; // Stop thinking, fly to the food!
                            }
                            lastDangerPosition = item.obj.transform.position;
                            // Otherwise, fly away AND UP into the sky to escape!
                            Vector3 runDir = (transform.position - item.obj.transform.position).normalized;
                            Vector3 safeSpot = transform.position + (runDir * 23f) + new Vector3(0, 15f, 0); 
                            
                            SetIntent(safeSpot, null, 2); 
                            return; 
                        }
                    }

                    // RULE 2: EAT MEAT (You will need to add "Meat" to your ResourceType enum!)
                    if (currentTask != 2) 
                    {
                        ResourceNode resource = item.obj.GetComponent<ResourceNode>();
                        // Check for Meat (Or whatever you want the bird to eat)
                        if (resource != null && resource.type == ResourceNode.ResourceType.Food) 
                        {
                            SetIntent(item.obj.transform.position, item.obj, 3); 
                            return; 
                        }
                    }
                }
            }

            // --- 2. IDLE BEHAVIORS ---
            if (isWild)
            {
                if (currentTask == 0) WanderInSky();
            }
            else
            {
                HandleTamedBrain(); 
            }
        }
    }
    private float GetGroundHeight(Vector3 position)
    {
        // Start the raycast from WAY up in the sky (e.g., Y = 1000)
        Vector3 rayOrigin = new Vector3(position.x, 1000f, position.z);
        
        // Cast downwards for a massive distance to guarantee we hit the terrain
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 2000f, LayerMask.GetMask("Ground")))
        {
            return hit.point.y;
        }
        
        // Fallback just in case it flies off the edge of the map
        return 0f; 
    }
    // --- TAMED ORBIT LOGIC ---
    private void HandleTamedBrain()
    {
        if (myMaster == null || myMaster.isDead || myMaster.currentJobType != TribeMember.UnitType.Scout)
        {
            // If the Scout dies or changes jobs, the Hawk flies away and becomes wild again!
            isWild = true;
            myMaster = null;
            this.gameObject.tag = "Untagged";
            Debug.Log("The Hawk's master is gone. It returned to the wild!");
            return;
        }

        // Tell the movement system to orbit!
        currentTask = 4; 
    }

    // -----------------------------------------------------------------------
    // 2. SET INTENT 
    // -----------------------------------------------------------------------
    public void SetIntent(Vector3 pos, GameObject obj, int taskID)
    {
        targetPosition = pos;
        targetObject = obj;
        currentTask = taskID;
    }

    private void WanderInSky()
    {
        // 1. Pick a random horizontal direction
        Vector2 randomCircle = Random.insideUnitCircle.normalized * 25f;
        Vector3 nextHorizontalPos = new Vector3(transform.position.x + randomCircle.x, 0, transform.position.z + randomCircle.y);

        // 2. Find the ground height at that NEW spot
        float groundAtTarget = GetGroundHeight(nextHorizontalPos);

        // 3. Pick a random altitude between 15m and 35m ABOVE that ground
        float randomFlyHeight = Random.Range(15f, 35f);
        
        // 4. Combine them into the final target
        Vector3 randomSpot = new Vector3(nextHorizontalPos.x, groundAtTarget + randomFlyHeight, nextHorizontalPos.z);
        
        SetIntent(randomSpot, null, 1); 
        Debug.Log($"Hawk wandering to new spot. Ground there is {groundAtTarget}m, flying at {randomFlyHeight}m above it.");
    }

    // -----------------------------------------------------------------------
    // 3. MOVEMENT 
    // -----------------------------------------------------------------------
    private void HandleMovement()
    {
        Vector3 currentDest = targetPosition;

        // --- THE MAGIC ORBIT MATH ---
        if (currentTask == 4 && myMaster != null)
        {
            // Increase the angle over time to make it circle
            orbitAngle += 1.5f * Time.deltaTime; 
            if (orbitAngle > Mathf.PI * 2) orbitAngle -= Mathf.PI * 2; // Reset loop

            // Calculate the exact 3D point floating above the Scout's head
            float offsetX = Mathf.Cos(orbitAngle) * orbitRadius;
            float offsetZ = Mathf.Sin(orbitAngle) * orbitRadius;
            
            currentDest = myMaster.transform.position + new Vector3(offsetX, orbitHeight, offsetZ);
        }
        // --- NEW: ALTITUDE CLAMPING IN EVERY MOVE ---
        float groundAtDest = GetGroundHeight(currentDest);
        float minAllowedY = groundAtDest + 1.0f;  // Don't go below 5m
        float maxAllowedY = groundAtDest + 40.0f; // Don't go above 40m
        
        // Force the destination to stay within these bounds
        currentDest.y = Mathf.Clamp(currentDest.y, minAllowedY, maxAllowedY);
        // --------------------------------------------

        Vector3 moveDir = (currentDest - transform.position).normalized;
        transform.position += moveDir * flySpeed * Time.deltaTime;
        if (moveDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            // Smooth banking/turning
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 3f);
        }


        // Arrival logic (Only if not orbiting, because orbiting never "arrives")
        if (currentTask != 4)
        {
            float dist = Vector3.Distance(transform.position, targetPosition); // 3D distance check!

            if (dist < 2.0f)
            {
                if (currentTask == 3 && targetObject != null)
                {
                    ResourceNode foodNode = targetObject.GetComponent<ResourceNode>();
                    if (foodNode != null) { foodNode.Gather(5); }
                }
                else if (currentTask == 2)
                {
                    // Calculate direction back to the danger
                    Vector3 lookBackDir = (lastDangerPosition - transform.position).normalized;
                    
                    if (lookBackDir != Vector3.zero)
                    {
                        // Snap rotation to face the last danger
                        transform.rotation = Quaternion.LookRotation(lookBackDir);
                        Debug.Log("Hawk reached safe altitude and is watching the human.");
                    }
                }
                SetIntent(transform.position, null, 0); 
            }
        }
    }
    private void OnDrawGizmos()
    {
        // Draw the current target
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(targetPosition, 1f);
        Gizmos.DrawLine(transform.position, targetPosition);

        // Draw the "Ground Check" ray
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, Vector3.down * 50f);

        // Draw the Look-Back target (from your camel logic)
        if (currentTask == 2) {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, lastDangerPosition);
        }
    }
}