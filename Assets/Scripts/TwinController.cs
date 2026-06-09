using UnityEngine;
using Fusion;

public class TwinController : NetworkBehaviour
{
    [Header("Ardity Setup")]
    public SerialController serialController;

    [Networked] public float NetKnobValue { get; set; }

    private bool IsSerialBridge => serialController != null && serialController.enabled;

    void Update()
    {
        if (Object == null) return;

        // Laptop (serial bridge) reads potentiometer and broadcasts to all
        if (IsSerialBridge)
        {
            string message = serialController.ReadSerialMessage();
            if (message != null && float.TryParse(message, out float parsedValue))
            {
                Rpc_UpdateKnobValue(parsedValue);
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_UpdateKnobValue(float value)
    {
        NetKnobValue = value;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_SendLedCommand(string command)
    {
        // Only the serial bridge executes the actual write
        if (IsSerialBridge && serialController != null)
        {
            serialController.SendSerialMessage(command);
            Debug.Log($"Sent to Arduino: {command}");
        }
    }
}