using UnityEngine;
using Fusion; // We now use Fusion
using System.Collections;

// Change 1: Inherit from NetworkBehaviour, not MonoBehaviour
public class TwinController : NetworkBehaviour
{
    [Header("Hardware Link")]
    public SerialController serialController;

    [Header("Game Logic")]
    public float targetValue = 512; 
    public float tolerance = 50; 
    
    [Header("Visuals")]
    public Transform virtualNeedle; 

    // Change 2: The Magic "Cloud Variable"
    // This variable automatically copies itself from Host to Clients.
    [Networked] public float NetKnobValue { get; set; }

    // Local variable (Hardware only knows this)
    private float hardwareRawValue = 0;

    // Change 3: FixedUpdateNetwork is the new "Update" for multiplayer
    public override void FixedUpdateNetwork()
    {
        // LOGIC FOR THE HOST (The one with the Arduino)
        if (Object.HasStateAuthority) 
        {
            // Take the raw hardware value and put it into the Cloud Variable
            NetKnobValue = hardwareRawValue;
        }

        // LOGIC FOR EVERYONE (Host + Clients)
        // We ALL update the visual needle based on the Cloud Variable
        AnimateNeedle();
    }

    void AnimateNeedle()
    {
        // Smoothly move needle based on NET_KNOB_VALUE (not hardware value)
        // This ensures Clients see exactly what Host sees
        float angle = Mathf.InverseLerp(0, 1023, NetKnobValue); 
        float finalRotation = Mathf.Lerp(-90, 90, angle);
        
        if (virtualNeedle != null)
            virtualNeedle.localRotation = Quaternion.Euler(0, 0, -finalRotation);
    }

    void Update()
    {
        // KEYBOARD INPUT (Only Host can control LEDs)
        if (Object != null && Object.HasStateAuthority)
        {
            if (Input.GetKeyDown(KeyCode.Space)) CheckAlignment();
        }
    }

    void CheckAlignment()
    {
        // Compare Cloud Value vs Target
        float difference = Mathf.Abs(NetKnobValue - targetValue);

        if (difference <= tolerance)
        {
            Debug.Log("Synced!");
            serialController.SendSerialMessage("G"); 
        }
        else
        {
            Debug.Log("Failed!");
            serialController.SendSerialMessage("R"); 
        }
    }

    // ARDITY LISTENER (Only runs on Host PC)
    void OnMessageArrived(string msg)
    {
        // Only the Host listens to Serial Port
        if (float.TryParse(msg, out float result))
        {
            hardwareRawValue = result;
        }
    }
    void OnConnectionEvent(bool success)
    {
        Debug.Log(success ? "ARDUINO HARDWARE CONNECTED" : "ARDUINO CONNECTION FAILED");
    }
}