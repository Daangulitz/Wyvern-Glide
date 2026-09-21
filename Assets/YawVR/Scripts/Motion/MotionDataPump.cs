namespace YawVR
{
    /// <summary>
    /// Turns the tracked orientation into wire bytes and sends them on the data
    /// channel. Depends on <see cref="IMotionEncoder"/> and <see cref="IDatagramChannel"/>
    /// rather than a concrete wire format or transport, so a future model with a
    /// different motion encoding only needs a new IMotionEncoder, not a change here
    /// (OCP). Driven once per FixedUpdate by <see cref="YawController"/>.
    /// </summary>
    public class MotionDataPump
    {
        private readonly IDatagramChannel dataChannel;
        private readonly IMotionEncoder encoder;

        public MotionDataPump(IDatagramChannel dataChannel, IMotionEncoder encoder)
        {
            this.dataChannel = dataChannel;
            this.encoder = encoder;
        }

        public void SendMotion(OVector rotation, Buzzer buzzer, byte smartPlug)
        {
            dataChannel.Send(encoder.EncodeMotion(rotation, buzzer, smartPlug));
        }
    }
}
