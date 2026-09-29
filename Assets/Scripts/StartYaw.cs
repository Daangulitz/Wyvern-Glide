using UnityEngine;

namespace YawVR
{
    /// <summary>
    /// Starts the YawTracker device on start
    /// </summary>
    public class StartYaw : MonoBehaviour
    {
        private void Start()
        {
            YawController.Instance.StartDevice();
        }
    }
}