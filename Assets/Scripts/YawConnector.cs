using UnityEngine;
using System.Net;

namespace YawVR
{
    public class YawConnector : MonoBehaviour, YawControllerDelegate
    {
        [SerializeField] private string yawIp = "10.10.30.23";

        private static YawConnector Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        private void Start()
        {
            YawController.Instance().ControllerDelegate = this;
            Debug.Log("Assigned ControllerDelegate");

            ConnectViaIp(yawIp);
        }

        private void ConnectViaIp(string ip)
        {
            var device = new YawDevice(IPAddress.Parse(yawIp), DeviceType.YAW3, 50020, 50010, "001", "DEBUG",
                DeviceStatus.Available);

            YawController.Instance().ConnectToDevice(device,
                () => Debug.Log($"Connected to Yaw at {ip}"),
                err => Debug.LogError("Manual IP connect failed: " + err));
        }

        public void ControllerStateChanged(ControllerState state)
        {
            Debug.Log("Controller state changed: " + state);
        }

        public void DidFoundDevice(YawDevice device)
        {
            Debug.Log("Found device: " + device.Name);
            if (YawController.Instance().State == ControllerState.Initial &&
                (device.Status == DeviceStatus.Available || device.Status == DeviceStatus.Unknown))
            {
                YawController.Instance().ConnectToDevice(device,
                    () => Debug.Log("Auto-connected to device"),
                    (err) => Debug.LogError("Auto-connect failed: " + err));
            }
        }

        public void DidDisconnectFrom(YawDevice device)
        {
            Debug.Log("Disconnected from: " + device.Name);
        }

        public void DeviceStoppedFromApp()
        {
            Debug.Log("Device stopped from app");
        }

        public void DeviceStartedFromApp()
        {
            Debug.Log("Device started from app");
        }
    }
}
