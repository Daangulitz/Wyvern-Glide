using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] private DatabaseManager databaseManager;

    private void Start()
    {
        databaseManager.OnActivePlayerLoaded += CheckPlayer;
    }

    private void CheckPlayer()
    {
        if (databaseManager.ActivePlayer == null)
        {
            Debug.Log("Geen actieve speler. Game wacht.");
            return;
        }

        Debug.Log(
            $"Game kan starten voor {databaseManager.ActivePlayer.name}!"
        );
    }
}