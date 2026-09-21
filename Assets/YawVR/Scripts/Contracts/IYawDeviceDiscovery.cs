namespace YawVR
{
    /// <summary>
    /// Broadcasting for nearby devices. Segregated out of <see cref="IYawControllerType"/>
    /// so UI that only lists/searches devices doesn't need the full controller surface.
    /// </summary>
    public interface IYawDeviceDiscovery
    {
        void DiscoverDevices(int onPort);
    }
}
