using System.Globalization;
using System.Net;
using UnityEngine;

namespace YawVR
{
    /// <summary>
    /// Parses the two kinds of ASCII messages that arrive on the UDP data channel:
    /// live telemetry (Y[]/P[]/R[]/U[]) from a connected device, and discovery replies
    /// (YAWDEVICE;...) from a broadcast ping. Split out of the old
    /// YawController.DidRecieveUDPMessage so parsing has no dependency on connection
    /// state or Unity lifecycle.
    /// </summary>
    public static class UdpMessageParser
    {
        /// <summary>
        /// Applies any Y[]/P[]/R[]/U[] fields found in <paramref name="message"/> directly
        /// onto <paramref name="device"/>. Returns true if the message looked like a
        /// telemetry line at all (mirrors the original's loose Contains-based check).
        /// </summary>
        public static bool TryApplyTelemetry(string message, YawDevice device)
        {
            if (!(message.Contains("Y[") || message.Contains("P[") || message.Contains("R[")))
                return false;

            ExtractValue(message, "Y[", ref device.ActualPosition.yaw);
            ExtractValue(message, "P[", ref device.ActualPosition.pitch);
            ExtractValue(message, "R[", ref device.ActualPosition.roll);

            if (ExtractValue(message, "U[", ref device.batteryVoltage))
            {
                device.batteryPercent = Mathf.InverseLerp(2.8f, 4.2f, device.batteryVoltage);
            }

            return true;
        }

        /// <summary>
        /// Parses a "YAWDEVICE;id;name;tcpPort;AVAILABLE|..." discovery reply. The
        /// device's UDP port is not carried in the reply; it's assumed to equal the
        /// port the discovery ping was broadcast on/from (<paramref name="assumedUdpPort"/>) —
        /// this is a known weak assumption, see firmware-todo.md.
        /// </summary>
        public static bool TryParseDiscoveryReply(string message, IPEndPoint remoteEndPoint, int assumedUdpPort, out YawDevice device)
        {
            device = null;
            if (!message.Contains("YAWDEVICE")) return false;

            var parts = message.Split(';');
            if (parts.Length < 5 || !int.TryParse(parts[3], out int tcpPort)) return false;

            DeviceStatus status = parts[4] == "AVAILABLE" ? DeviceStatus.Available : DeviceStatus.Reserved;
            device = new YawDevice(remoteEndPoint.Address, tcpPort, assumedUdpPort, parts[1], parts[2], status);
            return true;
        }

        private static bool ExtractValue(string msg, string key, ref float result)
        {
            int startIdx = msg.IndexOf(key);
            if (startIdx == -1) return false;

            startIdx += 2;
            int endIdx = msg.IndexOf(']', startIdx);
            if (endIdx == -1) return false;

            string valStr = msg.Substring(startIdx, endIdx - startIdx);
            return float.TryParse(valStr, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }
    }
}
