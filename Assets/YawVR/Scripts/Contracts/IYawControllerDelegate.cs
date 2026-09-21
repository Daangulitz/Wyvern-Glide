namespace YawVR
{
    /// <summary>
    /// The script that needs to receive notifications from the controller should
    /// implement this. Composed from the smaller observer interfaces above so a
    /// consumer that only cares about one slice of notifications can implement just
    /// that interface instead of this whole one; existing implementers of this
    /// interface (all five methods) are unaffected and keep compiling unchanged.
    /// </summary>
    public interface IYawControllerDelegate : IConnectionStateObserver, IDeviceDiscoveryObserver, IRemoteControlObserver
    {
    }
}
