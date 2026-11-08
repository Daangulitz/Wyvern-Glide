using UnityEngine;

public class Finish : MonoBehaviour
{
    private bool _finished = false;

    private void OnTriggerEnter(Collider other)
    {
        // Only once, even if several player colliders touch the trigger.
        if (_finished || !other.CompareTag("Player")) return;
        _finished = true;

        float finishTime = Timer.currentTime;
        GameManager.instance.UpdateBestTime(finishTime);
        Timer.StopTimer();

        Debug.Log($"Finish bereikt! Tijd: {finishTime:F2} seconden");

        // Sends the result to the database, parks the Yaw and resets the whole game for the next player.
        GameManager.instance.EndGame();
    }
}