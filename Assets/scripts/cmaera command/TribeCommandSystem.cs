using UnityEngine;
using System.Collections.Generic;

public partial  class TribeCommandSystem : MonoBehaviour
{
    public List<TribeMember> selectedUnits = new List<TribeMember>();
    private Vector3 startMousePos;
    private bool isDragging = false;
    private Texture2D selectionTexture;
    private Vector3 mousePosAtClick;
    private float lastClickTime;
    private const float doubleClickThreshold = 0.3f;
    public float formationSpacing = 2.0f; // Distance between soldiers

    void Start()
    {
        selectionTexture = new Texture2D(1, 1);
        selectionTexture.SetPixel(0, 0, new Color(0, 1, 0, 0.2f));
        selectionTexture.Apply();
    }

    void Update()
    {
            // NEW: If we are placing a building, do that and ignore standard clicking
        if (isBuildMode)
        {
            HandleBuildingPlacement();
            return; 
        }
        if (Input.GetMouseButtonDown(0)) { startMousePos = Input.mousePosition; isDragging = true; }
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            if (Vector3.Distance(startMousePos, Input.mousePosition) > 10f) SelectUnitsInBox();
            else HandleClickSelection();
        }

        if (Input.GetMouseButtonDown(1)) mousePosAtClick = Input.mousePosition;
        if (Input.GetMouseButtonUp(1) && selectedUnits.Count > 0)
        {
            if (Vector3.Distance(mousePosAtClick, Input.mousePosition) < 5f) GiveMoveOrder();
        }

        HandleUnitControls();
    }


    void HandleClickSelection()
{
    float timeSinceLastClick = Time.time - lastClickTime;
    lastClickTime = Time.time;
    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

    if (Physics.Raycast(ray, out RaycastHit hit, 1000f, LayerMask.GetMask("VisionSource")))
    {
        TribeMember unit = hit.collider.GetComponentInParent<TribeMember>();
        
        if (unit != null && unit.CompareTag("Player"))
        {
            if (timeSinceLastClick <= doubleClickThreshold) 
                SelectNearbySameType();
            else
            {
                if (!Input.GetKey(KeyCode.LeftControl)) 
                    DeselectAll();
                if (!selectedUnits.Contains(unit)) 
                { 
                    unit.SetSelected(true); 
                    selectedUnits.Add(unit); 
                }
            }
        }
    }
    else if (!Input.GetKey(KeyCode.LeftControl)) 
        DeselectAll();
}

void SelectNearbySameType()
{
    foreach (TribeMember unit in Object.FindObjectsByType<TribeMember>(FindObjectsSortMode.None))
    {
        
        if (unit.CompareTag("Player"))
        {
            Vector3 sPos = Camera.main.WorldToScreenPoint(unit.transform.position);
            if (sPos.z > 0 && sPos.x > 0 && sPos.x < Screen.width && sPos.y > 0 && sPos.y < Screen.height)
            {
                if (!selectedUnits.Contains(unit)) 
                { 
                    unit.SetSelected(true); 
                    selectedUnits.Add(unit); 
                }
            }
        }
    }
}

void SelectUnitsInBox()
{
    if (!Input.GetKey(KeyCode.LeftControl)) 
        DeselectAll();
    Rect rect = GetGUIRect(startMousePos, Input.mousePosition);
    foreach (TribeMember unit in Object.FindObjectsByType<TribeMember>(FindObjectsSortMode.None))
    {
        
        if (unit.CompareTag("Player"))
        {
            Vector3 sPos = Camera.main.WorldToScreenPoint(unit.transform.position);
            sPos.y = Screen.height - sPos.y;
            if (rect.Contains(sPos)) 
            { 
                unit.SetSelected(true); 
                selectedUnits.Add(unit); 
            }
        }
    }
}

    void DeselectAll() { foreach (var unit in selectedUnits) if (unit) unit.SetSelected(false); selectedUnits.Clear(); }
    void OnGUI() { if (isDragging) { GUI.color = new Color(0, 1, 0, 0.3f); GUI.DrawTexture(GetGUIRect(startMousePos, Input.mousePosition), selectionTexture); } }
    Rect GetGUIRect(Vector3 p1, Vector3 p2)
    {
        p1.y = Screen.height - p1.y; p2.y = Screen.height - p2.y;
        return Rect.MinMaxRect(Mathf.Min(p1.x, p2.x), Mathf.Min(p1.y, p2.y), Mathf.Max(p1.x, p2.x), Mathf.Max(p1.y, p2.y));
    }
}