using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    
    [Header("Game variables that need to be sent to the Database")]
    public static float BestTime = 0f;

    public static int CurrentCheckpointCount = 0;

    public static int TotalCheckpointCount = 8;

    [SerializeField] private DatabaseManager databaseManager;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
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

    public void EndGame()
    {
        StartCoroutine(SendGameResult(BestTime, CurrentCheckpointCount));
        
    }

    private IEnumerator SendGameResult(float gameTime, int score)
    {
        Debug.Log("Game is afgelopen! Resultaten worden verzonden naar de database.");
        string apiUrl = ApiConfig.BaseUrl + "/game/result";

       string json = JsonUtility.ToJson(new GameResultRequest
            {
            sessionId = databaseManager.ActivePlayer.sessionId,
            score = score,
            time = gameTime
        });

        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

        using UnityWebRequest request = new UnityWebRequest(
            apiUrl,
            "POST"
        );

        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader(
            "Content-Type",
            "application/json"
        );

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

