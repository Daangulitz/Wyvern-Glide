using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class DatabaseManager : MonoBehaviour
{
    public ActivePlayer ActivePlayer { get; private set; }

    public System.Action OnActivePlayerLoaded;

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
            Debug.Log("Er is geen actieve speler.");
            yield break;
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(request.error);
            yield break;
        }

        ActivePlayer =
            JsonUtility.FromJson<ActivePlayer>(
                request.downloadHandler.text
            );

        OnActivePlayerLoaded?.Invoke();

        Debug.Log($"Speler: {ActivePlayer.name}");
        Debug.Log($"Player ID: {ActivePlayer.playerId}");
        Debug.Log($"Game: {ActivePlayer.game.name}");
        Debug.Log($"Gebruikt score: {ActivePlayer.game.usesScore}");
    }
}