using UnityEngine;

namespace YawVR
{
    /// <summary>
    /// Modifies and stores orientation data before sending to the simulator: applies
    /// rotation multiplier and tilt-limit shaping to incoming orientation before
    /// writing it to this transform.
    ///
    /// Depends on <see cref="IMotionLimitsProvider"/> (DIP) instead of the concrete
    /// <see cref="YawController"/> — resolution strategy is unchanged (still looks for
    /// a YawController on the parent, falling back to the singleton), since
    /// YawController happens to implement the interface.
    /// </summary>
    public class YawTracker : MonoBehaviour
    {
        private IMotionLimitsProvider limitsProvider;

        private void Awake()
        {
            limitsProvider = GetComponentInParent<YawController>();
            if (limitsProvider == null) limitsProvider = YawController.Instance;
        }

        /// <summary>
        /// Sets the YawTracker's orientation, multiplier and limits are applied here
        /// </summary>
        public void SetRotation(Vector3 rot)
        {
            if (limitsProvider == null) return;

            rot = SignedVector(rot);
            rot = ApplyMultipliers(rot);
            rot = ApplyLimits(rot);

            transform.eulerAngles = rot;
        }

        private Vector3 ApplyMultipliers(Vector3 rot)
        {
            return Vector3.Scale(rot, limitsProvider.RotationMultiplier);
        }

        private Vector3 ApplyLimits(Vector3 rot)
        {
            Limits limits = limitsProvider.Limits;

            if (limits.pitch != -1) rot.x = Mathf.Clamp(rot.x, -limits.pitch, limits.pitch);
            if (limits.yaw != -1) rot.y = Mathf.Clamp(rot.y, -limits.yaw, limits.yaw);
            if (limits.roll != -1) rot.z = Mathf.Clamp(rot.z, -limits.roll, limits.roll);

            return rot;
        }

        private Vector3 SignedVector(Vector3 v)
        {
            v.x = Mathf.DeltaAngle(0, v.x);
            v.y = Mathf.DeltaAngle(0, v.y);
            v.z = Mathf.DeltaAngle(0, v.z);

            return v;
        }
    }
}
