using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class DatabaseManager : MonoBehaviour
{
    private const string activePlayerEndpoint =
        "/player/active?clientId=1";


    //DEBUGING
    private void Start()
    {
        GetActivePlayer();
    }

    public void GetActivePlayer()
    {
        StartCoroutine(GetActivePlayerRequest());
    }

    private IEnumerator GetActivePlayerRequest()
    {
        string apiUrl =
            ApiConfig.BaseUrl + activePlayerEndpoint;

        using UnityWebRequest request = UnityWebRequest.Get(apiUrl);

        yield return request.SendWebRequest();

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

        ActivePlayer activePlayer =
            JsonUtility.FromJson<ActivePlayer>(
                request.downloadHandler.text
            );

        Debug.Log($"Speler: {activePlayer.name}");
        Debug.Log($"Player ID: {activePlayer.playerId}");
        Debug.Log($"Game: {activePlayer.game.name}");
        Debug.Log($"Gebruikt score: {activePlayer.game.usesScore}");
    }
}