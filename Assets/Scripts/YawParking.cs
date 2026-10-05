using UnityEngine;
using YawVR;

public class YawParking : MonoBehaviour
{
    public void ParkYaw()
    {
        if (YawController.Instance.State != ControllerState.Started)
        {
            Debug.LogWarning($"[YawParking] Can't park: device isn't Started (currently {YawController.Instance.State})");
            return;
        }

        Debug.Log("Parking yaw...");
        YawController.Instance.StopDevice(true,
            onSuccess: () => Debug.Log("[YawParking] Parked."),
            onError: (err) => Debug.LogError($"[YawParking] Park failed: {err}"));
    }

    public void ResumeYaw()
    {
        if (YawController.Instance.State != ControllerState.Connected)
        {
            Debug.LogWarning($"[YawParking] Can't resume: device isn't Connected (currently {YawController.Instance.State})");
            return;
        }

        Debug.Log("Resuming yaw...");
        YawController.Instance.StartDevice(
            onSuccess: () => Debug.Log("[YawParking] Resumed."),
            onError: (err) => Debug.LogError($"[YawParking] Resume failed: {err}"));
    }
}