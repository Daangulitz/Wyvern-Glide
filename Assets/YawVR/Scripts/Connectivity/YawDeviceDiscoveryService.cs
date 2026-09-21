using System;
using System.Collections;
using System.Net;
using System.Text;
using UnityEngine;

namespace YawVR
{
    /// <summary>
    /// Owns broadcast device discovery: sends periodic "who's out there" pings and
    /// parses YAWDEVICE replies into <see cref="YawDevice"/> instances. Extracted from
    /// the old YawController god object, which also contained a private DidFoundDevice
    /// method that was never actually called by anything (dead code — not carried
    /// over here; see the refactor summary).
    /// </summary>
    public class YawDeviceDiscoveryService
    {
        private readonly IDatagramChannel dataChannel;
        private readonly ICoroutineRunner coroutineRunner;
        private int lastDiscoveryPort;
        private Coroutine autoDiscoverRoutine;

        public event Action<YawDevice> DeviceFound;

        public YawDeviceDiscoveryService(IDatagramChannel dataChannel, ICoroutineRunner coroutineRunner)
        {
            this.dataChannel = dataChannel;
            this.coroutineRunner = coroutineRunner;
            dataChannel.DatagramReceived += HandleDatagram;
        }

        public void DiscoverDevices(int onPort)
        {
            lastDiscoveryPort = onPort;
            dataChannel.SendBroadcast(onPort, CommandEncoder.DEVICE_DISCOVERY);
        }

        /// <summary>
        /// Repeatedly broadcasts on <paramref name="port"/> once per second for as long
        /// as <paramref name="shouldKeepSearching"/> returns true — a UDP discovery
        /// ping may be lost, so this keeps retrying instead of sending once.
        /// </summary>
        public void StartAutoDiscovery(int port, Func<bool> shouldKeepSearching)
        {
            autoDiscoverRoutine = coroutineRunner.RunCoroutine(AutoDiscoverLoop(port, shouldKeepSearching));
        }

        public void StopAutoDiscovery()
        {
            coroutineRunner.StopCoroutineIfRunning(ref autoDiscoverRoutine);
        }

        private IEnumerator AutoDiscoverLoop(int port, Func<bool> shouldKeepSearching)
        {
            while (shouldKeepSearching())
            {
                DiscoverDevices(port);
                yield return new WaitForSeconds(1f);
            }
        }

        private void HandleDatagram(byte[] bytes, IPEndPoint remoteEndPoint)
        {
            string message = Encoding.ASCII.GetString(bytes);
            if (UdpMessageParser.TryParseDiscoveryReply(message, remoteEndPoint, lastDiscoveryPort, out YawDevice device))
            {
                DeviceFound?.Invoke(device);
            }
        }
    }
}
