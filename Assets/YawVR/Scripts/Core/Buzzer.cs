using System;

namespace YawVR
{
    /// <summary>
    /// Buzzer info: amplitudes and hz.
    /// </summary>
    [Serializable]
    public class Buzzer
    {
        public bool isOn;
        public int right_amp, center_amp, left_amp, hz;

        public void SetBuzzerAmps(int right, int center, int left)
        {
            right_amp = right;
            center_amp = center;
            left_amp = left;
        }

        public void SetHz(int buzzerHz)
        {
            hz = buzzerHz;
        }

        public void SetOn(bool b)
        {
            isOn = b;
        }
    }
}
