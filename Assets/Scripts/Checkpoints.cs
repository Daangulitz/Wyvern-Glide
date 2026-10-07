using UnityEngine;

public class HiddenCheckpoint : MonoBehaviour
{
    private Finish finish;

    private void Start()
    {
        finish = FindObjectOfType<Finish>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            finish.UpdateCheckpointCount();
        }
    }
}
