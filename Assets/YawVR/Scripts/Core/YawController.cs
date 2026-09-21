using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Events;

namespace YawVR
{
    /// <summary>
    /// Composition root and public-API facade for the YawVR SDK.
    ///
    /// This class used to contain the entire SDK — networking, wire-protocol parsing,
    /// device state, motion pumping, discovery — in one ~800-line file implementing
    /// TCP/UDP delegate callbacks directly. It has been decomposed into collaborator
    /// services (<see cref="YawConnectionManager"/>, <see cref="YawDeviceDiscoveryService"/>,
    /// <see cref="MotionDataPump"/>) that depend only on abstractions
    /// (see Contracts/, Transport/, Protocol/). This class now just constructs those
    /// services, wires their events to the existing public API (IYawControllerDelegate,
    /// the serialized UnityEvents, the static OnConnectReceivers list), and hosts what
    /// genuinely has to live on a MonoBehaviour: the serialized inspector fields, the
    /// Unity lifecycle methods, and coroutine hosting (via <see cref="ICoroutineRunner"/>,
    /// for the plain C# services above, which are not MonoBehaviours themselves).
    ///
    /// The public API (Instance, TrackerObject, SendLED, Buzzer, Device,
    /// SetRotationMultiplier, SetTiltLimits, IYawControllerDelegate, ...) — and every
    /// serialized field a designer may have configured on the YawController prefab —
    /// is unchanged by this refactor. See the refactor summary for the exact surface
    /// verified against every consumer in the project (Sample/*.cs and the UI panels).
    /// </summary>
    public class YawController : MonoBehaviour, IYawControllerType, ICoroutineRunner
    {
        private static YawController instance;
        public static List<Action> OnConnectReceivers = new();

        // Kept only so the Inspector can show the current device during Play mode, and
        // so this component doesn't lose a serialized slot; the actual source of truth
        // is connectionManager.Device (see the Device property below), kept in sync in
        // HandleStateChanged.
        [SerializeField] private YawDevice device = null;

        private IReliableConnection connection;
        private IDatagramChannel dataChannel;
        private YawConnectionManager connectionManager;
        private YawDeviceDiscoveryService discoveryService;
        private MotionDataPump motionPump;

        private Orientation orientation;
        private YawTracker yawTracker;

        #region PROPERTIES
        public static YawController Instance
        {
            get
            {
                if (instance == null) throw new Exception("[YawController] Please drag YawController prefab into your scene.");
                return instance;
            }
        }

        public YawTracker TrackerObject => yawTracker;
        public ControllerState State => connectionManager.State;
        public YawDevice Device => connectionManager.Device;
        public IYawControllerDelegate ControllerDelegate { get; set; }
        public Vector3 RotationMultiplier => rotationMultiplier;
        public Limits Limits => gameLimits;
        public Buzzer Buzzer => buzzer;
        #endregion

        [SerializeField] private Transform referenceTransform; // we will copy this objects rotation, and send it to the sim
        [SerializeField] private string gameName; // name of the game
        [SerializeField] private ConnectType connectType; // connect type, for debug purposes
        [SerializeField] private string debug_ipAddress; // ip to connect in debug mode
        [SerializeField] private int udpClientPort;

        private OVector referenceRotation; // the rotation of the YAWTracker

        [SerializeField] private Vector3 rotationMultiplier = new(1, 1, 1); // multiplier for YAWTracker
        [SerializeField] private Limits gameLimits;
        [SerializeField] private Buzzer buzzer;
        [SerializeField] private byte smartPlug;

        [Header("Camera Cancellation")]
        [SerializeField] private MotionCompensation cancellation;

        [Header("Events")]
        [SerializeField] private UnityEvent onConnected;
        [SerializeField] private UnityEvent onDisconnected;
        [SerializeField] private StateChangeEvent onStateChanged;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                DestroyImmediate(gameObject);
                return;
            }

            orientation = GetComponentInChildren<Orientation>();
            yawTracker = GetComponentInChildren<YawTracker>();

            // Composition root: concrete transport/codec choices live here, and only here.
            connection = new TcpReliableConnection();
            dataChannel = new UdpDatagramChannel(udpClientPort);
            IMotionEncoder motionEncoder = new MotionDataCodec();

            connectionManager = new YawConnectionManager(connection, dataChannel, this, udpClientPort);
            connectionManager.SetGameName(gameName);

            discoveryService = new YawDeviceDiscoveryService(dataChannel, this);
            motionPump = new MotionDataPump(dataChannel, motionEncoder);

            WireConnectionManagerEvents();
            discoveryService.DeviceFound += HandleDeviceFound;

            dataChannel.StartListening();

            Debug.Log("[YawController] Initialized");
        }

        private void WireConnectionManagerEvents()
        {
            connectionManager.StateChanged += HandleStateChanged;
            connectionManager.CheckInReplyReceived += () => InvokeDelayed(UpdateIMUOffset, 0.1f);
            connectionManager.Connected += HandleConnected;
            connectionManager.Disconnected += () => onDisconnected?.Invoke();
            connectionManager.DeviceConnectionLost += HandleDeviceConnectionLost;
            connectionManager.DeviceStartedFromApp += () => ControllerDelegate?.DeviceStartedFromApp();
            connectionManager.DeviceStoppedFromApp += () => ControllerDelegate?.DeviceStoppedFromApp();
            connectionManager.DeviceReportedStateChanged += newState => onStateChanged?.Invoke(newState);
        }

        private void HandleStateChanged(ControllerState newState)
        {
            device = connectionManager.Device; // Inspector-visible mirror only, see field comment above.
            ControllerDelegate?.ControllerStateChanged(newState);
        }

        private void HandleConnected()
        {
            foreach (Action a in OnConnectReceivers) a?.Invoke();
            onConnected?.Invoke();
        }

        private void HandleDeviceConnectionLost(YawDevice lostDevice)
        {
            ControllerDelegate?.DidDisconnectFrom(lostDevice);
        }

        private void HandleDeviceFound(YawDevice foundDevice)
        {
            ControllerDelegate?.DidFoundDevice(foundDevice);
        }

        private void Start()
        {
            if (connectType == ConnectType.CONNECT_FIRST_FOUND_DEVICE)
            {
                Debug.Log("-----------------------------DISCOVER---------------------------");
                // Calling continuously because a UDP discovery packet may be lost.
                // We receive the device via the DeviceFound event -> HandleDeviceFound above.
                discoveryService.StartAutoDiscovery(50010, () => connectionManager.State == ControllerState.Initial);
            }

            if (connectType == ConnectType.DEBUG_CONNECT_TO_IP)
            {
                ConnectToDevice(new YawDevice(IPAddress.Parse(debug_ipAddress), 50020, 50010, "001", "DEBUG", DeviceStatus.Available), null, null);
            }
        }

        private void FixedUpdate()
        {
            referenceRotation.pitch = orientation.pitch;
            referenceRotation.yaw = orientation.yaw;
            referenceRotation.roll = orientation.roll;

            if (connectionManager.State == ControllerState.Started || connectionManager.State == ControllerState.Connected)
            {
                SendMotionData();
            }
        }

        private void OnDestroy()
        {
            if (instance != this) return;

            if (connectionManager.State != ControllerState.Initial && connectionManager.State != ControllerState.Disconnecting && connectionManager.Device != null)
            {
                DisconnectFromDevice(null, null);
            }

            TeardownTransports();
            instance = null;
        }

        private void OnApplicationQuit()
        {
            if (connectionManager.State != ControllerState.Initial && connectionManager.State != ControllerState.Disconnecting && connectionManager.Device != null)
            {
                DisconnectFromDevice(null, null);
            }

            TeardownTransports();
        }

        private void TeardownTransports()
        {
            connection?.Disconnect();
            dataChannel?.StopListening();
        }

        public void SetGameName(string gameName)
        {
            this.gameName = gameName;
            connectionManager.SetGameName(gameName);
        }

        public void DiscoverDevices(int onPort)
        {
            discoveryService.DiscoverDevices(onPort);
        }

        public void ConnectToDevice(YawDevice yawDevice, Action onSuccess, Action<string> onError)
        {
            connectionManager.Connect(yawDevice, onSuccess, onError);
        }

        public void StartDevice(Action onSuccess = null, Action<string> onError = null)
        {
            connectionManager.StartDevice(onSuccess, onError);
        }

        public void StopDevice(bool park, Action onSuccess = null, Action<string> onError = null)
        {
            connectionManager.StopDevice(park, onSuccess, onError);
        }

        public void CalibrateDevice(bool allAxis)
        {
            connectionManager.CalibrateDevice(allAxis);
        }

        public void DisconnectFromDevice(Action onSuccess, Action<string> onError)
        {
            connectionManager.DisconnectFromDevice(onSuccess, onError);
        }

        /// <summary>
        /// Set rotation limits for the YawTracker
        /// </summary>
        public void SetTiltLimits(float yawLimit, float pitchLimit, float rollLimit)
        {
            gameLimits.yaw = yawLimit;
            gameLimits.pitch = pitchLimit;
            gameLimits.roll = rollLimit;
        }

        /// <summary>
        /// Set rotation multiplier for the YawTracker
        /// </summary>
        public void SetRotationMultiplier(float yaw, float pitch, float roll)
        {
            rotationMultiplier.x = pitch;
            rotationMultiplier.y = yaw;
            rotationMultiplier.z = roll;
        }

        private void SendMotionData()
        {
            if (connectionManager.Device == null) return;

            float yaw = SignedForm(referenceRotation.yaw);
            float pitch = SignedForm(referenceRotation.pitch);
            float roll = SignedForm(referenceRotation.roll);

            motionPump.SendMotion(new OVector(yaw, pitch, roll), buzzer, smartPlug);
        }

        public void SendLED(Color32[] colors)
        {
            if (colors.Length != 129) return;
            dataChannel.Send(CommandEncoder.UdpLedCommand(colors));
        }

        public void SendLED(Color32 color)
        {
            dataChannel.Send(CommandEncoder.UdpLedCommand(color));
        }

        /// <summary>
        /// Mark the current IMU data as origin for the Camera rotation cancellation
        /// </summary>
        public void UpdateIMUOffset()
        {
            cancellation.UpdateOffset();
        }

        private float SignedForm(float angle)
        {
            return angle >= 180 ? angle - 360 : angle;
        }

        // ICoroutineRunner: lets the plain C# services above (which are not
        // MonoBehaviours) schedule/cancel Unity coroutines through this instance.
        public Coroutine RunCoroutine(IEnumerator routine) => StartCoroutine(routine);

        public void StopCoroutineIfRunning(ref Coroutine routine)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
        }

        public void InvokeDelayed(Action action, float delaySeconds)
        {
            StartCoroutine(DelayedInvoke(action, delaySeconds));
        }

        private IEnumerator DelayedInvoke(Action action, float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            action?.Invoke();
        }
    }
}
