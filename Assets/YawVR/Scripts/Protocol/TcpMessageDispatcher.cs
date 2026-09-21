using System;
using System.Collections.Generic;

namespace YawVR
{
    /// <summary>
    /// Routes an inbound TCP message to a registered handler by its first byte
    /// (command id). This is the OCP seam for the TCP protocol: a new command id is
    /// supported by registering a new handler (see <see cref="YawConnectionManager"/>),
    /// not by editing a growing switch statement.
    /// </summary>
    public class TcpMessageDispatcher
    {
        private readonly Dictionary<byte, Action<byte[]>> handlers = new();

        public void Register(byte commandId, Action<byte[]> handler)
        {
            handlers[commandId] = handler;
        }

        public void Dispatch(byte[] data)
        {
            if (data == null || data.Length == 0) return;
            if (handlers.TryGetValue(data[0], out var handler)) handler(data);
        }
    }
}
