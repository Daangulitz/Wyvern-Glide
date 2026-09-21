using System.Text;

namespace YawVR
{
    /// <summary>
    /// The YAW 3's current motion wire format: an ASCII string like
    /// "Y[012.345]P[359.990]R[180.000]V[0,0,0,0]F[0,0]" (yaw/pitch/roll, buzzer, smart plug).
    /// Implements <see cref="IMotionEncoder"/> so a future model with a different wire
    /// format can be swapped in via <see cref="MotionDataPump"/> without any other change.
    /// </summary>
    public class MotionDataCodec : IMotionEncoder
    {
        public byte[] EncodeMotion(OVector rotation, Buzzer buzzer, byte smartPlug)
        {
            string orientation = string.Format("Y[{0}]P[{1}]R[{2}]",
                CommandEncoder.FormatRotation(rotation.yaw),
                CommandEncoder.FormatRotation(rotation.pitch),
                CommandEncoder.FormatRotation(rotation.roll));

            string buzzerFormat = buzzer.isOn && buzzer.hz > 0
                ? string.Format("V[{0},{1},{2},{3}]", buzzer.right_amp, buzzer.center_amp, buzzer.left_amp, buzzer.hz)
                : "V[0,0,0,0]";

            string smartPlugFormat = string.Format("F[{0},{0}]", smartPlug);

            return Encoding.ASCII.GetBytes(orientation + buzzerFormat + smartPlugFormat);
        }
    }
}
