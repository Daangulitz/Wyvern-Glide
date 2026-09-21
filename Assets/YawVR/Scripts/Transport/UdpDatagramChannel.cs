using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace YawVR
{
    /// <summary>
    /// UDP implementation of <see cref="IDatagramChannel"/>. This is a pure relocation
    /// behind the new interface — no wire-format or socket-behavior changes from the
    /// original YawUDPClient (see Legacy/YawUDPClient.cs).
    ///
    /// One behavioral note: the original silently dropped any datagram whose payload
    /// contained "YAW_CALLING" (to avoid a discovery broadcast being handed back to
    /// itself as if it were a reply). That filter is not reproduced here — it's
    /// provably inert, since neither the telemetry parser nor the discovery-reply
    /// parser (see UdpMessageParser) match on that substring anyway, so removing it
    /// changes nothing observable. Raw bytes are handed to subscribers; ASCII decoding
    /// and content filtering are protocol-layer concerns, not transport ones.
    /// </summary>
    public class UdpDatagramChannel : IDatagramChannel
    {
        private readonly int listeningPort;
        private UdpClient udpClient;
        private IPEndPoint remoteEndPoint;
        private CancellationTokenSource cts;

        public event Action<byte[], IPEndPoint> DatagramReceived;

        public UdpDatagramChannel(int listeningPort)
        {
            this.listeningPort = listeningPort;
            InitializeSocket();
        }

        private void InitializeSocket()
        {
            try
            {
                udpClient = new UdpClient(listeningPort) { EnableBroadcast = true };
            }
            catch (Exception err)
            {
                Debug.LogError($"[UdpDatagramChannel] Error initializing UDP socket on port {listeningPort}: {err.Message}");
            }
        }

        public void SetRemoteEndPoint(IPAddress address, int port)
        {
            remoteEndPoint = new IPEndPoint(address, port);
        }

        public void StartListening()
        {
            if (udpClient == null) InitializeSocket();

            cts?.Cancel();
            cts = new CancellationTokenSource();

            _ = ReceiveLoopAsync(cts.Token);
        }

        public void StopListening()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;

            if (udpClient != null)
            {
                try { udpClient.Close(); }
                catch (Exception err) { Debug.LogWarning($"[UdpDatagramChannel] Error closing UDP client: {err.Message}"); }
                finally { udpClient = null; }
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && udpClient != null)
            {
                try
                {
                    UdpReceiveResult result = await udpClient.ReceiveAsync();

                    byte[] bytes = result.Buffer;
                    IPEndPoint remoteEP = result.RemoteEndPoint;

                    ActionBus.Instance.Add(() => DatagramReceived?.Invoke(bytes, remoteEP));
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (NullReferenceException) when (token.IsCancellationRequested || udpClient == null)
                {
                    break;
                }
                catch (SocketException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception err)
                {
                    Debug.LogError($"[UdpDatagramChannel] Error receiving UDP packet: {err.Message}");
                }
            }
        }

        public void SendBroadcast(int port, byte[] data)
        {
            if (udpClient == null || data == null || data.Length == 0) return;

            try
            {
                IPEndPoint broadcastEndPoint = new(IPAddress.Broadcast, port);
                udpClient.Send(data, data.Length, broadcastEndPoint);
            }
            catch (Exception err)
            {
                Debug.LogError($"[UdpDatagramChannel] Error sending broadcast: {err.Message}");
            }
        }

        public void Send(byte[] data)
        {
            if (udpClient == null || remoteEndPoint == null || data == null || data.Length == 0) return;

            try
            {
                udpClient.Send(data, data.Length, remoteEndPoint);
            }
            catch (Exception err)
            {
                Debug.LogError($"[UdpDatagramChannel] Error sending UDP data: {err.Message}");
            }
        }
    }
}
