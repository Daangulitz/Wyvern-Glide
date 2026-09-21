using System;

namespace YawVR
{
    [Serializable]
    public struct Parameters
    {
        public byte Power, RollLimit, PitchLimitF, PitchLimitB;
        public UInt32 YawLimit;
        public bool hasYawLimit;
    }
}
