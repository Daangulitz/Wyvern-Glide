using UnityEngine;

namespace YawVR
{
    /// <summary>
    /// Starts the YawTracker device 5 seconds after Start
    /// </summary>
    public class StartYaw : MonoBehaviour
    {
        [SerializeField] private float startDelay = 3f;

        private void Start()
        {
            Invoke(nameof(StartDevice), startDelay);
        }

        private void StartDevice()
        {
            Debug.Log("Starting YawTracker device");
            YawController.Instance.StartDevice();
        }
    }
}