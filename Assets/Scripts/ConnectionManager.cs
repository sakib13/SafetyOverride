using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;

/// <summary>
/// Handles Photon Fusion networking for the SafetyOverride experience.
/// Uses Shared Mode for colocation with building blocks.
/// </summary>
public class ConnectionManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Network Settings")]
    public NetworkRunner runner;

    [Tooltip("Fixed session name for all devices to join")]
    public string sessionName = "SafetyRoom";

    private bool _isConnecting = false;
    private bool _isConnected = false;

    public bool IsConnected => _isConnected;

    // Kept for ManualCalibrationManager compatibility (script is disabled but still compiles)
    public void StartNetworkAfterCalibration() { }

    private void Start()
    {
        Application.runInBackground = true;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        // Auto Matchmaking building block handles Fusion connection.
        // Do NOT start a second runner here — it conflicts and causes OperationTimeout.
        Debug.Log("[ConnectionManager] Networking delegated to Auto Matchmaking building block.");
    }

    private async void StartConnection()
    {
        if (_isConnecting || _isConnected) return;
        _isConnecting = true;

        Debug.Log($"[ConnectionManager] Starting Photon connection in Shared mode...");

        // Create a fresh NetworkRunner on a new child object to avoid reuse errors
        var runnerObj = new GameObject("NetworkRunner");
        runnerObj.transform.SetParent(transform);
        runner = runnerObj.AddComponent<NetworkRunner>();
        runner.AddCallbacks(this);

        // Shared mode for all platforms (colocation building blocks require this)
        GameMode mode = GameMode.Shared;
        Debug.Log("[ConnectionManager] Connecting as Shared mode");

        // Get or add scene manager
        var sceneManager = runner.GetComponent<INetworkSceneManager>();
        if (sceneManager == null)
            sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        // Start the game - no Scene passed to avoid additive scene reload
        var result = await runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = sessionName,
            SceneManager = sceneManager
        });

        _isConnecting = false;

        if (result.Ok)
        {
            _isConnected = true;
            Debug.Log($"[ConnectionManager] Connected! IsSharedModeMasterClient: {runner.IsSharedModeMasterClient}");
        }
        else
        {
            Debug.LogError($"[ConnectionManager] Connection failed: {result.ShutdownReason}");
            Debug.Log("[ConnectionManager] Retrying in 5 seconds...");
            Invoke(nameof(StartConnection), 5f);
        }
    }

    // INetworkRunnerCallbacks implementation
    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("[ConnectionManager] Connected to server!");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"[ConnectionManager] Disconnected from server: {reason}");
        _isConnected = false;
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"[ConnectionManager] Connect failed: {reason}");
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[ConnectionManager] Player joined: {player}");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[ConnectionManager] Player left: {player}");
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log($"[ConnectionManager] Session list updated: {sessionList.Count} sessions");
    }

    // Required empty implementations
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"[ConnectionManager] Shutdown: {shutdownReason}");
        _isConnected = false;
    }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
