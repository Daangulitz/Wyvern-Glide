using UnityEngine;

namespace YawVR {
    /// <summary>
    /// Cancels the camera's rotation based on IMU data
    /// </summary>
    public class MotionCompensation : MonoBehaviour
    {
        [SerializeField] private Transform cameraOffsetTransform;
        [SerializeField] private YawController yawController;
        [SerializeField] [Range(0f,0.9f)] private float smoothing = 0.7f;
        
        private Vector3 simData;
        private Vector3 offset;
        
        private float imuYawOffset = 0f;

        public void UpdateOffset() {
            offset.y = simData.y;
        }

        private void LateUpdate()
        {
            Debug.Log($"IMU yaw: {yawController.Device.ActualPosition.yaw}");

            if (YawController.Instance().State != ControllerState.Started &&
                YawController.Instance().State != ControllerState.Connected) return;
            
            simData.y = -yawController.Device.ActualPosition.yaw;

            if (cameraOffsetTransform != null) {
                cameraOffsetTransform.rotation = Quaternion.Slerp(cameraOffsetTransform.rotation,Quaternion.Euler(simData - offset),1f-smoothing);   
            }
            
            /*if (YawController.Instance().State != ControllerState.Started &&
                YawController.Instance().State != ControllerState.Connected)
                return;

            float currentIMUYaw = yawController.Device.ActualPosition.yaw;

            // Calculate how much the chair has turned since the offset
            float compensatedYaw = Mathf.DeltaAngle(imuYawOffset, currentIMUYaw);

            // Cancel out that yaw to keep the player stable inside the buggy
            Quaternion targetRotation = Quaternion.Euler(0f, -compensatedYaw, 0f);

            cameraOffsetTransform.localRotation = Quaternion.Slerp(
                cameraOffsetTransform.localRotation,
                targetRotation,
                1f - smoothing
            );*/
        }
    }
}
