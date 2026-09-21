using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace YawVR
{
    /// <summary>
    /// TCP implementation of <see cref="IReliableConnection"/> for the YAW 3's control
    /// channel. Behaviourally equivalent to the original YawTCPClient (see
    /// Legacy/YawTCPClient.cs), except for three fixes applied during the SOLID
    /// refactor — all pure SDK-side correctness fixes with no wire-format impact:
    ///
    ///   1. Disconnect detection is no longer inverted. The original checked
    ///      `if (!connected)` in the read loop's finally block, which meant an
    ///      unexpected drop (cable pulled, sim powered off) never raised the
    ///      disconnect callback, while a caller-requested disconnect always did
    ///      (backwards from what both call sites wanted). Here, a caller-requested
    ///      <see cref="Disconnect"/> sets <see cref="disconnectRequested"/> first, and
    ///      <see cref="Disconnected"/> only fires when the loop exits WITHOUT that flag set.
    ///   2. Outbound writes are serialized through a queue + single background sender
    ///      thread instead of unsynchronized concurrent `async void` sends, which could
    ///      interleave bytes from two commands issued in the same frame.
    ///   3. TCP_NODELAY is enabled, so single-byte heartbeat/command writes are not
    ///      held back by Nagle's algorithm.
    ///
    /// NOT fixed here (needs a firmware-side wire-format change first — see
    /// firmware-todo.md): inbound messages are still handed to
    /// <see cref="MessageReceived"/> exactly as they arrive from one ReadAsync call,
    /// with no length-prefix framing. Two back-to-back replies can still coalesce into
    /// one callback, and one reply can still be split across two callbacks, if the
    /// firmware doesn't send a length/delimiter the SDK can key off.
    /// </summary>
    public class TcpReliableConnection : IReliableConnection
    {
        private TcpClient tcpClient;
        private CancellationTokenSource cts;
        private readonly BlockingCollection<byte[]> sendQueue = new();
        private Thread sendThread;
        private bool disconnectRequested;

        public bool IsConnected => tcpClient != null && tcpClient.Connected;

        public event Action<byte[]> MessageReceived;
        public event Action Disconnected;

        public async void Connect(string host, int port, Action onSuccess, Action<string> onError)
        {
            Disconnect();
            while (sendQueue.TryTake(out _)) { } // drop anything queued from a previous session

            disconnectRequested = false;
            cts = new CancellationTokenSource();

            try
            {
                tcpClient = new TcpClient { NoDelay = true };
                IPAddress ipAddress = IPAddress.Parse(host);

                await tcpClient.ConnectAsync(ipAddress, port);

                if (tcpClient.Connected)
                {
                    Debug.Log($"[TcpReliableConnection] Connected to: {host}:{port}");

                    sendThread = new Thread(() => SendLoop(cts.Token)) { IsBackground = true, Name = "YawTcpSend" };
                    sendThread.Start();
                    _ = ReadLoopAsync(cts.Token);

                    ActionBus.Instance.Add(() => onSuccess?.Invoke());
                }
                else
                {
                    FailConnect("Unable to connect to TCP server.", onError);
                }
            }
            catch (Exception ex)
            {
                FailConnect($"Connection failed: {ex.Message}", onError);
            }
        }

        public void Disconnect()
        {
            disconnectRequested = true;
            CloseSocket();
        }

        public void Send(byte[] data)
        {
            if (data == null || data.Length == 0 || tcpClient == null || !tcpClient.Connected) return;
            sendQueue.Add(data);
        }

        private void SendLoop(CancellationToken token)
        {
            try
            {
                NetworkStream ns = tcpClient.GetStream();
                foreach (byte[] data in sendQueue.GetConsumingEnumerable(token))
                {
                    ns.Write(data, 0, data.Length);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogError($"[TcpReliableConnection] Error sending data: {ex.Message}");
            }
        }

        private async Task ReadLoopAsync(CancellationToken token)
        {
            var buffer = new byte[4096];
            bool lostUnexpectedly = false;

            try
            {
                NetworkStream ns = tcpClient.GetStream();

                while (!token.IsCancellationRequested && tcpClient.Connected)
                {
                    int bytesRead = await ns.ReadAsync(buffer, 0, buffer.Length, token);
                    if (bytesRead > 0)
                    {
                        byte[] data = new byte[bytesRead];
                        Array.Copy(buffer, data, bytesRead);

                        ActionBus.Instance.Add(() => MessageReceived?.Invoke(data));
                    }
                    else
                    {
                        lostUnexpectedly = !disconnectRequested;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                if (!disconnectRequested)
                {
                    lostUnexpectedly = true;
                    Debug.LogWarning($"[TcpReliableConnection] Read error: {ex.Message}");
                }
            }
            finally
            {
                if (lostUnexpectedly)
                {
                    CloseSocket();
                    ActionBus.Instance.Add(() => Disconnected?.Invoke());
                }
            }
        }

        private void FailConnect(string message, Action<string> onError)
        {
            CloseSocket();
            ActionBus.Instance.Add(() => onError?.Invoke(message));
        }

        private void CloseSocket()
        {
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }

            if (tcpClient != null)
            {
                try { tcpClient.Close(); }
                catch (Exception ex) { Debug.Log($"[TcpReliableConnection] Error closing client: {ex.Message}"); }
                finally { tcpClient = null; }
            }
        }
    }
}
