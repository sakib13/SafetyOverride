using UnityEngine;
using Fusion;

public class SafetyGameManager : NetworkBehaviour
{
    [Header("Hardware Link")]
    public TwinController arduinoController;

    [Header("Scene Objects")]
    public Transform greenZone;
    public Transform needle;
    public GameObject confirmButton;

    [Header("Game Logic")]
    public float zoneWidth = 0.15f;
    public float TargetCenterX = 0f;

    private float minX = -0.418f;
    private float maxX = 0.418f;

    public override void Spawned()
    {
        if (Runner.IsServer) // SUPERVISOR (Host)
        {
            if (greenZone) greenZone.gameObject.SetActive(true);
            if (confirmButton) confirmButton.SetActive(true);
            if (needle) needle.gameObject.SetActive(false);

            StartNewRound();
        }
        else // TECHNICIAN (Client)
        {
            if (needle) needle.gameObject.SetActive(true);
            if (greenZone) greenZone.gameObject.SetActive(false);
            if (confirmButton) confirmButton.SetActive(false);
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
            arduinoController.Rpc_SendLedCommand("G");
            Invoke("StartNewRound", 5.0f);
        }
        else
        {
            Debug.Log("FAIL! Sending 'R'");
            arduinoController.Rpc_SendLedCommand("R");
        }
    }

    public override void Render()
    {
        if (needle != null && arduinoController != null)
        {
            float syncedValue = arduinoController.NetKnobValue;
            float targetX = Remap(syncedValue, 0, 1023, minX, maxX);
            needle.localPosition = new Vector3(targetX, needle.localPosition.y, needle.localPosition.z);
        }
    }

    private float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}