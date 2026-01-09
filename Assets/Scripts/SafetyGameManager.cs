using UnityEngine;
using Fusion;

public class SafetyGameManager : NetworkBehaviour
{
    [Header("Hardware Link")]
    public TwinController arduinoController; // Drag ArduinoManager here

    [Header("Scene Objects")]
    public Transform greenZone;     // Drag GreenZone here
    public Transform needle;        // Drag Needle here
    public UnityEngine.UI.Button confirmButton; // Drag Button here

    [Header("Game Logic")]
    public float zoneWidth = 0.15f;
    public float TargetCenterX = 0f;

    // --- THESE WERE MISSING IN YOUR CODE ---
    // They define how far left/right the needle can go
    private float minX = -0.418f; 
    private float maxX = 0.418f;
    // ---------------------------------------

    public override void Spawned()
    {
        // 1. SEPARATE ROLES
        if (Runner.IsServer) // SUPERVISOR (Host)
        {
            if(greenZone) greenZone.gameObject.SetActive(true);
            if(confirmButton) confirmButton.gameObject.SetActive(true);
            
            // Supervisor sees the needle too so they can judge
            if(needle) needle.gameObject.SetActive(true); 
            
            StartNewRound();
        }
        else // TECHNICIAN (Client)
        {
            if(needle) needle.gameObject.SetActive(true);
            
            // Hide secrets from Technician
            if(greenZone) greenZone.gameObject.SetActive(false);
            if(confirmButton) confirmButton.gameObject.SetActive(false);
        }
    }

    public void StartNewRound()
    {
        if (greenZone != null)
        {
            float randomX = Random.Range(minX + 0.5f, maxX - 0.5f);
            greenZone.localPosition = new Vector3(randomX, greenZone.localPosition.y, greenZone.localPosition.z);
            TargetCenterX = randomX;
        }
    }

    public void OnConfirmButtonPressed()
    {
        if (!arduinoController) return;

        float currentNeedleX = needle.localPosition.x;
        float halfZone = zoneWidth / 2f;
        
        if (currentNeedleX >= (TargetCenterX - halfZone) && currentNeedleX <= (TargetCenterX + halfZone))
        {
            Debug.Log("SUCCESS! Sending 'G'");
            arduinoController.SendLedCommand("G"); 
            Invoke("StartNewRound", 5.0f);
        }
        else
        {
            Debug.Log("FAIL! Sending 'R'");
            arduinoController.SendLedCommand("R"); 
        }
    }

    // This function moves the needle on the Client side
    public override void Render()
    {
        if (needle != null && arduinoController != null)
        {
            // 1. Get the Networked Value (Synced from Host)
            float syncedValue = arduinoController.NetKnobValue;

            // 2. Convert Arduino (0-1023) to Screen Position (-4 to +4)
            float targetX = Remap(syncedValue, 0, 1023, minX, maxX);

            // 3. Move the Needle
            needle.localPosition = new Vector3(targetX, needle.localPosition.y, needle.localPosition.z);
        }
    }

    private float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}