using UnityEngine;
using Fusion;
using System.Collections;

public class TwinController : NetworkBehaviour
{
    [Header("Ardity Setup")]
    public SerialController serialController; // Drag Ardity Prefab here

    // This is the variable SafetyGameManager reads
    [Networked] public float NetKnobValue { get; set; }

    // --- READING FROM ARDUINO (Knob) ---
    void Update()
    {
        // Only the Host reads from Serial and updates the Network var
        if (Object != null && Object.HasStateAuthority && serialController != null)
        {
            string message = serialController.ReadSerialMessage();

            if (message == null)
                return;

            // Check if message is valid number
            if (float.TryParse(message, out float parsedValue))
            {
                NetKnobValue = parsedValue;
            }
        }
    }

    // --- SENDING TO ARDUINO (LEDs) ---
    // This is the function calling the error. It is now fixed.
    public void SendLedCommand(string message)
    {
        if (serialController != null)
        {
            serialController.SendSerialMessage(message);
            Debug.Log($"Sent to Arduino: {message}");
        }
        else
        {
            Debug.LogWarning("Serial Controller is not assigned in TwinController!");
        }
    }
}