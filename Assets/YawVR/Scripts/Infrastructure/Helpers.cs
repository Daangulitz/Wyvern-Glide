using System;
using System.IO;

namespace YawVR
{
    /// <summary>
    /// Angle-math and misc string/array utilities. Byte&lt;-&gt;primitive conversion
    /// moved to <see cref="ByteConversion"/> (it was duplicated between the old
    /// Commands.cs and this class). The following members were removed as dead code
    /// during the SOLID refactor — confirmed zero references anywhere in the repo:
    /// SubArray&lt;T&gt;, FromShort, RandomString, ContainsAny, ReadSingle (which simply
    /// threw NotImplementedException).
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        ///   <para>Clamps value between 0 and 1 and returns value.</para>
        /// </summary>
        public static float Clamp01(float value)
        {
            if ((double)value < 0.0)
                return 0.0f;
            if ((double)value > 1.0)
                return 1f;
            return value;
        }

        /// <summary>
        ///   <para>Linearly interpolates between a and b by t.</para>
        /// </summary>
        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Clamp01(t);
        }

        public static float floatConversion(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes); // Convert big endian to little endian
            }
            float myFloat = BitConverter.ToSingle(bytes, 0);
            return (float)Math.Round(myFloat, 3);
        }

        public static float ClampBetween(float v, float min, float max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        public static float Clamp(float v, float limit)
        {
            if (limit == -1) return v;
            if (v > limit) return limit;
            if (v < -limit) return -limit;
            return v;
        }

        public static float NormalizeAngle(float angle)
        {
            float newAngle = angle;
            while (newAngle <= -180) newAngle += 360;
            while (newAngle > 180) newAngle -= 360;
            return newAngle;
        }

        public static string StringToFilename(string s)
        {
            Array.ForEach(Path.GetInvalidFileNameChars(),
                c => s = s.Replace(c.ToString(), "_"));

            return s;
        }

        public static double RadianToDegree(double angle)
        {
            return angle * (180.0 / Math.PI);
        }
    }
}
