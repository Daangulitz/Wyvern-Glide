using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Game variables that need to be sent to the Database")]
    public static float BestTime = 0f;

    public static int CurrentCheckpointCount = 0;

    public static int TotalCheckpointCount = 8;

    [SerializeField] private DatabaseManager databaseManager;

    [Header("Reset after finish")]
    [Tooltip("Seconds between sending the result and reloading the game (time for the Yaw to park, or to show the result).")]
    [SerializeField] private float resetDelaySeconds = 3f;

    /// <summary>
    /// Fired when the game is finished and the result was sent, just before the game resets.
    /// Park the Yaw here.
    /// </summary>
    public static event Action OnGameFinished;

    private bool _isEnding = false;
    private bool _resetPending = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void UpdateBestTime(float newBestTime)
    {
        BestTime = newBestTime;
        Debug.Log($"Nieuwe beste tijd: {BestTime:F2} seconden");
    }

    public void UpdateCheckpointCount()
    {
        CurrentCheckpointCount++;
    }

    /// <summary>Ends the game: sends the result, then resets everything for the next player.</summary>
    public void EndGame()
    {
        if (_isEnding) return;
        _isEnding = true;

        StartCoroutine(EndGameRoutine());
    }

    private IEnumerator EndGameRoutine()
    {
        Debug.Log("Game is afgelopen! Resultaten worden verzonden naar de database.");

        // Wait until the result is sent (also continues if sending failed, so the game never gets stuck).
        yield return SendGameResult(BestTime, CurrentCheckpointCount);

        // Let other scripts react, for example the Yaw parking.
        OnGameFinished?.Invoke();

        if (resetDelaySeconds > 0f)
            yield return new WaitForSeconds(resetDelaySeconds);

        ResetGame();
    }

    private void ResetGame()
    {
        Debug.Log("Game wordt gereset voor de volgende speler.");

        // Values that live outside the scene.
        BestTime = 0f;
        CurrentCheckpointCount = 0;

        if (databaseManager != null)
            databaseManager.ClearActivePlayer();

        _isEnding = false;
        _resetPending = true;

        // Reloading the scene resets the dragon, the timer, the checkpoints and the controls (locked until calibration).
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!_resetPending) return;
        _resetPending = false;

        // The old DatabaseManager was destroyed with the old scene: find the new one.
        databaseManager = FindFirstObjectByType<DatabaseManager>();

        if (databaseManager == null)
        {
            Debug.LogWarning("GameManager: no DatabaseManager found in the new scene, no new player can be loaded.");
            return;
        }

        // Starts asking for the next active player (and keeps asking every few seconds until there is one).
        databaseManager.ResetForNewPlayer();
    }

    private IEnumerator SendGameResult(float gameTime, int score)
    {
        if (databaseManager == null || databaseManager.ActivePlayer == null)
        {
            Debug.LogError("Resultaat versturen overgeslagen: er is geen actieve speler geladen.");
            yield break;
        }

        string apiUrl = ApiConfig.BaseUrl + "/game/result";

        string json = JsonUtility.ToJson(new GameResultRequest
        {
            sessionId = databaseManager.ActivePlayer.sessionId,
            score = score,
            time = gameTime
        });

        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

        using UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");

        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Resultaat versturen mislukt: {request.error}");
            Debug.LogError(request.downloadHandler.text);
            yield break;
        }

        Debug.Log("Game resultaat succesvol opgeslagen!");
        Debug.Log(request.downloadHandler.text);
    }
}

[System.Serializable]
public class GameResultRequest
{
    public int sessionId;
    public int score;
    public float time;
}