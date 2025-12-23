using UnityEngine;
using Fusion;

public class TwinController : NetworkBehaviour
{
    // We hide this from Inspector so you don't have to drag it manually anymore
    private SerialController serialController;

    [Header("Game Logic")]
    public float targetValue = 512; 
    public float tolerance = 100; 
    
    [Networked] public float NetKnobValue { get; set; }

    private float hardwareRawValue = 0;

    // --- 1. AUTO-CONNECT ON START ---
    public override void Spawned()
    {
        // HOST ONLY: Find the physical connection
        if (Object.HasStateAuthority)
        {
            serialController = FindObjectOfType<SerialController>();
            
            if (serialController == null)
            {
                Debug.LogError("CRITICAL ERROR: No 'SerialController' found in the scene!");
            }
            else
            {
                Debug.Log("Network Host connected to Serial Port successfully.");
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority) 
        {
            NetKnobValue = hardwareRawValue;
        }
    }

    void Update()
    {
        // Listen for Spacebar on ALL computers (PC or Laptop)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RPC_CheckAlignment(); 
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_CheckAlignment()
    {
        // 2. SAFETY CHECK
        if (serialController == null)
        {
            Debug.LogError("Cannot light LED: Serial Controller is missing!");
            // Try to find it one last time just in case
            serialController = FindObjectOfType<SerialController>();
            if (serialController == null) return;
        }

        Debug.Log("Host received 'Check Alignment' signal!");

        float difference = Mathf.Abs(NetKnobValue - targetValue);

        // LOGIC: Green if Close, Red if Far
        if (difference <= tolerance)
        {
            Debug.Log("Result: SAFE (Green)");
            serialController.SendSerialMessage("G"); 
        }
        else
        {
            Debug.Log("Result: UNSAFE (Red)");
            serialController.SendSerialMessage("R"); 
        }
    }

    // ARDITY LISTENER
    void OnMessageArrived(string msg)
    {
        if (float.TryParse(msg, out float result))
        {
            hardwareRawValue = result;
        }
    }
    
    void OnConnectionEvent(bool success) {}

    void OnGUI()
    {
        // Define the box style
        GUIStyle style = new GUIStyle("box");
        style.fontSize = 20;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleLeft;

        // Calculate limits
        float minGreen = targetValue - tolerance;
        float maxGreen = targetValue + tolerance;
        string status = (NetKnobValue >= minGreen && NetKnobValue <= maxGreen) ? "SAFE (GREEN)" : "UNSAFE (RED)";

        // Create the string to display
        string debugText = 
            $"--- DEBUG INFO ---\n" +
            $"Current Knob Value: {NetKnobValue:F0}\n" +
            $"Target Center:      {targetValue}\n" +
            $"Safe Zone (+/-):    {tolerance}\n" +
            $"------------------\n" +
            $"Turn Green Between: {minGreen} - {maxGreen}\n" +
            $"CURRENT STATUS:     {status}";

        // Draw the box on screen (Top Left)
        GUI.Box(new Rect(10, 10, 300, 160), debugText, style);
    }
}