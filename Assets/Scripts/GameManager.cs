using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    
    [Header("Game variables that need to be sent to the Database")]
    public static float BestTime = 0f;

    public static int CurrentCheckpointCount = 0;

    public static int TotalCheckpointCount = 8;

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
    }

    public void UpdateCheckpointCount()
    {
        CurrentCheckpointCount++;
    }
}
