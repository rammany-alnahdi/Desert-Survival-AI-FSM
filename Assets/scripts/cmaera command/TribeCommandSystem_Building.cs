using UnityEngine;

public partial class TribeCommandSystem : MonoBehaviour
{
    [Header("Building System")]
    public GameObject currentGhostPrefab; // The transparent hologram
    public GameObject currentRealPrefab;  // The actual building
    private GameObject activeGhost;
    private bool isBuildMode = false;

    // Call this via a UI Button or a Keyboard shortcut (e.g., 'B' for Build)
    public void StartBuildingMode(GameObject ghostPrefab=null, GameObject realPrefab=null)
    {
        if (selectedUnits.Count == 0) 
        {
            Debug.Log("Select a worker first!");
            return;
        }

        isBuildMode = true;
        if (ghostPrefab != null) 
        {
            currentGhostPrefab = ghostPrefab;
            currentRealPrefab = realPrefab;
        }

        // Spawn the ghost at the mouse cursor
        activeGhost = Instantiate(currentGhostPrefab);
    }

    private void HandleBuildingPlacement()
    {
        if (!isBuildMode || activeGhost == null) return;

        // 1. Raycast from mouse to the Ground layer
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, LayerMask.GetMask("Ground")))
        {
            // Move ghost to the mouse position
            activeGhost.transform.position = hit.point;

            // Optional: Snap to grid
            // float gridSize = 2f;
            // activeGhost.transform.position = new Vector3(
            //     Mathf.Round(hit.point.x / gridSize) * gridSize,
            //     hit.point.y,
            //     Mathf.Round(hit.point.z / gridSize) * gridSize
            // );

            // 2. Left Click to Place
            if (Input.GetMouseButtonDown(0))
            {
                PlaceBuilding(activeGhost.transform.position);
            }
        }

        // 3. Right Click or Escape to Cancel
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelBuildingMode();
        }
    }

    private void PlaceBuilding(Vector3 position)
    {
        // Destroy the hologram and spawn the real building
        Destroy(activeGhost);
        Instantiate(currentRealPrefab, position, Quaternion.identity);

        isBuildMode = false;

        // --- COMMAND THE WORKERS ---
        // Tell all selected units to walk to the new building and construct it!
        foreach (TribeMember unit in selectedUnits)
        {
            // unit.SetTask(???); -> We will need to add a "Build" task to your TribeMember!
            unit.targetPosition = position;
        }
    }

    private void CancelBuildingMode()
    {
        isBuildMode = false;
        if (activeGhost != null) Destroy(activeGhost);
    }
}