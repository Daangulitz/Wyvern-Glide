namespace YawVR
{
    /// <summary>
    /// Notified whenever the controller's connection state machine transitions.
    /// Segregated out of <see cref="IYawControllerDelegate"/> for consumers that only
    /// care about connection state (e.g. a status indicator) and nothing else.
    /// </summary>
    public interface IConnectionStateObserver
    {
        void ControllerStateChanged(ControllerState state);
    }
}
