using UnityEngine;
public partial class TribeMember
{
    // -----------------------------------------------------------------------------------
   
    private void HandleTamingTask()
    {
        if (targetObject == null) 
        {
            SetTask(0); 
            return;
        }

        // Route to the correct taming function based on the animal
        if (targetObject.GetComponent<Camel>() != null)
        {
            HandleTamingTaskCamel();
        }
        else if (targetObject.GetComponent<Hawk>() != null)
        {
            HandleTamingTaskHawk();
        }
        else
        {
            SetTask(0); // Failsafe if it's not an animal
        }
    }
    public void SetSelected(bool status) 
    { 
        if (selectionIndicator) 
        {
            selectionIndicator.SetActive(status);
        }

        // If we deselect the unit, we "release" it back to the AI (optional)
        if (status == false) 
        {
            isPlayerCommanded = false;
            iAi = true;
        }
    }
  
}
