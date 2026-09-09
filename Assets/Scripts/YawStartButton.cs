using UnityEngine;
using UnityEngine.InputSystem;
using YawVR;

public class YawStartButton : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    
    private InputAction yawOn;
    private InputAction yawOff;

    private bool disableCanRun = false;

    private void OnEnable()
    {
        yawOn = inputActions.FindActionMap("GForce G29").FindAction("X");
        yawOff = inputActions.FindActionMap("GForce G29").FindAction("O");
    
        yawOn.performed += OnYawOn;
        yawOff.performed += OnYawOff;

        yawOn.Enable();
        yawOff.Enable();
    }

    private void OnDisable()
    {
        yawOn.performed -= OnYawOn;
        yawOff.performed -= OnYawOff;

        yawOn.Disable();
        yawOff.Disable();
    }

    private void OnYawOn(InputAction.CallbackContext context)
    {
        if (YawController.Instance().State == ControllerState.Connected)
        {
            YawController.Instance().StartDevice(
                () => Debug.Log("Yaw started"),
                error => Debug.LogError("Failed to start device: " + error)
            );
        }
        else
        {
            Debug.LogWarning("Yaw not in a ready-to-start state: " + YawController.Instance().State);
        }
        
        disableCanRun = true;
    }
    
    private void OnYawOff(InputAction.CallbackContext context)
    {
        if (!disableCanRun) return;
        
        if (YawController.Instance().State == ControllerState.Started)
        {
            YawController.Instance().StopDevice(
                true,
                () => Debug.Log("Yaw stopped"),
                error => Debug.LogError("Failed to stop device: " + error)
            );
        }
        else
        {
            Debug.LogWarning("Yaw not in a running state: " + YawController.Instance().State);
        }
    }
}
