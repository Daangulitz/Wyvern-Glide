using System;
using UnityEngine.Events;

namespace YawVR
{
    [Serializable]
    public class StateChangeEvent : UnityEvent<DeviceState> { }
}
