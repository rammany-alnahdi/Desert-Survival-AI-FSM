using UnityEngine;

public class HideableObject : MonoBehaviour
{
    private Renderer myRenderer;
    private int viewersCount = 0;
    
    // We store the layer number as an integer for maximum speed
    private int visionLayer;

    void Start()
    {
        myRenderer = GetComponent<Renderer>();
        visionLayer = LayerMask.NameToLayer("VisionSource");
        
        // Start hidden
        if(myRenderer != null) myRenderer.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the thing entering the bubble is on the VisionSource layer
        if (other.gameObject.layer == visionLayer)
        {
            viewersCount++;
            myRenderer.enabled = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == visionLayer)
        {
            viewersCount--;
            
            if (viewersCount <= 0)
            {
                viewersCount = 0;
                myRenderer.enabled = false;
            }
        }
    }
}