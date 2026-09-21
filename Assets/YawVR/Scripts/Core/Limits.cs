using System;

namespace YawVR
{
    /// <summary>
    /// Game limits. The limits are applied to the YawVR Tracker.
    /// </summary>
    [Serializable]
    public class Limits
    {
        public float yaw = -1, pitch = -1, roll = -1;

        public Limits(float yaw, float pitch, float roll)
        {
            this.yaw = yaw;
            this.pitch = pitch;
            this.roll = roll;
        }
    }
}
