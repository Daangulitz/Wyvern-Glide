namespace YawVR
{
    /// <summary>
    /// Encodes the game's current orientation (plus buzzer/smart-plug state) into the
    /// wire format a specific YAW model's firmware expects. This is the seam for
    /// supporting a future model with a different wire format without touching the
    /// motion pump or connection logic. See <see cref="MotionDataCodec"/> for
    /// the YAW 3's current ASCII format.
    /// </summary>
    public interface IMotionEncoder
    {
        byte[] EncodeMotion(OVector rotation, Buzzer buzzer, byte smartPlug);
    }
}
