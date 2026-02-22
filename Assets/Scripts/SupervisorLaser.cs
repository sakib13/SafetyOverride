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

    [Networked] private Vector3 LaserOrigin { get; set; }
    [Networked] private Vector3 LaserDirection { get; set; }
    [Networked] private NetworkBool LaserActive { get; set; }

    private LineRenderer _lineRenderer;
    private OVRCameraRig _cameraRig;

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
        LaserActive = false;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Runner.IsServer) return;
        if (_cameraRig == null) return;

        Transform rightHand = _cameraRig.rightHandAnchor;
        LaserOrigin = rightHand.position;
        LaserDirection = rightHand.forward;

        // Activate laser only after first valid hand data
        if (!LaserActive)
            LaserActive = true;
    }

    public override void Render()
    {
        if (_lineRenderer == null) return;

        _lineRenderer.enabled = LaserActive;

        if (LaserActive)
        {
            _lineRenderer.SetPosition(0, LaserOrigin);
            _lineRenderer.SetPosition(1, LaserOrigin + LaserDirection * laserLength);
        }
    }
}
