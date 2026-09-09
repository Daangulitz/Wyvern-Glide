using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
public class ResetView : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;

    [SerializeField] private XROrigin xrOrigin;
    public Transform driverHeadTarget;
    

    private InputAction resetViewButton;

    private void OnEnable()
    {
        resetViewButton = inputActions.FindActionMap("LeftHandle").FindAction("ToggleSmartCamera");
        resetViewButton.performed += ResetHeadsetView;
        resetViewButton.Enable();
    }

    private void OnDisable()
    {
        resetViewButton.performed -= ResetHeadsetView;
        resetViewButton.Disable();
    }
    
    private void ResetHeadsetView(InputAction.CallbackContext context)
    {
        if (xrOrigin == null || driverHeadTarget == null)
        {
            Debug.LogError("XR Origin or Driver Target is not assigned.");
            return;
        }

        Transform hmd = xrOrigin.Camera.transform;

        Vector3 headsetLocalOffset = xrOrigin.transform.InverseTransformPoint(hmd.position);
        Vector3 worldTargetHeadPos = driverHeadTarget.position;
        Vector3 newRigPosition = worldTargetHeadPos - (xrOrigin.transform.rotation * headsetLocalOffset);
        xrOrigin.transform.position = newRigPosition;

        Vector3 hmdForward = hmd.forward;
        hmdForward.y = 0;
        hmdForward.Normalize();

        Vector3 targetForward = driverHeadTarget.forward;
        targetForward.y = 0;
        targetForward.Normalize();

        float angleDifference = Vector3.SignedAngle(hmdForward, targetForward, Vector3.up);

        
        xrOrigin.transform.RotateAround(hmd.position, Vector3.up, angleDifference);

    }
}

