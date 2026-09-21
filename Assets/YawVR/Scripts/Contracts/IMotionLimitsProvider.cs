using UnityEngine;

namespace YawVR
{
    /// <summary>
    /// What <see cref="YawTracker"/> needs from its host in order to apply multiplier
    /// and limit shaping to incoming orientation data. Segregated out of the full
    /// controller surface so YawTracker depends on this narrow abstraction (DIP)
    /// instead of the concrete <see cref="YawController"/>.
    /// </summary>
    public interface IMotionLimitsProvider
    {
        Vector3 RotationMultiplier { get; }
        Limits Limits { get; }
    }
}
