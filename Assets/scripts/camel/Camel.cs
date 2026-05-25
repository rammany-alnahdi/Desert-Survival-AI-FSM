using UnityEngine;

public class Camel : MonoBehaviour
{
    [Header("Camel State")]
    public bool isWild = true; // True = Runs away. False = Domesticated/Carrying Tent
    public TribeMember myMaster; // <--- NEW: The specific Herder that owns this camel
    public float moveSpeed = 1.0f;
    public float health = 200f;

    [Header("Navigation")]
    public Vector3 targetPosition;
    public GameObject targetObject;
    public int currentTask = 0; // 0: Idle, 1: Wander, 2: Flee, 3: Go To Food, 4: Go To Base

    private TribeVisionSensor visionSensor;
    private float scanTimer = 0f;
    private float stuckTimer = 0f;
    private Vector3 lastDangerPosition;

    void Start()
    {
        visionSensor = GetComponentInChildren<TribeVisionSensor>();
        targetPosition = transform.position;
    }

    void Update()
    {
        SnapToGround();
        HandleBrain();
        
        if (currentTask != 0)
        {
            HandleMovement();
        }
    }

    private void SnapToGround()
    {
        Vector3 rayOrigin = new Vector3(transform.position.x, transform.position.y + 10f, transform.position.z);
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 20f, LayerMask.GetMask("Ground")))
        {
            float heightOffset = 0.5f; 
            transform.position = new Vector3(transform.position.x, hit.point.y + heightOffset, transform.position.z);
        }
    }

    // -----------------------------------------------------------------------
    // 1. THE BRAIN (Decisions based on state and vision)
    // -----------------------------------------------------------------------
    private void HandleBrain()
    {
        scanTimer += Time.deltaTime;
        if (scanTimer >= 0.3f) 
        {
            scanTimer = 0f;

            // --- 1. SHARED VISION (Both Wild and Tamed use their eyes) ---
            if (visionSensor != null && visionSensor.visibleItems.Count > 0)
            {
                foreach (var item in visionSensor.visibleItems)
                {
                    if (item.obj == null) continue;

                    // RULE 1: WILD CAMELS FLEE
                    if (isWild && item.obj.layer == LayerMask.NameToLayer("VisionSource"))
                    {
                        TribeMember human = item.obj.GetComponent<TribeMember>();
                        if (human != null && !human.isDead)
                        {
                            if (human.currentJobType == TribeMember.UnitType.Herder && human.currentTask == 6)
                                continue; 

                            // --- NEW: Remember where the danger was! ---
                            lastDangerPosition = item.obj.transform.position; 

                            Vector3 runDir = (transform.position - lastDangerPosition).normalized;
                            Vector3 safeSpot = transform.position + (runDir * 20f);
                            
                            SetIntent(safeSpot, null, 2); 
                            return; 
                        }
                    }

                    // RULE 2: EAT FOOD (Both Wild and Tamed)
                    // (Don't stop to eat if currently fleeing for your life)
                    if (currentTask != 2) 
                    {
                        ResourceNode resource = item.obj.GetComponent<ResourceNode>();
                        if (resource != null && resource.type == ResourceNode.ResourceType.CamelFood)
                        {
                            SetIntent(item.obj.transform.position, item.obj, 3); 
                            return; // Stop thinking, go eat!
                        }
                    }
                }
            }

            // --- 2. IDLE BEHAVIORS (If we didn't see anything urgent) ---
            if (isWild)
            {
                if (currentTask == 0) WanderRandomly();
            }
            else
            {
                HandleTamedBrain(); // Tamed herd logic
            }
        }
    }

    // --- TAMED HERD LOGIC ---
    // --- TAMED HERD LOGIC ---
    private void HandleTamedBrain()
    {
        // 1. Is my master missing, dead, or no longer a Herder?
        if (myMaster == null || myMaster.isDead || myMaster.currentJobType != TribeMember.UnitType.Herder)
        {
            if (currentTask != 4) // Task 4 = Go to Base
            {
                GameObject baseStation = GameObject.FindWithTag("Base");
                if (baseStation != null)
                {
                    SetIntent(baseStation.transform.position, baseStation, 4);
                    Debug.Log("My master is gone or changed jobs! Heading to base.");
                }
            }
            return;
        }

        // 2. We have a valid master! How far away are they?
        float distToMaster = Vector3.Distance(transform.position, myMaster.transform.position);

        // 3. Master is > 200f -> Go to Base
        if (distToMaster > 50f)
        {
            if (currentTask != 4) 
            {
                GameObject baseStation = GameObject.FindWithTag("Base");
                if (baseStation != null)
                {
                    SetIntent(baseStation.transform.position, baseStation, 4);
                    Debug.Log("Master is too far away! Camel heading to base.");
                }
            }
            return;
        }

        // 4. Master is <= 200f -> Stay within 50f radius of the EXACT Master
        if (currentTask == 0 || currentTask == 4)
        {
            WanderNear(myMaster.transform.position, 25f);
        }
        else if (currentTask == 1) // If already wandering
        {
            if (Vector3.Distance(targetPosition, myMaster.transform.position) > 25f)
            {
                WanderNear(myMaster.transform.position, 25f); // Reroute back to the master!
            }
        }
    }

    private void WanderNear(Vector3 center, float radius)
    {
        Vector2 randomCircle = Random.insideUnitCircle * radius;
        Vector3 randomSpot = center + new Vector3(randomCircle.x, 0, randomCircle.y);
        SetIntent(randomSpot, null, 1); 
    }

    // -----------------------------------------------------------------------
    // 2. SET INTENT 
    // -----------------------------------------------------------------------
    public void SetIntent(Vector3 pos, GameObject obj, int taskID)
    {
        targetPosition = pos;
        targetPosition.y = transform.position.y; 
        
        targetObject = obj;
        currentTask = taskID;
    }

    private void WanderRandomly()
    {
        Vector2 randomCircle = Random.insideUnitCircle.normalized * 10f;
        Vector3 randomSpot = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
        SetIntent(randomSpot, null, 1); 
    }

    // -----------------------------------------------------------------------
    // 3. MOVEMENT 
    // -----------------------------------------------------------------------
    private void HandleMovement()
    {
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) 
        {
            agent.updateRotation = false;
            agent.updatePosition = false;
            agent.isStopped = true; 
        }

        Vector3 moveDir = (targetPosition - transform.position).normalized;

        transform.position += moveDir * moveSpeed * Time.deltaTime;
        stuckTimer += Time.deltaTime;
        if (moveDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }

        float dist = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                                      new Vector3(targetPosition.x, 0, targetPosition.z));

        // Arrived successfully OR got stuck for too long!
        if (dist < 1.5f || stuckTimer > 5.0f) 
        {
            if (currentTask == 3 && targetObject != null && dist < 1.5f) 
            {
                ResourceNode foodNode = targetObject.GetComponent<ResourceNode>();
                if (foodNode != null) { foodNode.Gather(10); health += 1000; }
            }
            // --- NEW: THE LOOK BACK MECHANIC ---
            else if (currentTask == 2)
            {
                // We reached the safe spot! Instantly snap around to look at the danger
                Vector3 lookBackDir = (lastDangerPosition - transform.position).normalized;
                lookBackDir.y = 0; // Keep it flat
                
                if (lookBackDir != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(lookBackDir);
                    Debug.Log("Camel arrived and turned around to check if it's safe!");
                }
            }

            stuckTimer = 0f; 
            SetIntent(transform.position, null, 0); // Reset to Idle
        }
    }
}