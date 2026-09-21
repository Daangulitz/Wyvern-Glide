namespace YawVR
{
    /// <summary>
    /// Notified when the device is started/stopped by something other than this app
    /// (e.g. the device's own physical controls, or another app). Segregated out of
    /// <see cref="IYawControllerDelegate"/>.
    /// </summary>
    public interface IRemoteControlObserver
    {
        void DeviceStoppedFromApp();
        void DeviceStartedFromApp();
    }
}
