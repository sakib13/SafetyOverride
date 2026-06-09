using UnityEngine;
using Fusion;

public class SupervisorLaser : NetworkBehaviour
{
    [Header("Laser Settings")]
    [Tooltip("How far the laser ray extends")]
    public float laserLength = 3f;

    [Tooltip("Thickness of the laser ray")]
    public float laserWidth = 0.005f;

    [Tooltip("Color of the laser ray")]
    public Color laserColor = Color.red;

    [Header("References")]
    [Tooltip("The GameContent parent - used for face-to-face mirroring")]
    public Transform gameContent;

    // Network the offset from GC rather than world position - avoids sync issues
    [Networked] private Vector3 LaserLocalOffset { get; set; }
    [Networked] private Vector3 LaserLocalDirection { get; set; }
    [Networked] private NetworkBool LaserActive { get; set; }

    private LineRenderer _lineRenderer;
    private OVRCameraRig _cameraRig;

    public override void Spawned()
    {
        enabled = false;
        return;
        _cameraRig = FindFirstObjectByType<OVRCameraRig>();

        // Create LineRenderer on this GameObject
        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.positionCount = 2;
        _lineRenderer.startWidth = laserWidth;
        _lineRenderer.endWidth = laserWidth * 0.5f;
        _lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
        _lineRenderer.material.color = laserColor;
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.enabled = false;

        // Laser starts inactive - activated after first valid hand data
        if (HasStateAuthority)
            LaserActive = false;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Runner.IsSharedModeMasterClient) return;
        if (_cameraRig == null || gameContent == null) return;

        Transform rightHand = _cameraRig.rightHandAnchor;

        // Store hand position as offset from own gameContent - no host GC sync needed
        LaserLocalOffset = rightHand.position - gameContent.position;
        LaserLocalDirection = rightHand.forward;

        if (!LaserActive)
            LaserActive = true;
    }

    private void EnsureLineRenderer()
    {
        if (_lineRenderer != null) return;

        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.positionCount = 2;
        _lineRenderer.startWidth = laserWidth;
        _lineRenderer.endWidth = laserWidth * 0.5f;
        _lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
        _lineRenderer.material.color = laserColor;
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.enabled = false;
    }

    public override void Render()
    {
        EnsureLineRenderer();
        if (_lineRenderer == null) return;

        _lineRenderer.enabled = LaserActive;

        if (LaserActive && gameContent != null)
        {
            Vector3 offset = LaserLocalOffset;
            Vector3 dir = LaserLocalDirection;

            // Client: negate X and Z for face-to-face (180 degree Y rotation)
            if (!Runner.IsSharedModeMasterClient)
            {
                offset.x = -offset.x;
                offset.z = -offset.z;
                dir.x = -dir.x;
                dir.z = -dir.z;
            }

            Vector3 origin = gameContent.position + offset;
            _lineRenderer.SetPosition(0, origin);
            _lineRenderer.SetPosition(1, origin + dir * laserLength);
        }
    }
}
