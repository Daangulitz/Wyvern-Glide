namespace YawVR
{
    /// <summary>
    /// Notified about devices appearing/disappearing on the network.
    /// Segregated out of <see cref="IYawControllerDelegate"/>.
    /// </summary>
    public interface IDeviceDiscoveryObserver
    {
        /// <summary>A device was found on the network.</summary>
        void DidFoundDevice(YawDevice device);

        /// <summary>The connection to a previously-connected device was lost unexpectedly.</summary>
        void DidDisconnectFrom(YawDevice device);
    }
}
