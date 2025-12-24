using UnityEngine;
using UnityEngine.UI; 
using Fusion;

public class SafetyGameManager : NetworkBehaviour
{
    [Header("Hardware Link")]
    // This connects to your existing Arduino script
    public TwinController arduinoController;

    [Header("Scene Objects")]
    public Transform greenZone;     // The Green Cube
    public Transform needle;        // Your Sliding Needle
    public Button confirmButton;    // The 2D Button

    [Header("Game Logic")]
    // This variable syncs the target position across the network
    [Networked] public float TargetCenterX { get; set; } 
    
    // Logic Settings: These match the size of your 3D cubes
    private float trackWidth = 1.0f; 
    private float zoneWidth = 0.15f; 

    // This runs automatically when the network starts
    public override void Spawned()
    {
       /// FOR TESTING: We comment out the "if/else" so EVERYONE sees EVERYTHING.
        
        // if (Runner.IsServer) 
        // {
            if(greenZone) greenZone.gameObject.SetActive(true);
            if(confirmButton) confirmButton.gameObject.SetActive(true);
            if(needle) needle.gameObject.SetActive(true); // <--- CHANGED TO TRUE for testing
        // }
        // else 
        // {
        //     if(greenZone) greenZone.gameObject.SetActive(false);
        //     if(confirmButton) confirmButton.gameObject.SetActive(false);
        //     if(needle) needle.gameObject.SetActive(true);
        // }

        // Start the round logic immediately
        if (Runner.IsServer)
        {
            StartNewRound();
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Only move the needle if we have a connection to the Arduino script
        if (needle != null && arduinoController != null)
        {
            // 1. Get Knob Value (0 to 100) from your TwinController
            float knobValue = arduinoController.NetKnobValue; 

            // 2. Normalize the value (0.0 to 1.0)
            float normalized = Mathf.Clamp(knobValue, 0f, 100f) / 100f;

            // 3. Map it to the Track coordinates
            // Since Track is 1.0 wide, center is 0. Range is -0.5 to +0.5.
            float targetX = (normalized * trackWidth) - (trackWidth / 2f);

            // 4. Apply the new position to the Needle
            needle.localPosition = new Vector3(targetX, needle.localPosition.y, needle.localPosition.z);
        }
    }

    void StartNewRound()
    {
        // Calculate the safe area so the Green Zone doesn't stick out of the track
        float halfTrack = trackWidth / 2f;
        float halfZone = zoneWidth / 2f;
        float minX = -halfTrack + halfZone;
        float maxX = halfTrack - halfZone;

        // Pick a random X position
        TargetCenterX = Random.Range(minX, maxX);
        
        UpdateGreenZonePosition();
    }

    void UpdateGreenZonePosition()
    {
        if (greenZone != null)
        {
            // Move the Green Cube to the new hidden target spot
            greenZone.localPosition = new Vector3(TargetCenterX, greenZone.localPosition.y, greenZone.localPosition.z);
        }
    }

    // This function will be triggered by your On-Screen Button
    public void OnConfirmButtonPressed()
    {
        float currentNeedleX = needle.localPosition.x;
        float halfZone = zoneWidth / 2f;
        
        // Check if needle is inside the zone bounds
        if (currentNeedleX >= (TargetCenterX - halfZone) && currentNeedleX <= (TargetCenterX + halfZone))
        {
            Debug.Log("SUCCESS! Sending 'G' to Arduino.");
            
            // CORRECT PASSWORD: Send "G" (must match Arduino code)
            arduinoController.SendLedCommand("G"); 
            
            // Wait 5 seconds before resetting so you can see the light
            Invoke("StartNewRound", 5.0f);
        }
        else
        {
            Debug.Log("FAIL! Sending 'R' to Arduino.");
            
            // CORRECT PASSWORD: Send "R" (must match Arduino code)
            arduinoController.SendLedCommand("R"); 
        }
    }
}