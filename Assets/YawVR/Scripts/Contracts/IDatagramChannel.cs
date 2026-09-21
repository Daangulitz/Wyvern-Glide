using System;
using System.Net;

namespace YawVR
{
    /// <summary>
    /// Abstraction over an unreliable, connectionless datagram channel (UDP today).
    /// Shared by <see cref="YawConnectionManager"/> (telemetry), the device discovery
    /// service, and the motion data pump, so all three can be pointed at a single
    /// underlying socket without depending on its concrete type.
    /// See <see cref="UdpDatagramChannel"/> for the current implementation.
    /// </summary>
    public interface IDatagramChannel
    {
        void StartListening();
        void StopListening();

        void SetRemoteEndPoint(IPAddress address, int port);
        void Send(byte[] data);
        void SendBroadcast(int port, byte[] data);

        /// <summary>Raised on the Unity main thread for every datagram received.</summary>
        event Action<byte[], IPEndPoint> DatagramReceived;
    }
}
