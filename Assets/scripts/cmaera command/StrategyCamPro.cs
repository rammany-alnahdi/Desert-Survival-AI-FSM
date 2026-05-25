using UnityEngine;

public class TribeWarsGodView : MonoBehaviour
{
    [Header("Speeds")]
    public float moveSpeed = 50f;
    public float rotationSpeed = 300f; // Lowered slightly for better control
    public float zoomSpeed = 40f;
    public float smoothing = 5f; // New: How smooth the camera follows terrain

    [Header("Limits")]
    public float minHeight = 5f;
    public float maxHeight = 60f;
    public float minPitch = 20f;  // Changed: Prevents looking "up" too much
    public float maxPitch = 85f;

    private float targetZoom;
    private int groundLayer;
    private float currentPitch;
    private float currentYaw; // New: To keep horizontal rotation stable

    void Start()
    {
        groundLayer = LayerMask.GetMask("Ground");
        
        // Capture initial rotation
        currentPitch = transform.eulerAngles.x;
        currentYaw = transform.eulerAngles.y;
        targetZoom = transform.position.y;
    }

    void Update()
    {
        // 1. MOVEMENT (WASD)
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        
        Vector3 forward = transform.forward;
        forward.y = 0; 
        forward.Normalize();
        Vector3 right = transform.right;
        right.y = 0;
        right.Normalize();
        
        transform.position += (forward * v + right * h) * moveSpeed * Time.deltaTime;

        // 2. ROTATION (Right Click)
        if (Input.GetMouseButton(1))
        {
            currentYaw += Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            currentPitch -= Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime; // Negative to feel natural

            // Clamp vertical look
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
            
            transform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        }

        // 3. ZOOM (Mouse Wheel)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetZoom = Mathf.Clamp(targetZoom - (scroll * zoomSpeed), minHeight, maxHeight);
        }

        // 4. SMART GROUND FOLLOWING
        // We fire the ray from high above the current position
        Vector3 rayOrigin = new Vector3(transform.position.x, transform.position.y + 50f, transform.position.z);
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 500f, groundLayer))
        {
            float desiredHeight = hit.point.y + targetZoom;
            
            // Smoothly move the Y position so it doesn't "jitter" on bumpy terrain
            float smoothedY = Mathf.Lerp(transform.position.y, desiredHeight, Time.deltaTime * smoothing);
            transform.position = new Vector3(transform.position.x, smoothedY, transform.position.z);
        }
    }
}