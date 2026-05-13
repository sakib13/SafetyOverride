using UnityEngine;
using System.Linq;

/// <summary>
/// Diagnostic script that logs the state of the colocation pipeline.
/// Add this to any active GameObject in the scene (e.g. CalibrationManager).
/// Remove after debugging.
/// </summary>
public class ColocationDiagnostics : MonoBehaviour
{
    private bool _entitlementChecked = false;
    private bool _entitlementResult = false;
    private float _checkTimer = 0f;
    private int _checkCount = 0;

    private void Start()
    {
        Debug.Log("[ColocationDiag] === DIAGNOSTICS STARTED ===");
        Debug.Log($"[ColocationDiag] Device: {SystemInfo.deviceModel}, DeviceID hash: {(ulong)SystemInfo.deviceUniqueIdentifier.GetHashCode()}");
        CheckEntitlement();
    }

    private void CheckEntitlement()
    {
#if META_PLATFORM_SDK_DEFINED
        Debug.Log("[ColocationDiag] META_PLATFORM_SDK_DEFINED = true, checking entitlement...");
        Meta.XR.MultiplayerBlocks.Shared.PlatformInit.GetEntitlementInformation(info =>
        {
            _entitlementChecked = true;
            if (info.OculusUser != null)
            {
                _entitlementResult = true;
                Debug.Log($"[ColocationDiag] ENTITLEMENT OK - UserID: {info.OculusUser.ID}, OculusID: {info.OculusUser.OculusID}, Entitled: {info.IsEntitled}");
            }
            else
            {
                Debug.LogError($"[ColocationDiag] ENTITLEMENT FAILED - OculusUser is NULL, Entitled: {info.IsEntitled}");
            }
        });
#else
        Debug.LogError("[ColocationDiag] META_PLATFORM_SDK_DEFINED is NOT defined! Colocation cannot work.");
        _entitlementChecked = true;
#endif
    }

    private void Update()
    {
        _checkTimer += Time.deltaTime;

        // Log colocation state at 5s, 15s, 30s
        if ((_checkCount == 0 && _checkTimer > 5f) ||
            (_checkCount == 1 && _checkTimer > 15f) ||
            (_checkCount == 2 && _checkTimer > 30f))
        {
            _checkCount++;
            LogColocationState();
        }
    }

    private void LogColocationState()
    {
        Debug.Log($"[ColocationDiag] === STATE CHECK ({_checkTimer:F0}s) ===");
        Debug.Log($"[ColocationDiag] Entitlement checked: {_entitlementChecked}, result: {_entitlementResult}");

        // Check Fusion runner
        var runners = Fusion.NetworkRunner.Instances;
        Debug.Log($"[ColocationDiag] NetworkRunner instances: {runners.Count}");
        foreach (var runner in runners)
        {
            if (runner != null)
            {
                Debug.Log($"[ColocationDiag] Runner '{runner.name}' - IsRunning: {runner.IsRunning}, IsMaster: {runner.IsSharedModeMasterClient}, PlayerCount: {runner.ActivePlayers?.Count()}");
            }
        }

        // Check bootstrapper
        var bootstrappers = FindObjectsOfType<Meta.XR.MultiplayerBlocks.Fusion.FusionNetworkBootstrapper>(true);
        Debug.Log($"[ColocationDiag] FusionNetworkBootstrapper count: {bootstrappers.Length}");
        foreach (var b in bootstrappers)
        {
            var no = b.GetComponent<Fusion.NetworkObject>();
            Debug.Log($"[ColocationDiag]   Bootstrapper on '{b.gameObject.name}' - active: {b.gameObject.activeInHierarchy}, enabled: {b.enabled}, NetworkObject: {no != null}, NO.IsValid: {no?.IsValid}");
        }

        // Check colocation controller
        var controllers = FindObjectsOfType<Meta.XR.MultiplayerBlocks.Shared.ColocationController>(true);
        Debug.Log($"[ColocationDiag] ColocationController count: {controllers.Length}");

        // Check SharedSpatialAnchorCore
        var ssaCores = FindObjectsOfType<Meta.XR.BuildingBlocks.SharedSpatialAnchorCore>(true);
        Debug.Log($"[ColocationDiag] SharedSpatialAnchorCore count: {ssaCores.Length}");

        Debug.Log($"[ColocationDiag] === END STATE CHECK ===");
    }
}
