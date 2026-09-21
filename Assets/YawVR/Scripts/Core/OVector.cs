using System;

namespace YawVR
{
    /// <summary>
    /// OVector is a Vector3D with yaw,pitch,roll named variables.
    /// </summary>
    [Serializable]
    public struct OVector
    {
        public float yaw, pitch, roll;

        public OVector(float yaw, float pitch, float roll)
        {
            this.yaw = yaw;
            this.pitch = pitch;
            this.roll = roll;
        }
    }
}
