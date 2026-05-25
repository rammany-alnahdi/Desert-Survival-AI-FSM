using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public partial class TribeMember : MonoBehaviour
{
    // -----------------------------------------------------------------------------------
    // VARIABLES
    // -----------------------------------------------------------------------------------
    [Header("AI State")]
    public bool isPlayerCommanded = false; 
    public bool iAi = false; 
    public enum UnitType { Herder, Warrior, Scout, Merchant }
    private float scanTimer = 0f;
    private float idleScanDelay = 0;

    [Header("Survival & Experience")]
    public float health = 100f;
    public float hydration = 100f;
    public bool isDead = false;
    public int experiencePoints = 0, level = 1;

    [Header("Current Job")]
    public int maxWeightCapacity;
    public float attackPower = 20f, defense, waterDrainRate, baseMoveSpeed;
    [SerializeField] public UnitType _currentJobType = UnitType.Herder;
    public UnitType currentJobType 
    { 
        get => _currentJobType; 
        set 
        {
            if (_currentJobType != value)
            {
                _currentJobType = value;
                AssignJob(value); 
            }
        }
    }

    [Header("Weapon System")]
    public WeaponData equippedWeapon;
    public Transform weaponHandAnchorright;
    public Transform weaponHandAnchorleft;
    private GameObject currentWeaponObject;
    private Collider weaponCollider;
    private bool isEquippingWeapon = false;

    [Header("Weapon Status")]
    public bool needsWeapon = false;
    public bool hasRequestedWeapon = false;
    public bool needToReturnWeapon = false;   
    private float weaponCheckTimer = 0f;

    [Header("Task & Movement")]
    private GameObject targetObject; 
    public GameObject selectionIndicator;
    private float currentMoveSpeed, actionCooldown = 0f;
    public int currentTotalWeight = 0, currentTask = 0; 
    public float actionRate = 1.2f;
    private bool isMoving = false;

    [Header("Combat Visuals")]
    public float punchAngleOffset = -30f; 
    private bool isMidSwing = false;
    private bool hasHitThisSwing = false;
    public enum CombatStyle { Unarmed, Melee, Ranged, Spear, Axe }
    public CombatStyle currentCombatStyle = CombatStyle.Unarmed;

    [Header("Combat Rotation")]
    private float currentTwistAngle = 0f; // The active twist amount

    public Vector3 targetPosition;
    private ResourceNode targetResource;
    public Dictionary<ResourceNode.ResourceType, int> inventory = new Dictionary<ResourceNode.ResourceType, int>();
    private Animator anim;

    // --- AVOIDANCE VARIABLES ---
    private bool isAvoidingObstacle = false;
    private Vector3 avoidanceTarget;
    // -----------------------------------------------------------------------------------
    // INITIALIZATION & LOOP
    // -----------------------------------------------------------------------------------
    void Start() 
    {
        AssignJob(currentJobType);
        equippedWeapon = null; 
        anim = GetComponentInChildren<Animator>(); 
    }

    void Update()
    {
        if (isDead) return;

        // Safety Check: abort if target vanished
        if (targetObject != null && targetObject.Equals(null))
        {
            Debug.Log($"{name} target was destroyed, aborting");
            SetTask(0);
            return;
        }

        // 1. Run Sensors
        HandleSensorsAndSurvival();
        UpdateMovementSpeed();

        // 2. Warrior Logic (Check for weapons occasionally)
        if (currentJobType == UnitType.Warrior)
        {
            weaponCheckTimer += Time.deltaTime;
            if (weaponCheckTimer >= 10f) 
            {
                CheckWeaponNeeds();
                weaponCheckTimer = 0f;
            }
        }
        actionCooldown += Time.deltaTime;
        // 3. MAIN STATE MACHINE
        // Prioritize Combat (needs frame-by-frame updates for rotation)
        if (currentTask == 5) 
        {
            HandleCombatLogic(); 
        }
        else if (isMoving) 
        {
            HandleMovement();
        }
        else if (currentTask != 0)
        {
            // For gathering/drinking, we use the cooldown
            
            HandleActionController();
        }

        UpdateAnimatorParameters();
        if (Time.frameCount % 2 == 0) SnapToGround();
    }

    // -----------------------------------------------------------------------------------
    // THE BRAIN (Intent & Tasks)
    // -----------------------------------------------------------------------------------
    public void SetIntent(Vector3 pos, GameObject target, bool manual)
    {
        if (isDead) return;

        isPlayerCommanded = manual;
        iAi = !manual;
        targetPosition = pos;
        targetObject = target; 
        targetResource = target?.GetComponent<ResourceNode>();
        actionCooldown = 0;

        // --- NEW CHECK ---
        float dist = Vector3.Distance(transform.position, pos);
        bool isEnemy = target != null && target.GetComponent<TribeMember>() != null;

        // If it is an enemy AND we are already close (< 2.0m)
        if (isEnemy && dist < 2.0f)
        {
            targetEnemy = target.GetComponent<TribeMember>();
            SetTask(5); // Start fighting immediately!
        }
        else
        {
            SetTask(1); // Too far, need to Travel first
        }
    }

    public void SetTask(int newTask)
    {
        if (isDead) return;
        currentTask = newTask;
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                // 1. SNAP the agent back to our body
                agent.Warp(transform.position); 
                
                // 2. TURN IT BACK ON
                agent.updatePosition = true;
                agent.updateRotation = true;
                agent.isStopped = false;
            }

        switch (newTask)
        {
            case 0: // IDLE
                isMoving = false;
                targetResource = null;
                break;
                
            case 1: // TRAVELING
                isMoving = true;
                break;
                
            case 2: // GATHERING
                isMoving = false;
                break;
                
            case 3: // DEPOSITING
                isMoving = false;
                break;

            case 4: // DRINKING
                isMoving = false; 
                break;

            case 5: // COMBAT
                isMoving = false; // HandleCombatLogic takes over movement
                break;
            case 6: // TAMING    <-------- NEW!
                isMoving = false; 
                break;
        }

        if (isPlayerCommanded) scanTimer = 0;
        UpdateAnimatorParameters();
    }

    // -----------------------------------------------------------------------------------
    // MOVEMENT & ARRIVAL
    // -----------------------------------------------------------------------------------
    void HandleMovement()
    {
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) 
        {
            agent.updateRotation = false;
            agent.isStopped = true; 
        }
        if (targetObject != null && currentTask == 1) 
        {
            targetPosition = targetObject.transform.position;
            targetPosition.y = transform.position.y; // Keep it flattened so we don't float
        }
        // 1. Figure out where we are trying to go right now
        Vector3 currentDest = isAvoidingObstacle ? avoidanceTarget : targetPosition;

        // 2. Scan for obstacles ON THAT PATH
        ProcessVisionForObstacles(currentDest);

        // 3. Update currentDest in case ProcessVisionForObstacles just found a NEW tree
        if (isAvoidingObstacle)
        {
            currentDest = avoidanceTarget; 

            float dodgeDist = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), 
                                               new Vector3(avoidanceTarget.x, 0, avoidanceTarget.z));
            
            if (dodgeDist < 0.5f)
            {
                isAvoidingObstacle = false; 
                currentObstacle = null; // Clear the memory
            }
        }

        // 4. CALCULATE & APPLY MOVE
        Vector3 moveDir = (currentDest - transform.position).normalized;
        Vector3 separation = GetSeparationForce(); 
        Vector3 finalMove = (moveDir + separation).normalized;

        transform.position += finalMove * currentMoveSpeed * Time.deltaTime;
        LookAt(currentDest);

        // 5. CHECK ARRIVAL
        
        if (!isAvoidingObstacle)
        {
            float distance = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                                          new Vector3(targetPosition.x, 0, targetPosition.z));
            if (distance < 2f)
            {
                // ARRIVAL LOGIC
                if (targetObject != null && targetObject.GetComponent<TribeMember>() != null && targetObject.CompareTag("Enemy"))
                {
                    TribeMember enemy = targetObject.GetComponent<TribeMember>();
                    if (enemy != null && !enemy.isDead)
                    {
                        targetEnemy = enemy;          
                        SetTask(5); // Switch to Combat                  
                        Debug.Log($"{name} engaged {enemy.name}");
                    }
                    else SetTask(0);
                }
                else if (targetObject != null && targetObject.GetComponent<Camel>() != null)
                {
                    SetTask(6); // Switch to Taming!
                    Debug.Log($"{name} reached the camel and is starting to tame it!");
                }
                // --- NEW: HAWK ARRIVAL ---
                else if (targetObject != null && targetObject.GetComponent<Hawk>() != null)
                {
                    SetTask(6); // Switch to Taming!
                    Debug.Log($"{name} reached the hawk and is starting to tame it!");
                }

                else if (targetResource != null)
                {
                    if (targetResource.type == ResourceNode.ResourceType.water) SetTask(4);
                    else SetTask(2);
                }
                else if (targetObject != null && targetObject.CompareTag("Base"))
                {
                    BaseStation baseStation = targetObject.GetComponent<BaseStation>();

                    // A. RETURN WEAPON
                    if (needToReturnWeapon && baseStation != null && HasValidWeapon())
                    {
                        baseStation.ReturnWeaponToStorage(equippedWeapon);
                        UnequipWeapon();                    
                        needToReturnWeapon = false;
                        hasRequestedWeapon = false;
                        Debug.Log($"{name} returned weapon");
                    }
                    // B. GET WEAPON (Warrior Fix)
                    else if (needsWeapon && !HasValidWeapon())
                    {
                        bool gotWeapon = baseStation.TryEquipWarrior(this);
                        if (gotWeapon)
                        {
                            needsWeapon = false;
                            hasRequestedWeapon = false;
                            SetTask(0);   
                            return; // EXIT HERE so we don't try to deposit
                        }
                        else
                        {
                            Debug.Log($"{name} couldn't get weapon, will try again later");
                            StartCoroutine(TryWeaponAgainLater());
                            SetTask(0); // Go Idle
                            return; // EXIT HERE so we don't try to deposit
                        }
                    }

                    // C. DEPOSIT RESOURCES
                    if (baseStation != null && baseStation.depositPoint != null)
                    {
                        targetPosition = baseStation.depositPoint.position;
                    }
                    SetTask(3);   
                }
                else
                {
                    SetTask(0); // Arrived at ground position
                }
            }
        }
    }

    // -----------------------------------------------------------------------------------
    // ACTION CONTROLLER (Gather, Drink, etc.)
    // -----------------------------------------------------------------------------------
    void HandleActionController()
    {
        if (targetResource != null) LookAt(targetResource.transform.position);
        else if (targetObject != null) LookAt(targetObject.transform.position); // Added so they look at the camel!
        if (targetResource != null) LookAt(targetResource.transform.position);
        
        if (actionCooldown >= actionRate) 
        { 
            // Combat (Task 5) is handled in Update now!
            
            actionCooldown = 0; 
            
            if (currentTask == 2) GatherTask(); 
            else if (currentTask == 3) DepositTask(); 
            else if (currentTask == 4) DrinkTask(); 
            // --- NEW: TAMING HOOK ---
            else if (currentTask == 6) HandleTamingTask();
        }
    }

    // -----------------------------------------------------------------------------------
    // HELPERS
    // -----------------------------------------------------------------------------------
    void LookAt(Vector3 pos) 
    { 
        Vector3 dir = (pos - transform.position).normalized; 
        dir.y = 0; 
        if (dir != Vector3.zero) 
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f); 
    }

    void SnapToGround() 
    { 
        if (Physics.Raycast(transform.position + Vector3.up * 10, Vector3.down, out RaycastHit hit, 20, LayerMask.GetMask("Ground"))) 
        {
            float heightOffset = 1.0f; // Adjusted to 0 for standard pivots
            transform.position = new Vector3(transform.position.x, hit.point.y + heightOffset, transform.position.z);
        }
    }

    Vector3 GetSeparationForce()
    {
        Vector3 force = Vector3.zero;
        int neighbors = 0;
        int layerMask = LayerMask.GetMask("Water"); 
        
        Collider[] hits = Physics.OverlapSphere(transform.position, 0.4f, layerMask);
        
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            // Optimization: Check Tag first
            
                Vector3 pushDir = transform.position - hit.transform.position;
                force += pushDir.normalized; 
                neighbors++;
            
        }

        if (neighbors > 0) return (force / neighbors) * 1.5f; 
        return Vector3.zero;
    }

    public bool HasValidWeapon()
    {
        return equippedWeapon != null && !string.IsNullOrEmpty(equippedWeapon.weaponName);
    }
    
    private System.Collections.IEnumerator TryWeaponAgainLater()
    {
        yield return new WaitForSeconds(30f); 
        if (needsWeapon && !hasRequestedWeapon)
        {
            // Assuming you have a GoGetWeapon logic or similar
            // GoGetWeapon(); 
        }
    }
}