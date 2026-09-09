using System.Collections;
using UnityEngine;
using YawVR;

public class YawEvents : MonoBehaviour
{
    private static YawEvents Instance { get; set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void OnYawConnected()
    {
        StartCoroutine(WaitForConnectionThenPark());
    }

    public void OnYawDisconnected()
    {
        
    }

    public void OnYawStateChanged()
    {
        
    }

    private IEnumerator WaitForConnectionThenPark()
    {
        while (YawController.Instance() == null || YawController.Instance().State != ControllerState.Connected)
        {
            yield return null;
        }

        Debug.Log("Yaw connected. Starting...");

        bool startDone = false;
        YawController.Instance().StartDevice(
            onSuccess: () =>
            {
                Debug.Log("Yaw started.");
                startDone = true;
            },
            onError: error =>
            {
                Debug.LogError("Failed to start Yaw: " + error);
                startDone = true;
            }
        );

        while (!startDone)
        {
            yield return null;
        }

        if (YawController.Instance().State == ControllerState.Started)
        {
            YawController.Instance().StopDevice(
                park: true,
                onSuccess: () => Debug.Log("Yaw successfully parked."),
                onError: error => Debug.LogError("Failed to park Yaw: " + error)
            );
        }
        else
        {
            Debug.LogWarning("Yaw not in Started state after start. Skipping park.");
        }
    }
}
