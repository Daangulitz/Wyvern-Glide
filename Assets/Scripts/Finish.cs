using UnityEngine;

public class Finish : MonoBehaviour
{
    private static Finish instance;
    private float finishTime;

    public int CurrentCheckpointCount{private get; set;} = 0;

    private int TotalCheckpointCount = 0;

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

    private void Start()
    {
        TotalCheckpointCount = GameObject.FindGameObjectsWithTag("Checkpoint").Length;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            finishTime = Timer.currentTime;
            GameManager.instance.UpdateBestTime(finishTime);
            Timer.StopTimer();
        }
    }

    public void UpdateCheckpointCount()
    {
        CurrentCheckpointCount++;
    }
}
