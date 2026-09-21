using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace YawVR
{
    /*Communication between the game and the simulator:

        UDP messages:
        Every udp command is ascii encoded string sent as byte array

        TCP messages:
        Every tcp command begins with the byte identifier of the given command,
        followed by the command parameters.
        Integer and float parameters are converted into 4 bytes (sent in big endian format),
        string parameters are converted into byte array with ASCII encoding.
        (CommandIds contains the tcp command ids)
    */

    /// <summary>
    /// Encodes outbound wire messages for the current YAW 3 protocol. Motion encoding
    /// specifically lives in <see cref="MotionDataCodec"/> behind <see cref="IMotionEncoder"/>
    /// so a future model with a different wire format doesn't need to change this class.
    /// </summary>
    public static class CommandEncoder
    {
        public static readonly byte[] DEVICE_DISCOVERY = Encoding.ASCII.GetBytes("YAW_CALLING");

        private static ushort udpLedCounter = 0;

        public static byte[] UdpLedCommand(UnityEngine.Color32[] colors)
        {
            var bytes = new byte[390];
            bytes[0] = CommandIds.UDP_LED_CMD;
            ByteConversion.FromUShort(udpLedCounter, out bytes[1], out bytes[2]);

            for (int i = 3; i < bytes.Length; i += 3)
            {
                bytes[i] = colors[(i - 3) / 3].g;
                bytes[i + 1] = colors[(i - 3) / 3].r;
                bytes[i + 2] = colors[(i - 3) / 3].b;
            }

            udpLedCounter++;
            return bytes;
        }

        public static byte[] UdpLedCommand(UnityEngine.Color32 color)
        {
            var bytes = new byte[390];
            bytes[0] = CommandIds.UDP_LED_CMD;
            ByteConversion.FromUShort(udpLedCounter, out bytes[1], out bytes[2]);

            for (int i = 3; i < bytes.Length; i += 3)
            {
                bytes[i] = color.g;
                bytes[i + 1] = color.r;
                bytes[i + 2] = color.b;
            }

            udpLedCounter++;
            return bytes;
        }

        public static byte[] CheckIn(int udpListeningPort, string gameName)
        {
            List<byte> message = new();
            message.AddRange(ByteConversion.IntToBytes(udpListeningPort));
            message.AddRange(Encoding.ASCII.GetBytes(gameName));
            return ByteConversion.PrependByte(message.ToArray(), CommandIds.CHECK_IN);
        }

        public static readonly byte[] START = { CommandIds.START };
        public static readonly byte[] CALIBRATE_TEMPLATE = { CommandIds.CALIBRATE };
        public const byte STOP = CommandIds.STOP;
        public static readonly byte[] EXIT = { CommandIds.EXIT };

        internal static string FormatRotation(float f)
        {
            return f.ToString("000.000", CultureInfo.InvariantCulture);
        }
    }
}
