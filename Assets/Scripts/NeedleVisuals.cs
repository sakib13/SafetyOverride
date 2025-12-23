using UnityEngine;
using Fusion; // Need this to check if the network is ready

public class NeedleVisuals : MonoBehaviour
{
    void Update()
    {
        // 1. Find the Brain
        var controller = FindObjectOfType<TwinController>();
        
        // 2. THE FIX: Check "Is Valid?" before reading.
        // We only proceed if:
        // A. We found the controller
        // B. The controller has a Network Object
        // C. That Network Object is fully Spawned and Valid
        if (controller != null && controller.Object != null && controller.Object.IsValid)
        {
            // Now it is safe to read the variable
            float val = controller.NetKnobValue;
            
            // Do the Math
            float angle = Mathf.InverseLerp(0, 1023, val); 
            float finalRotation = Mathf.Lerp(-90, 90, angle);
            
            // Rotate
            transform.localRotation = Quaternion.Euler(0, 0, -finalRotation);
        }
    }
}