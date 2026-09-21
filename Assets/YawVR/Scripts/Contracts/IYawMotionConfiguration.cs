namespace YawVR
{
    /// <summary>
    /// Motion shaping (multiplier/limits) plus buzzer state. Segregated out of
    /// <see cref="IYawControllerType"/>; extends <see cref="IMotionLimitsProvider"/>
    /// so the two never drift apart.
    /// </summary>
    public interface IYawMotionConfiguration : IMotionLimitsProvider
    {
        Buzzer Buzzer { get; }
        void SetTiltLimits(float yawLimit, float pitchLimit, float rollLimit);
        void SetRotationMultiplier(float yaw, float pitch, float roll);
    }
}
