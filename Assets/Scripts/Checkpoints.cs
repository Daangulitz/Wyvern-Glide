using UnityEngine;

public class Checkpoints : MonoBehaviour
{
    private GameManager GM;

    private void Start()
    {
        GM = FindObjectOfType<GameManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GM.UpdateCheckpointCount();
        }
    }
}
