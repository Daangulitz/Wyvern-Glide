using System;

namespace YawVR
{
    /// <summary>
    /// Abstraction over a reliable, ordered, connection-oriented channel (TCP today).
    /// High-level SDK logic (see <see cref="YawConnectionManager"/>) depends on this
    /// instead of a concrete socket implementation, so the transport can be swapped
    /// (a different protocol, a mock for tests, a future reliable-UDP layer) without
    /// touching connection/device logic. See <see cref="TcpReliableConnection"/>
    /// for the current implementation.
    /// </summary>
    public interface IReliableConnection
    {
        bool IsConnected { get; }

        void Connect(string host, int port, Action onSuccess, Action<string> onError);
        void Disconnect();
        void Send(byte[] data);

        /// <summary>Raised on the Unity main thread for every message received.</summary>
        event Action<byte[]> MessageReceived;

        /// <summary>
        /// Raised on the Unity main thread when the connection is lost unexpectedly.
        /// Must NOT be raised as a result of a caller-initiated <see cref="Disconnect"/>.
        /// </summary>
        event Action Disconnected;
    }
}
