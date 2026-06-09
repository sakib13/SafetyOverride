using UnityEngine;

/// <summary>
/// Simple Manual Calibration for Collocated VR (Seated scenario)
///
/// How to use:
/// 1. Both users sit at opposite sides of a table, FACING EACH OTHER
/// 2. Both users press A button or Trigger on right controller
/// 3. Game content appears in front of them at table height
///
/// With Eye Level tracking origin, content is positioned relative to head.
/// heightOffset controls how far below eye level (-0.3m = table height when seated)
/// </summary>
public class ManualCalibrationManager : MonoBehaviour
{
    [Header("Required References")]
    [Tooltip("Parent object containing all game content (LinearGauge, BigRedButton)")]
    public Transform gameContentParent;

    [Tooltip("Reference to ConnectionManager to start networking after calibration")]
    public ConnectionManager connectionManager;

    [Header("Optional References")]
    [Tooltip("The OVRCameraRig - auto-found if not set")]
    public OVRCameraRig cameraRig;

    [Tooltip("Instruction canvas shown before calibration - hidden on trigger press")]
    public GameObject instructionCanvas;

    [Header("Calibration Settings")]
    [Tooltip("Distance in front of user where content appears (arm reach ~0.5m)")]
    public float contentDistance = 0.5f;

    [Tooltip("Height offset from eye level (negative = below eyes, e.g., -0.3 for table height)")]
    public float heightOffset = -0.3f;

    // State
    private bool _isCalibrated = false;
    private bool _isReady = false;

    public bool IsCalibrated => _isCalibrated;

    private void Start()
    {
        // Auto-find OVRCameraRig if not assigned
        if (cameraRig == null)
        {
            cameraRig = FindFirstObjectByType<OVRCameraRig>();
        }

        if (connectionManager == null)
        {
            connectionManager = FindFirstObjectByType<ConnectionManager>();
        }

        // Validate required references
        if (gameContentParent == null)
        {
            Debug.LogError("[Calibration] GameContentParent is not assigned! Please assign it in the Inspector.");
            return;
        }

        // Move content far away until calibrated (don't use SetActive(false) - breaks poke interaction)
        gameContentParent.position = new Vector3(0, -100f, 0);

        // Wait a moment for tracking to initialize
        Invoke(nameof(SetReady), 1.0f);

        Debug.Log("[Calibration] Stand at the floor marker and press A button or Trigger to calibrate.");
    }

    private void SetReady()
    {
        _isReady = true;
        Debug.Log("[Calibration] Ready! Press A button or Right Trigger to place game content.");
    }

    private void Update()
    {
        if (_isCalibrated || !_isReady) return;

        // Check for calibration input
        bool calibratePressed = false;

        // A button on right controller
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            calibratePressed = true;
        }

        // Right trigger
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            calibratePressed = true;
        }

        // Fallback for editor testing
        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Space))
        {
            calibratePressed = true;
        }
        #endif

        if (calibratePressed)
        {
            PerformCalibration();
        }
    }

    private void PerformCalibration()
    {
        if (_isCalibrated) return;
        if (cameraRig == null)
        {
            Debug.LogError("[Calibration] CameraRig not found!");
            return;
        }

        _isCalibrated = true;

        // Hide instruction canvas
        if (instructionCanvas != null)
            instructionCanvas.SetActive(false);

        Transform head = cameraRig.centerEyeAnchor;

        // Get horizontal forward direction
        Vector3 userForward = head.forward;
        userForward.y = 0;
        userForward.Normalize();

        // Position content in front of user, relative to current head position
        // With Eye Level tracking origin, head.position.y is relative to starting eye level
        Vector3 contentPosition = head.position + userForward * contentDistance;
        contentPosition.y = head.position.y + heightOffset; // Offset from eye level (negative = below)

        // Place game content at calibrated position (no rotation - keeps original orientation)
        gameContentParent.position = contentPosition;

        Debug.Log($"[Calibration] Content placed at {contentPosition}");

        // Start network connection
        if (connectionManager != null)
        {
            connectionManager.StartNetworkAfterCalibration();
        }

        Debug.Log("[Calibration] Calibration complete!");
    }

    /// CONTENT IS NOT USED CURRENTLY USED ANYWHERE
    /// Call this to reset calibration (for testing)
    /// </summary>
    public void ResetCalibration()
    {
        _isCalibrated = false;

        if (gameContentParent != null)
        {
            gameContentParent.position = new Vector3(0, -100f, 0);
        }

        Debug.Log("[Calibration] Reset. Press A or Trigger to recalibrate.");
    }
}
