using System.Collections;
using UnityEngine;

namespace YawVR
{
    /// <summary>
    /// Minimal seam that lets plain C# service classes (which are not MonoBehaviours)
    /// schedule and cancel Unity coroutines without being MonoBehaviours themselves.
    /// Implemented by <see cref="YawController"/>, which is the object that actually
    /// hosts coroutine execution.
    /// </summary>
    public interface ICoroutineRunner
    {
        Coroutine RunCoroutine(IEnumerator routine);
        void StopCoroutineIfRunning(ref Coroutine routine);

        /// <summary>Schedules a parameterless call after <paramref name="delaySeconds"/>, mirroring MonoBehaviour.Invoke.</summary>
        void InvokeDelayed(System.Action action, float delaySeconds);
    }
}
