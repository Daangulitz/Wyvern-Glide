using UnityEngine;

public class Finish : MonoBehaviour
{
    private static Finish instance;
    private float finishTime;

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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            finishTime = Timer.currentTime;
            GameManager.instance.UpdateBestTime(finishTime);
            Timer.StopTimer();
        }
    }
}
