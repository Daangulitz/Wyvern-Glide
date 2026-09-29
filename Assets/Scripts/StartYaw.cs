using UnityEngine;

namespace YawVR
{
    /// <summary>
    /// Starts the YawTracker device 5 seconds after Start
    /// </summary>
    public class StartYaw : MonoBehaviour
    {
        private void Start()
        {
            Invoke(nameof(StartDevice), 5f);
        }

        private void StartDevice()
        {
            Debug.Log("Starting YawTracker device");
            YawController.Instance.StartDevice();
        }
    }
}