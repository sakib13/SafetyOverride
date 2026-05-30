using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using System.Linq;
using System.Threading.Tasks;
#endif

/// <summary>
/// Handles Photon Fusion networking for the SafetyOverride experience.
/// On Quest: Local Matchmaking building block handles connection.
/// In Editor: Disables building block matchmaking, waits for Quest to create a session,
/// then joins it directly via Photon lobby discovery as the Arduino serial bridge.
/// </summary>
public class ConnectionManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Network Settings")]
    public NetworkRunner runner;

    [Tooltip("Fixed session name for all devices to join")]
    public string sessionName = "SafetyRoom";

    private bool _isConnected = false;

    public bool IsConnected => _isConnected;

    // Kept for ManualCalibrationManager compatibility (script is disabled but still compiles)
    public void StartNetworkAfterCalibration() { }

    private void Awake()
    {
#if UNITY_EDITOR
        // Disable building block matchmaking in Editor — OVR APIs don't work
        // and LocalMatchmaking would create its own separate session
        var localMatchmaking = FindObjectOfType<Meta.XR.MultiplayerBlocks.Shared.LocalMatchmaking>();
        if (localMatchmaking != null)
        {
            localMatchmaking.gameObject.SetActive(false);
            Debug.Log("[ConnectionManager] Disabled LocalMatchmaking for Editor mode");
        }
#endif
    }

    private void Start()
    {
        Application.runInBackground = true;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

#if UNITY_EDITOR
        JoinQuestSession();
#else
        Debug.Log("[ConnectionManager] Networking delegated to Local Matchmaking building block.");
#endif
    }

#if UNITY_EDITOR
    private async void JoinQuestSession()
    {
        Debug.Log("[ConnectionManager] Editor: waiting 8s for Quest to create session...");
        await Task.Delay(8000);

        if (this == null) return; // Play mode stopped

        // Create a standalone runner — don't use building block runners
        var runnerObj = new GameObject("EditorRunner");
        runner = runnerObj.AddComponent<NetworkRunner>();
        runner.AddCallbacks(this);
        var sceneManager = runnerObj.AddComponent<NetworkSceneManagerDefault>();

        // Shared mode with lobby name matching Quest's CustomMatchmaking lobby
        // No session name = auto-join first available session in this lobby
        var result = await runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Shared,
            CustomLobbyName = "myLobby",
            SceneManager = sceneManager
        });

        if (result.Ok)
        {
            _isConnected = true;
            Debug.Log($"[ConnectionManager] Editor connected! Session: '{runner.SessionInfo?.Name}', Players: {runner.ActivePlayers?.Count()}");
        }
        else
        {
            Debug.LogError($"[ConnectionManager] Failed: {result.ShutdownReason} - {result.ErrorMessage}");
        }
    }
#endif

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
