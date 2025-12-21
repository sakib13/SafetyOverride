using UnityEngine;
using System.Collections;

public class TwinController : MonoBehaviour
{
    [Header("Hardware Link")]
    public SerialController serialController;

    [Header("Game Logic")]
    [Range(0, 1023)] public float targetValue = 512; // The "Correct" spot
    public float tolerance = 50; // How lenient the system is (Safety margin)
    
    [Header("Visuals")]
    public Transform virtualNeedle; 
    public Transform targetZoneVisual; // (Optional) To see where to stop

    // Data
    private float currentKnobValue = 0;
    
    void Update()
    {
        // 1. VISUALIZE NEEDLE
        // Map 0-1023 to -90 to 90 degrees
        float angle = Mathf.InverseLerp(0, 1023, currentKnobValue); 
        float finalRotation = Mathf.Lerp(-90, 90, angle);
        
        if (virtualNeedle != null)
            virtualNeedle.localRotation = Quaternion.Euler(0, 0, -finalRotation);

        // 2. SIMULATE THE LEVER (Spacebar = Pull Lever)
        if (Input.GetKeyDown(KeyCode.Space)) 
        {
            CheckAlignment();
        }
        
        // Debug: Reset the lights when we let go
        if (Input.GetKeyUp(KeyCode.Space))
        {
            serialController.SendSerialMessage("O"); // Off
        }
    }

    void CheckAlignment()
    {
        // THE MATH: Is the knob close to the target?
        float difference = Mathf.Abs(currentKnobValue - targetValue);

        if (difference <= tolerance)
        {
            Debug.Log("SUCCESS! System Synced.");
            serialController.SendSerialMessage("G"); // Green
        }
        else
        {
            Debug.Log($"FAILURE! Diff: {difference}. Too far!");
            serialController.SendSerialMessage("R"); // Red
        }
    }

    // Called by Ardity
    void OnMessageArrived(string msg)
    {
        if (float.TryParse(msg, out float result))
        {
            currentKnobValue = result;
        }
    }

    void OnConnectionEvent(bool success)
    {
        Debug.Log(success ? "ARDUINO CONNECTED" : "CONNECTION FAILED");
    }
}