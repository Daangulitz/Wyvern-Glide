using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class DatabaseManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int clientId = 1;
    [SerializeField] private bool fetchOnStart = true;
    [SerializeField] private float timeoutSeconds = 10f;
    [Tooltip("How many extra times to try again after a connection problem (404 and server errors are not retried).")]
    [SerializeField] private int retryCount = 2;
    [SerializeField] private float retryDelaySeconds = 2f;
    [Tooltip("When the server says there is no active player, ask again automatically.")]
    [SerializeField] private bool refreshWhenNoPlayer = true;
    [SerializeField] private float noPlayerRefreshSeconds = 5f;

    [Header("Debug")]
    [SerializeField] private bool verboseLogging = true;

    /// <summary>The player that is currently active, or null when none is loaded.</summary>
    public ActivePlayer ActivePlayer { get; private set; }

    public bool HasActivePlayer => ActivePlayer != null;
    public bool IsLoading { get; private set; }

    private bool _noPlayerReported = false; // so the "no player" log/event only happens once per waiting period

    /// <summary>Called when an active player was loaded successfully.</summary>
    public Action OnActivePlayerLoaded;

    /// <summary>Called when the server says there is no active player (404).</summary>
    public Action OnNoActivePlayer;

    /// <summary>Called when the request failed (no connection, server error, bad data). Gives the error text.</summary>
    public Action<string> OnRequestFailed;

    private string ActivePlayerUrl => $"{ApiConfig.BaseUrl}/player/active?clientId={clientId}";

    private void Start()
    {
        if (verboseLogging) Debug.Log("[DatabaseManager] Start");

        if (fetchOnStart)
            GetActivePlayer();
    }

    private void OnDisable()
    {
        // Coroutines stop when the object is disabled, so allow a new request later.
        IsLoading = false;
    }

    /// <summary>Asks the server for the active player. Safe to call again to refresh.</summary>
    public void GetActivePlayer()
    {
        if (IsLoading)
        {
            if (verboseLogging) Debug.Log("[DatabaseManager] Already loading, request ignored.");
            return;
        }

        StartCoroutine(GetActivePlayerRequest());
    }

    /// <summary>Forget the current player (for example when a session ends).</summary>
    public void ClearActivePlayer()
    {
        ActivePlayer = null;
    }

    private IEnumerator GetActivePlayerRequest()
    {
        IsLoading = true;

        string url = ActivePlayerUrl;
        int totalAttempts = Mathf.Max(0, retryCount) + 1;

        // Keeps asking every few seconds while there is no active player.
        while (true)
        {
            bool noPlayer = false;

            for (int attempt = 1; attempt <= totalAttempts; attempt++)
            {
                if (verboseLogging) Debug.Log($"[DatabaseManager] GET {url} (try {attempt}/{totalAttempts})");

                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    request.timeout = Mathf.CeilToInt(timeoutSeconds);

                    // SendWebRequest can throw straight away (for example "Insecure connection not allowed"),
                    // which would silently kill the coroutine. Catch it so the real reason is logged.
                    UnityWebRequestAsyncOperation operation;
                    try
                    {
                        operation = request.SendWebRequest();
                    }
                    catch (Exception e)
                    {
                        Fail($"Could not send request: {e.Message}\n" +
                             "If this says 'Insecure connection not allowed', set Project Settings > Player > " +
                             "Other Settings > 'Allow downloads over HTTP' to 'Always allowed' (or use https).");
                        yield break;
                    }

                    yield return operation;

                    long code = request.responseCode;

                    if (verboseLogging)
                        Debug.Log($"[DatabaseManager] Done: result={request.result}, code={code}, error={request.error}");

                    // 404 = the server answered, there is just no active player.
                    if (code == 404)
                    {
                        ActivePlayer = null;

                        if (!_noPlayerReported)
                        {
                            _noPlayerReported = true;
                            Debug.Log("[DatabaseManager] Er is geen actieve speler.");
                            OnNoActivePlayer?.Invoke();
                        }

                        if (!refreshWhenNoPlayer)
                        {
                            IsLoading = false;
                            yield break;
                        }

                        noPlayer = true;
                        break; // leave the retry loop, wait, then ask again
                    }

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        _noPlayerReported = false;
                        HandleSuccess(request.downloadHandler.text);
                        yield break;
                    }

                    // Connection problems and timeouts can be temporary: try again.
                    bool canRetry = request.result == UnityWebRequest.Result.ConnectionError
                                    && attempt < totalAttempts;

                    if (canRetry)
                    {
                        Debug.LogWarning($"[DatabaseManager] Connection problem ({request.error}), trying again in {retryDelaySeconds}s...");
                        yield return new WaitForSeconds(retryDelaySeconds);
                        continue;
                    }

                    string body = request.downloadHandler != null ? request.downloadHandler.text : "";
                    Fail($"Request failed: {request.error} (code {code}) {body}");
                    yield break;
                }
            }

            if (!noPlayer) yield break;

            if (verboseLogging)
                Debug.Log($"[DatabaseManager] No active player, asking again in {noPlayerRefreshSeconds}s...");

            yield return new WaitForSeconds(noPlayerRefreshSeconds);
        }
    }

    private void HandleSuccess(string json)
    {
        ActivePlayer player;

        try
        {
            player = JsonUtility.FromJson<ActivePlayer>(json);
        }
        catch (Exception e)
        {
            Fail($"Server answer is not valid JSON: {e.Message}\n{json}");
            return;
        }

        if (player == null)
        {
            Fail($"Server answer was empty or could not be read:\n{json}");
            return;
        }

        ActivePlayer = player;
        IsLoading = false;

        Debug.Log($"[DatabaseManager] Speler: {player.name}");
        Debug.Log($"[DatabaseManager] Player ID: {player.playerId}");

        if (player.game != null)
        {
            Debug.Log($"[DatabaseManager] Game: {player.game.name}");
            Debug.Log($"[DatabaseManager] Gebruikt score: {player.game.usesScore}");
        }
        else
        {
            Debug.LogWarning("[DatabaseManager] The player has no 'game' data in the server answer.");
        }

        OnActivePlayerLoaded?.Invoke();
    }

    private void Fail(string message)
    {
        IsLoading = false;
        Debug.LogError($"[DatabaseManager] {message}");
        OnRequestFailed?.Invoke(message);
    }
}