using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class TestGame : MonoBehaviour
{
    [SerializeField] private DatabaseManager databaseManager;

    private float startTime;

    private void Start()
    {
        databaseManager.OnActivePlayerLoaded += StartGame;
    }

    private void StartGame()
    {
        Debug.Log($"Test game gestart voor {databaseManager.ActivePlayer.name}!");

        startTime = Time.time;

        StartCoroutine(PlayTestGame());
    }

    private IEnumerator PlayTestGame()
    {
        yield return new WaitForSeconds(3f);

        FinishGame();
    }

    private void FinishGame()
    {
        float gameTime = Time.time - startTime;
        int score = 100;

        Debug.Log("Test game afgelopen!");
        Debug.Log($"Tijd: {gameTime:F2} seconden");
        Debug.Log($"Score: {score}");

        StartCoroutine(SendGameResult(gameTime, score));
    }

    private IEnumerator SendGameResult(float gameTime, int score)
    {
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