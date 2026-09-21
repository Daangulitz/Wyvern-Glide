using System;

namespace YawVR
{
    /// <summary>
    /// Connection lifecycle: connect, start, stop, calibrate, disconnect.
    /// Segregated out of <see cref="IYawControllerType"/> so a consumer that only
    /// drives connection state doesn't need to depend on motion/discovery members too.
    /// </summary>
    public interface IYawConnectionControl
    {
        ControllerState State { get; }
        YawDevice Device { get; }

        void ConnectToDevice(YawDevice yawDevice, Action onSuccess, Action<string> onError);
        void DisconnectFromDevice(Action onSuccess, Action<string> onError);
        void StartDevice(Action onSuccess, Action<string> onError);
        void StopDevice(bool park, Action onSuccess, Action<string> onError);
        void CalibrateDevice(bool allAxis);
    }
}
