namespace YawVR
{
    /// <summary>
    /// Full controller surface, composed from the segregated interfaces above.
    /// Kept for backward compatibility with anything written against the whole
    /// surface; new code should depend on the narrower interface it actually needs
    /// (<see cref="IYawConnectionControl"/>, <see cref="IYawDeviceDiscovery"/>,
    /// <see cref="IYawMotionConfiguration"/>) instead of this one.
    /// </summary>
    public interface IYawControllerType : IYawConnectionControl, IYawDeviceDiscovery, IYawMotionConfiguration
    {
        IYawControllerDelegate ControllerDelegate { get; set; }
        void SetGameName(string gameName);
    }
}
