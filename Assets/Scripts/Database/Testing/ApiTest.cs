using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class ApiTest : MonoBehaviour
{
    private const string apiUrl =
        ApiConfig.BaseUrl + "/player/active?clientId=1";

    private void Start()
    {
        StartCoroutine(GetActivePlayer());
    }

    private IEnumerator GetActivePlayer()
    {
        Debug.Log($"Request URL: {apiUrl}");

        using UnityWebRequest request = UnityWebRequest.Get(apiUrl);

        yield return request.SendWebRequest();

        Debug.Log($"HTTP status: {request.responseCode}");

        if (request.responseCode == 404)
        {
            Debug.Log("Er is momenteel geen actieve speler.");
            yield break;
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(request.error);
            yield break;
        }

        string json = request.downloadHandler.text;

        ActivePlayer activePlayer =
            JsonUtility.FromJson<ActivePlayer>(json);

        Debug.Log($"Speler: {activePlayer.name}");
        Debug.Log($"Player ID: {activePlayer.playerId}");
        Debug.Log($"Game: {activePlayer.game.name}");
        Debug.Log($"Gebruikt score: {activePlayer.game.usesScore}");
    }
}