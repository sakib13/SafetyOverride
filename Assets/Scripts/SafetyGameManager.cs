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

    [Header("Shared Difficulty Cube")]
    [Tooltip("Drag the shared cube here. Its X position controls green zone speed.")]
    public Transform difficultyCube;
    [Tooltip("Cube X position that maps to minimum speed")]
    public float cubeMinX = -0.3f;
    [Tooltip("Cube X position that maps to maximum speed")]
    public float cubeMaxX = 0.3f;
    [Tooltip("Slowest green zone speed")]
    public float minSpeed = 0.1f;
    [Tooltip("Fastest green zone speed")]
    public float maxSpeed = 1.0f;

    [Header("Green Zone Movement")]
    [Tooltip("Speed of green zone oscillation. Higher = faster.")]
    public float greenZoneSpeed = 0.3f;

    [Header("Game Logic")]
    public float zoneWidth = 0.15f;

    private float minX = -0.418f;
    private float maxX = 0.418f;

    [Networked] private float GreenZoneX { get; set; }
    [Networked] public NetworkBool ClientConfirmed { get; set; }

    [Header("Button Materials")]
    public Material redMaterial;
    public Material greenMaterial;
    public Material yellowMaterial;

    private Renderer _supervisorButtonRenderer;
    private Renderer _clientButtonRenderer;
    private MaterialPropertyBlock _clientButtonMPB;
    private Renderer _cubeRenderer;
    private MaterialPropertyBlock _cubeMPB;
    private bool _ledCommandSent = false;
    [Networked] private NetworkBool GreenZoneFrozen { get; set; }

    private void Awake()
    {
        // Hide all role-specific objects until networking assigns roles
        if (greenZone) greenZone.gameObject.SetActive(false);
        if (needle) needle.gameObject.SetActive(false);
        if (confirmButton) confirmButton.SetActive(false);
        if (clientButton) clientButton.SetActive(false);
    }

    public override void Spawned()
    {
        if (Runner.IsSharedModeMasterClient) // SUPERVISOR (first player)
        {
            if (greenZone) greenZone.gameObject.SetActive(true);
            if (confirmButton) confirmButton.SetActive(true);
            if (needle) needle.gameObject.SetActive(false);
            if (clientButton) clientButton.SetActive(false);
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
        if (!Runner.IsSharedModeMasterClient) return;

        // Update green zone speed based on cube position
        if (difficultyCube != null)
        {
            float cubeX = Mathf.Clamp(difficultyCube.localPosition.x, cubeMinX, cubeMaxX);
            float t = (cubeX - cubeMinX) / (cubeMaxX - cubeMinX);
            greenZoneSpeed = Mathf.Lerp(minSpeed, maxSpeed, t);
        }

        if (GreenZoneFrozen) return;

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
        _ledCommandSent = false; // Allow supervisor to send a new LED command
        Debug.Log("[SafetyGameManager] Client confirmed - supervisor button unlocked!");
    }

    // ---- DEBUG SHORTCUTS (REMOVABLE) ----
    // Press C in Editor to simulate client button press
    // Press F in Editor to freeze/unfreeze green zone for testing
    #if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C) && Runner != null && !Runner.IsSharedModeMasterClient)
        {
            Debug.Log("[DEBUG] Simulating client button press via keyboard");
            OnClientButtonPressed();
        }
        if (Input.GetKeyDown(KeyCode.F) && Runner != null)
        {
            Rpc_ToggleGreenZoneFreeze();
        }
    }
    #endif
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void Rpc_ToggleGreenZoneFreeze()
    {
        GreenZoneFrozen = !GreenZoneFrozen;
        Debug.Log($"[DEBUG] Green zone {(GreenZoneFrozen ? "FROZEN" : "MOVING")}");
    }
    // ---- END DEBUG SHORTCUTS ----

    // Called by Supervisor's button UnityEvent (_whenSelect)
    public void OnConfirmButtonPressed()
    {
        if (!arduinoController) return;
        if (_ledCommandSent) return; // Prevent repeated sends while button is held

        float currentNeedleX = needle.localPosition.x;
        float halfZone = zoneWidth / 2f;

        Debug.Log($"[SafetyGameManager] NeedleX={currentNeedleX:F3}, GreenZoneX={GreenZoneX:F3}, halfZone={halfZone:F3}, range=[{(GreenZoneX - halfZone):F3}, {(GreenZoneX + halfZone):F3}], NetKnob={arduinoController.NetKnobValue:F1}");

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

        _ledCommandSent = true;
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
        if (Runner != null && Runner.IsSharedModeMasterClient && confirmButton != null)
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

        // Client button: force yellow every frame via MaterialPropertyBlock
        if (Runner != null && !Runner.IsSharedModeMasterClient && clientButton != null)
        {
            if (_clientButtonRenderer == null)
            {
                Transform cap = clientButton.transform.Find("Button/Visuals/ButtonVisual/Button");
                if (cap != null)
                    _clientButtonRenderer = cap.GetComponent<MeshRenderer>();
                if (_clientButtonRenderer != null)
                    _clientButtonMPB = new MaterialPropertyBlock();
            }
            if (_clientButtonRenderer != null)
            {
                Color yellow = yellowMaterial != null ? yellowMaterial.color : Color.yellow;
                _clientButtonRenderer.GetPropertyBlock(_clientButtonMPB);
                _clientButtonMPB.SetColor("_BaseColor", yellow);
                _clientButtonMPB.SetColor("_Color", yellow);
                _clientButtonRenderer.SetPropertyBlock(_clientButtonMPB);
            }
        }

        // Update cube color based on difficulty
        if (difficultyCube != null)
        {
            if (_cubeRenderer == null)
            {
                _cubeRenderer = difficultyCube.GetComponent<Renderer>();
                _cubeMPB = new MaterialPropertyBlock();
            }
            if (_cubeRenderer != null)
            {
                float cubeX = Mathf.Clamp(difficultyCube.localPosition.x, cubeMinX, cubeMaxX);
                float t = (cubeX - cubeMinX) / (cubeMaxX - cubeMinX);
                Color difficultyColor = Color.Lerp(Color.green, Color.red, t);
                _cubeRenderer.GetPropertyBlock(_cubeMPB);
                _cubeMPB.SetColor("_BaseColor", difficultyColor);
                _cubeMPB.SetColor("_Color", difficultyColor);
                _cubeRenderer.SetPropertyBlock(_cubeMPB);
            }
        }

        // Update needle position from Arduino
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