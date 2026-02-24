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
    public GameObject clientButton;

    [Header("Green Zone Movement")]
    [Tooltip("Speed of green zone oscillation. Higher = faster.")]
    public float greenZoneSpeed = 0.3f;

    [Tooltip("How long the green zone pauses when it stops (seconds).")]
    public float pauseDuration = 5f;

    [Header("Game Logic")]
    public float zoneWidth = 0.15f;

    private float minX = -0.418f;
    private float maxX = 0.418f;

    [Networked] private float GreenZoneX { get; set; }
    [Networked] public NetworkBool ClientConfirmed { get; set; }
    [Networked] private NetworkBool GreenZonePaused { get; set; }

    private float _pauseTimer;
    private float _nextPauseTime;

    [Header("Button Materials")]
    public Material redMaterial;
    public Material greenMaterial;
    public Material yellowMaterial;

    private Renderer _supervisorButtonRenderer;
    private Renderer _clientButtonRenderer;

    public override void Spawned()
    {
        if (Runner.IsServer) // SUPERVISOR (Host)
        {
            if (greenZone) greenZone.gameObject.SetActive(true);
            if (confirmButton) confirmButton.SetActive(true);
            if (needle) needle.gameObject.SetActive(false);
            if (clientButton) clientButton.SetActive(false);
            _nextPauseTime = Random.Range(3f, 8f);
        }
        else // TECHNICIAN (Client)
        {
            if (needle) needle.gameObject.SetActive(true);
            if (clientButton) clientButton.SetActive(true);
            if (greenZone) greenZone.gameObject.SetActive(false);
            if (confirmButton) confirmButton.SetActive(false);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Runner.IsServer) return;

        float dt = Runner.DeltaTime;

        if (GreenZonePaused)
        {
            // Count down pause timer
            _pauseTimer -= dt;
            if (_pauseTimer <= 0f)
            {
                GreenZonePaused = false;
                _nextPauseTime = Random.Range(3f, 8f);
            }
            return;
        }

        // Count down until next pause
        _nextPauseTime -= dt;
        if (_nextPauseTime <= 0f)
        {
            GreenZonePaused = true;
            _pauseTimer = pauseDuration;
            return;
        }

        float amplitude = (maxX - minX) / 2f - zoneWidth / 2f;
        GreenZoneX = Mathf.Sin((float)Runner.SimulationTime * greenZoneSpeed) * amplitude;
    }

    // Called by Client's yellow button UnityEvent (_whenSelect)
    public void OnClientButtonPressed()
    {
        Rpc_SetClientConfirmed();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void Rpc_SetClientConfirmed()
    {
        ClientConfirmed = true;
        Debug.Log("[SafetyGameManager] Client confirmed - supervisor button unlocked!");
    }

    // ---- DEBUG SHORTCUT (REMOVABLE) ----
    // Press C in Editor to simulate client button press
    #if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C) && Runner != null && !Runner.IsServer)
        {
            Debug.Log("[DEBUG] Simulating client button press via keyboard");
            OnClientButtonPressed();
        }
    }
    #endif
    // ---- END DEBUG SHORTCUT ----

    // Called by Supervisor's button UnityEvent (_whenSelect)
    public void OnConfirmButtonPressed()
    {
        if (!arduinoController) return;

        float currentNeedleX = needle.localPosition.x;
        float halfZone = zoneWidth / 2f;

        if (currentNeedleX >= (GreenZoneX - halfZone) && currentNeedleX <= (GreenZoneX + halfZone))
        {
            Debug.Log("SUCCESS! Sending 'G'");
            arduinoController.Rpc_SendLedCommand("G");
        }
        else
        {
            Debug.Log("FAIL! Sending 'R'");
            arduinoController.Rpc_SendLedCommand("R");
        }

        // Reset for next attempt
        ClientConfirmed = false;
    }

    public override void Render()
    {
        // Update green zone visual position
        if (greenZone != null)
        {
            greenZone.localPosition = new Vector3(
                GreenZoneX,
                greenZone.localPosition.y,
                greenZone.localPosition.z
            );
        }

        // Supervisor button: swap material to red or green based on client confirmation
        if (Runner != null && Runner.IsServer && confirmButton != null)
        {
            if (_supervisorButtonRenderer == null)
            {
                Transform cap = confirmButton.transform.Find("Button/Visuals/ButtonVisual/Button");
                if (cap != null)
                    _supervisorButtonRenderer = cap.GetComponent<MeshRenderer>();
            }

            if (_supervisorButtonRenderer != null)
                _supervisorButtonRenderer.sharedMaterial = ClientConfirmed ? greenMaterial : redMaterial;
        }

        // Client button: swap material to yellow once
        if (Runner != null && !Runner.IsServer && clientButton != null)
        {
            if (_clientButtonRenderer == null)
            {
                Transform cap = clientButton.transform.Find("Button/Visuals/ButtonVisual/Button");
                if (cap != null)
                    _clientButtonRenderer = cap.GetComponent<MeshRenderer>();
                if (_clientButtonRenderer != null)
                    _clientButtonRenderer.sharedMaterial = yellowMaterial;
            }
        }

        // Update needle position from Arduino
        if (needle != null && arduinoController != null)
        {
            float syncedValue = arduinoController.NetKnobValue;
            float targetX = Remap(syncedValue, 0, 1023, minX, maxX);
            // Negate X for client to fix face-to-face mirroring
            float displayX = Runner.IsServer ? targetX : -targetX;
            needle.localPosition = new Vector3(displayX, needle.localPosition.y, needle.localPosition.z);
        }
    }

    private float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}