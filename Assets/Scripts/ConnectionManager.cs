using UnityEngine;
using Fusion;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class ConnectionManager : MonoBehaviour
{
    public NetworkRunner runner;

    private async void Start()
    {
        // 1. FORCE BACKGROUND EXECUTION
        Application.runInBackground = true; 
        
        // 2. DISABLE VSYNC (Prevents OS from throttling the window)
        QualitySettings.vSyncCount = 0;

        // 3. FORCE 60 FPS (Keeps the heartbeat steady)
        Application.targetFrameRate = 60;

        // AUTOMATIC START
        await StartHost();
    }

    public async Task StartHost()
    {
        if (runner == null)
        {
            runner = GetComponent<NetworkRunner>();
        }

        // Prevent double-starting
        if (runner.IsRunning) return;

        Debug.Log("Connecting...");

        // FIX: Use 'AutoHostOrClient' so the 2nd window joins the 1st
        await runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient, // <--- CHANGED THIS
            SessionName = "SafetyRoom",
            Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
            SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
        
        Debug.Log("Connected!");
    }
}