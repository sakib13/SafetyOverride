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

    [Networked] private Vector3 LaserOrigin { get; set; }
    [Networked] private Vector3 LaserDirection { get; set; }
    [Networked] private NetworkBool LaserActive { get; set; }

    private LineRenderer _lineRenderer;
    private OVRCameraRig _cameraRig;
    private Vector3 _hostGcPosition;
    private bool _hostGcReceived;

    public override void Spawned()
    {
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
        if (!Runner.IsServer) return;
        if (_cameraRig == null) return;

        Transform rightHand = _cameraRig.rightHandAnchor;

        // Store world-space positions
        LaserOrigin = rightHand.position;
        LaserDirection = rightHand.forward;

        // Activate laser and send host gameContent position once
        if (!LaserActive)
        {
            LaserActive = true;
            if (gameContent != null)
                Rpc_SendHostGcPosition(gameContent.position);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_SendHostGcPosition(Vector3 pos)
    {
        _hostGcPosition = pos;
        _hostGcReceived = true;
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

        if (LaserActive)
        {
            Vector3 origin = LaserOrigin;
            Vector3 dir = LaserDirection;

            // Client: proper local-space conversion for face-to-face mirroring
            if (!Runner.IsServer && _hostGcReceived && gameContent != null)
            {
                // Convert host world-space to local offset
                Vector3 localOrigin = origin - _hostGcPosition;

                // Negate X and Z for face-to-face (180 degree Y flip)
                localOrigin.x = -localOrigin.x;
                localOrigin.z = -localOrigin.z;
                dir.x = -dir.x;
                dir.z = -dir.z;

                // Convert back using client's gameContent position
                origin = localOrigin + gameContent.position;
            }
            else if (!Runner.IsServer)
            {
                // Fallback: simple X negation if host GC not received yet
                origin.x = -origin.x;
                dir.x = -dir.x;
            }

            _lineRenderer.SetPosition(0, origin);
            _lineRenderer.SetPosition(1, origin + dir * laserLength);
        }
    }
}
