using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles Photon Fusion networking for the SafetyOverride experience.
/// Waits for manual calibration to complete before starting network connection.
/// </summary>
public class ConnectionManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Network Settings")]
    public NetworkRunner runner;

    [Tooltip("Fixed session name for all devices to join")]
    public string sessionName = "SafetyRoom";

    [Header("Calibration")]
    [Tooltip("If true, waits for StartNetworkAfterCalibration() to be called")]
    public bool waitForCalibration = true;

    private bool _isConnecting = false;
    private bool _isConnected = false;
    private bool _calibrationComplete = false;

    public bool IsConnected => _isConnected;

    private void Start()
    {
        Application.runInBackground = true;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        if (!waitForCalibration)
        {
            // Legacy mode: auto-start after delay
            float delay = 5f;
            #if UNITY_EDITOR
            delay = 12f;
            #endif
            Invoke(nameof(StartConnection), delay);
        }
        // Otherwise, wait for StartNetworkAfterCalibration() to be called
    }

    /// <summary>
    /// Called by ManualCalibrationManager after calibration is complete.
    /// </summary>
    public void StartNetworkAfterCalibration()
    {
        if (_calibrationComplete) return;
        _calibrationComplete = true;

        Debug.Log("[ConnectionManager] Calibration complete - starting network connection...");
        StartConnection();
    }

    private async void StartConnection()
    {
        if (_isConnecting || _isConnected) return;
        _isConnecting = true;

        Debug.Log($"[ConnectionManager] Starting Photon connection...");

        // Get or create NetworkRunner
        if (runner == null)
        {
            runner = GetComponent<NetworkRunner>();
        }

        if (runner == null)
        {
            runner = gameObject.AddComponent<NetworkRunner>();
        }

        // Add callbacks
        runner.AddCallbacks(this);

        // Determine game mode
        #if UNITY_EDITOR
        GameMode mode = GameMode.Client; // Editor always joins as client
        Debug.Log("[ConnectionManager] Editor mode - connecting as Client");
        #else
        GameMode mode = GameMode.AutoHostOrClient; // Quest devices auto-determine
        Debug.Log("[ConnectionManager] Quest mode - connecting as AutoHostOrClient");
        #endif

        // Start the game
        var result = await runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = sessionName,
            Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        _isConnecting = false;

        if (result.Ok)
        {
            _isConnected = true;
            Debug.Log($"[ConnectionManager] Connected! IsServer: {runner.IsServer}, IsClient: {runner.IsClient}");
        }
        else
        {
            Debug.LogError($"[ConnectionManager] Connection failed: {result.ShutdownReason}");

            // Retry after delay
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

        // Try to reconnect
        Invoke(nameof(StartConnection), 3f);
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
