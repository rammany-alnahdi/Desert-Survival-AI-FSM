using UnityEngine;

public class ResourceNode : MonoBehaviour
{
    public enum ResourceType { Wood, Stone, Food, water, CamelFood  }
    public ResourceType type;
    public int amount = 100;
    public int weightPerUnit = 2;
    
    // --- NEW VARIABLES ---
    private int maxAmount;
    private Renderer myRenderer;

    void Start()
    {
        maxAmount = amount; // Remember how much food we started with
        myRenderer = GetComponentInChildren<Renderer>();
    }
    
    public int Gather(int quantity)
    {
        int gathered = Mathf.Min(quantity, amount);
        amount -= gathered;
        
        // --- NEW: Update the visual look of the grass ---
        if (type == ResourceType.CamelFood)
        {
            UpdateVisuals();
        }
        
        if (amount <= 0)
        {
            Die(); // The patch is completely eaten/destroyed
        }
        
        return gathered; 
    }

    private void UpdateVisuals()
    {
        if (myRenderer != null)
        {
            // Calculate a percentage from 0.0 (empty) to 1.0 (full)
            float foodPercentage = (float)amount / maxAmount;
            
            // Define our colors
            Color lushGreen = new Color(0.2f, 0.8f, 0.2f); // Bright Green
            Color dryDirt = new Color(0.6f, 0.5f, 0.3f);   // Brown/Yellowish Sand
            
            // Lerp blends smoothly between the two colors based on the percentage
            myRenderer.material.color = Color.Lerp(dryDirt, lushGreen, foodPercentage);
        }
    }
    
    void Die() 
    { 
        Destroy(gameObject); 
    }
}