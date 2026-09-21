using System;
using System.Collections;
using System.Net;
using System.Text;
using UnityEngine;

namespace YawVR
{
    /// <summary>
    /// Owns the TCP control-channel lifecycle — connect / check-in / start / stop /
    /// calibrate / disconnect — the heartbeat poll, and applying inbound UDP telemetry
    /// to the current device. Extracted from the old YawController god object; depends
    /// only on abstractions (<see cref="IReliableConnection"/>, <see cref="IDatagramChannel"/>,
    /// <see cref="ICoroutineRunner"/>), not on Unity lifecycle or a concrete transport.
    ///
    /// TCP replies are routed through a <see cref="TcpMessageDispatcher"/> registered
    /// in the constructor (OCP: a new command id is supported by registering a new
    /// handler here, not by editing a switch statement).
    /// </summary>
    public class YawConnectionManager
    {
        private readonly IReliableConnection connection;
        private readonly IDatagramChannel dataChannel;
        private readonly ICoroutineRunner coroutineRunner;
        private readonly TcpMessageDispatcher dispatcher = new();
        private readonly int udpClientPort;

        private string gameName;
        private YawDevice device;
        private ControllerState state = ControllerState.Initial;

        private CallBacks callBacks;
        private CallbackTimeouts callbackTimeouts;

        public ControllerState State => state;
        public YawDevice Device => device;

        public event Action<ControllerState> StateChanged;

        /// <summary>Fires unconditionally whenever a CHECK_IN_ANS reply arrives (mirrors
        /// the original's unconditional `Invoke(nameof(UpdateIMUOffset), 0.1f)` placement,
        /// even before checking whether the check-in was accepted).</summary>
        public event Action CheckInReplyReceived;

        /// <summary>Fires when a check-in is accepted (device available), before the
        /// state flips to Connected — matches the original ordering exactly.</summary>
        public event Action Connected;

        /// <summary>Fires on both a caller-requested disconnect and an unexpected
        /// connection loss — matches the original's single onDisconnected UnityEvent.</summary>
        public event Action Disconnected;

        /// <summary>Fires only when the connection is lost unexpectedly (never on a
        /// caller-requested disconnect) — maps to IYawControllerDelegate.DidDisconnectFrom.</summary>
        public event Action<YawDevice> DeviceConnectionLost;

        public event Action DeviceStartedFromApp;
        public event Action DeviceStoppedFromApp;
        public event Action<DeviceState> DeviceReportedStateChanged;

        public YawConnectionManager(IReliableConnection connection, IDatagramChannel dataChannel, ICoroutineRunner coroutineRunner, int udpClientPort)
        {
            this.connection = connection;
            this.dataChannel = dataChannel;
            this.coroutineRunner = coroutineRunner;
            this.udpClientPort = udpClientPort;

            connection.MessageReceived += dispatcher.Dispatch;
            connection.Disconnected += HandleUnexpectedDisconnect;
            dataChannel.DatagramReceived += HandleDatagram;

            RegisterTcpHandlers();
        }

        public void SetGameName(string name) => gameName = name;

        public void Connect(YawDevice yawDevice, Action onSuccess, Action<string> onError)
        {
            if (state != ControllerState.Initial)
            {
                DisconnectFromDevice(
                    () => Connect(yawDevice, onSuccess, onError),
                    error => { onError?.Invoke(error); Connect(yawDevice, onSuccess, onError); });
                return;
            }

            SetState(ControllerState.Connecting);

            callbackTimeouts.tcpConnectionAttemptTimeout = coroutineRunner.RunCoroutine(ResponseTimeout(error =>
            {
                onError?.Invoke("Failed to create TCP connection");
                SetState(ControllerState.Initial);
                connection.Disconnect();
            }));

            connection.Connect(yawDevice.IPAddress.ToString(), yawDevice.TCPPort,
                onSuccess: () =>
                {
                    coroutineRunner.StopCoroutineIfRunning(ref callbackTimeouts.tcpConnectionAttemptTimeout);
                    device = yawDevice;

                    callBacks.connectingError = onError;
                    callBacks.connectingSuccess = onSuccess;

                    callbackTimeouts.connectingTimeout = coroutineRunner.RunCoroutine(ResponseTimeout(error =>
                    {
                        onError?.Invoke(error);
                        SetState(ControllerState.Initial);
                    }));

                    connection.Send(CommandEncoder.CheckIn(udpClientPort, gameName));
                    coroutineRunner.RunCoroutine(HeartbeatLoop());
                },
                onError: error =>
                {
                    coroutineRunner.StopCoroutineIfRunning(ref callbackTimeouts.tcpConnectionAttemptTimeout);
                    onError?.Invoke(error);
                    SetState(ControllerState.Initial);
                });
        }

        public void StartDevice(Action onSuccess, Action<string> onError)
        {
            if (state == ControllerState.Connected)
            {
                callBacks.startSuccess = onSuccess;
                callBacks.startError = onError;
                callbackTimeouts.startTimeout = coroutineRunner.RunCoroutine(ResponseTimeout(onError));
                SetState(ControllerState.Starting);
                connection.Send(CommandEncoder.START);
            }
            else onError?.Invoke("Attempted to start device when device has not been in connected ready state");
        }

        public void StopDevice(bool park, Action onSuccess, Action<string> onError)
        {
            if (state == ControllerState.Started)
            {
                callBacks.stopSuccess = onSuccess;
                callBacks.stopError = onError;
                callbackTimeouts.stopTimeout = coroutineRunner.RunCoroutine(ResponseTimeout(onError));
                SetState(ControllerState.Stopping);
                connection.Send(new byte[] { CommandEncoder.STOP, (byte)(park ? 1 : 0) });
            }
            else onError?.Invoke("Attempted to stop simulator when simulator had not been in started state");
        }

        public void CalibrateDevice(bool allAxis)
        {
            if (state == ControllerState.Connected)
            {
                connection.Send(new byte[2] { CommandEncoder.CALIBRATE_TEMPLATE[0], (byte)(allAxis ? 1 : 0) });
            }
        }

        public void DisconnectFromDevice(Action onSuccess, Action<string> onError)
        {
            if (state != ControllerState.Initial)
            {
                callBacks.exitSuccess = onSuccess;
                callBacks.exitError = onError;

                callbackTimeouts.exitTimeout = coroutineRunner.RunCoroutine(ResponseTimeout(error =>
                {
                    SetState(ControllerState.Initial);
                    onError?.Invoke(error);
                }));

                connection.Send(CommandEncoder.EXIT);
                SetState(ControllerState.Disconnecting);
                Disconnected?.Invoke();
            }
            else onError?.Invoke("Attempted to disconnect when no device was connected");
        }

        private void SetState(ControllerState newState)
        {
            state = newState;
            if (newState == ControllerState.Initial)
            {
                device = null;
                if (connection.IsConnected) connection.Disconnect();
            }
            StateChanged?.Invoke(newState);
        }

        private void HandleUnexpectedDisconnect()
        {
            YawDevice lostDevice = device;
            Disconnected?.Invoke();
            Debug.Log("[YawConnectionManager] TCP connection lost unexpectedly.");
            DeviceConnectionLost?.Invoke(lostDevice);
            SetState(ControllerState.Initial);
        }

        private void HandleDatagram(byte[] bytes, IPEndPoint remoteEndPoint)
        {
            if (device == null) return;
            string message = Encoding.ASCII.GetString(bytes);
            UdpMessageParser.TryApplyTelemetry(message, device);
        }

        private void RegisterTcpHandlers()
        {
            dispatcher.Register(CommandIds.CHECK_IN_ANS, HandleCheckInAnswer);
            dispatcher.Register(CommandIds.START, HandleStartReply);
            dispatcher.Register(CommandIds.STOP, HandleStopReply);
            dispatcher.Register(CommandIds.EXIT, HandleExitReply);
            dispatcher.Register(CommandIds.SET_POWER, HandleSetPowerReply);
            dispatcher.Register(CommandIds.GET_STATE, HandleGetStateReply);
            dispatcher.Register(CommandIds.GET_TEMPS, HandleGetTempsReply);
        }

        private void HandleCheckInAnswer(byte[] data)
        {
            CheckInReplyReceived?.Invoke();
            coroutineRunner.StopCoroutineIfRunning(ref callbackTimeouts.connectingTimeout);

            if (state != ControllerState.Connecting) return;

            string message = Encoding.ASCII.GetString(data, 1, data.Length - 1);
            if (message.Contains("AVAILABLE"))
            {
                Connected?.Invoke();

                dataChannel.SetRemoteEndPoint(device.IPAddress, device.UDPPort);
                SetState(ControllerState.Connected);

                callBacks.connectingSuccess?.Invoke();
                ClearCallbacks(ref callBacks.connectingSuccess, ref callBacks.connectingError);
            }
            else
            {
                var messageParts = message.Split(';');
                if (messageParts.Length != 3) return;

                SetState(ControllerState.Initial);
                callBacks.connectingError?.Invoke($"Device is in use from: {messageParts[2]} with game: {messageParts[1]}");
                ClearCallbacks(ref callBacks.connectingSuccess, ref callBacks.connectingError);
            }
        }

        private void HandleStartReply(byte[] data)
        {
            coroutineRunner.StopCoroutineIfRunning(ref callbackTimeouts.startTimeout);

            if (state == ControllerState.Starting)
            {
                SetState(ControllerState.Started);
                if (callBacks.startSuccess != null)
                {
                    callBacks.startSuccess();
                    callBacks.startSuccess = null;
                    callBacks.startError = null;
                }
            }
            else
            {
                SetState(ControllerState.Started);
                DeviceStartedFromApp?.Invoke();
            }
        }

        private void HandleStopReply(byte[] data)
        {
            coroutineRunner.StopCoroutineIfRunning(ref callbackTimeouts.stopTimeout);

            if (state != ControllerState.Initial && state != ControllerState.Disconnecting)
            {
                SetState(ControllerState.Connected);
                if (callBacks.stopSuccess != null)
                {
                    callBacks.stopSuccess();
                    callBacks.stopSuccess = null;
                    callBacks.stopError = null;
                }
                else
                {
                    // NOTE: the original called SetState(Connected) here a second time
                    // in a row (already set above), which double-fires StateChanged
                    // with the same state on this specific path (device stopped from
                    // the app itself, not from our own StopDevice call). That's almost
                    // certainly a copy-paste artifact, but it wasn't in the approved
                    // fix list for this refactor, so it's preserved exactly rather than
                    // silently dropped. See firmware-todo.md / summary if you want it removed.
                    SetState(ControllerState.Connected);
                    DeviceStoppedFromApp?.Invoke();
                }
            }
        }

        private void HandleExitReply(byte[] data)
        {
            coroutineRunner.StopCoroutineIfRunning(ref callbackTimeouts.exitTimeout);

            SetState(ControllerState.Initial);
            if (callBacks.exitSuccess != null)
            {
                callBacks.exitSuccess();
                callBacks.exitSuccess = null;
                callBacks.exitError = null;
            }
        }

        private void HandleSetPowerReply(byte[] data)
        {
            if (device == null) return;

            if (data.Length < 5)
            {
                Debug.LogWarning("[YawConnectionManager] SET_POWER reply too short, dropping.");
                return;
            }

            if (data.Length > 5)
            {
                if (data.Length < 25)
                {
                    Debug.LogWarning("[YawConnectionManager] SET_POWER param-dump reply truncated, dropping.");
                    return;
                }

                device.deviceParams.Power = data[4];
                device.deviceParams.PitchLimitF = data[13];
                device.deviceParams.PitchLimitB = data[9];
                device.deviceParams.RollLimit = data[17];
                device.deviceParams.YawLimit = ByteConversion.BytesToUInt32(data, 19, littleEndian: false);
                device.deviceParams.hasYawLimit = data[24] == 1;
            }
            else
            {
                device.deviceParams.Power = data[4];
            }
        }

        private void HandleGetStateReply(byte[] data)
        {
            if (device == null) return;

            if (data.Length < 2)
            {
                Debug.LogWarning("[YawConnectionManager] GET_STATE reply too short, dropping.");
                return;
            }

            string stateString = Encoding.ASCII.GetString(data, 2, data.Length - 2).Trim();
            DeviceState newState = DeviceState.STOPPED;
            switch (stateString)
            {
                case "disabled": newState = DeviceState.STOPPED; break;
                case "simulation mode": newState = DeviceState.STARTED; break;
                case "emergency mode": newState = DeviceState.NOTRACKER; break;
                case "parking": newState = DeviceState.PARKING; break;
            }

            if (device.State != newState) DeviceReportedStateChanged?.Invoke(newState);
            device.State = newState;
        }

        private void HandleGetTempsReply(byte[] data)
        {
            if (device == null) return;

            if (data.Length < 4)
            {
                Debug.LogWarning("[YawConnectionManager] GET_TEMPS reply too short, dropping.");
                return;
            }

            device.temps[0] = data[1];
            device.temps[1] = data[2];
            device.temps[2] = data[3];
        }

        private IEnumerator HeartbeatLoop()
        {
            var wait = new WaitForSeconds(1f);
            yield return wait;
            connection.Send(new byte[] { CommandIds.GET_ALL_APP_PARAMS });
            while (connection.IsConnected)
            {
                connection.Send(new byte[] { CommandIds.GET_STATE });
                connection.Send(new byte[] { CommandIds.GET_TEMPS });
                yield return wait;
            }
        }

        private IEnumerator ResponseTimeout(Action<string> onError)
        {
            if (onError == null) yield break;
            yield return new WaitForSeconds(10f);
            onError("Command timeout");
        }

        private void ClearCallbacks(ref Action success, ref Action<string> error)
        {
            success = null;
            error = null;
        }

        private struct CallBacks
        {
            public Action connectingSuccess;
            public Action<string> connectingError;
            public Action startSuccess;
            public Action<string> startError;
            public Action stopSuccess;
            public Action<string> stopError;
            public Action exitSuccess;
            public Action<string> exitError;
        }

        private struct CallbackTimeouts
        {
            public Coroutine connectingTimeout;
            public Coroutine startTimeout;
            public Coroutine stopTimeout;
            public Coroutine exitTimeout;
            public Coroutine tcpConnectionAttemptTimeout;
        }
    }
}
