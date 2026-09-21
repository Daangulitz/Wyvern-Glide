using System;

namespace YawVR
{
    /// <summary>
    /// Single home for the big-endian byte&lt;-&gt;primitive conversions the wire protocol
    /// uses. Previously duplicated between the old Commands.cs and Helpers.cs; every
    /// method here is non-mutating (never writes back into the caller's buffer), unlike
    /// the old Helpers.ReadInt, which swapped bytes in place as a side effect of reading.
    /// </summary>
    public static class ByteConversion
    {
        public static byte[] IntToBytes(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return bytes;
        }

        public static byte[] FloatToBytes(float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return bytes;
        }

        public static byte[] BoolToBytes(bool value)
        {
            return BitConverter.GetBytes(value);
        }

        public static int BytesToInt(byte[] data, int startIndex)
        {
            return (int)BytesToUInt32(data, startIndex, littleEndian: false);
        }

        public static float BytesToFloat(byte[] data, int startIndex)
        {
            byte[] copy = { data[startIndex], data[startIndex + 1], data[startIndex + 2], data[startIndex + 3] };
            if (BitConverter.IsLittleEndian) Array.Reverse(copy);
            return BitConverter.ToSingle(copy, 0);
        }

        public static bool BytesToBool(byte[] data, int startIndex)
        {
            return BitConverter.ToBoolean(data, startIndex);
        }

        /// <summary>
        /// Reads a UInt32 from <paramref name="data"/> at <paramref name="offset"/> without
        /// mutating the input array (the old Helpers.ReadInt swapped bytes in the caller's
        /// buffer in place, which could corrupt data if the buffer was read twice or pooled).
        /// </summary>
        public static uint BytesToUInt32(byte[] data, int offset, bool littleEndian)
        {
            byte[] copy = { data[offset], data[offset + 1], data[offset + 2], data[offset + 3] };
            if (BitConverter.IsLittleEndian != littleEndian) Array.Reverse(copy);
            return BitConverter.ToUInt32(copy, 0);
        }

        public static byte[] PrependByte(byte[] data, byte newFirstByte)
        {
            byte[] result = new byte[data.Length + 1];
            data.CopyTo(result, 1);
            result[0] = newFirstByte;
            return result;
        }

        public static void FromUShort(ushort number, out byte low, out byte high)
        {
            high = (byte)(number >> 8);
            low = (byte)(number & 255);
        }
    }
}
