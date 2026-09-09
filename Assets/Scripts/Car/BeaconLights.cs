using UnityEngine;
using UnityEngine.InputSystem;

public class BeaconLights : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;

    [SerializeField] private GameObject beaconlights;
    
    private InputAction beaconlightOn;
    private InputAction beaconlightOff;

    private void OnEnable()
    {
        beaconlightOn = inputActions.FindActionMap("SwitchPanel").FindAction("BeaconLightsOn");
        beaconlightOff = inputActions.FindActionMap("SwitchPanel").FindAction("BeaconLightsOff");
    
        beaconlightOn.performed += OnBeaconlightsOn;
        beaconlightOff.performed += OnBeaconlightsOff;

        beaconlightOn.Enable();
        beaconlightOff.Enable();
    }

    private void OnDisable()
    {
        beaconlightOn.performed -= OnBeaconlightsOn;
        beaconlightOff.performed -= OnBeaconlightsOff;

        beaconlightOn.Disable();
        beaconlightOff.Disable();
    }

    private void OnBeaconlightsOn(InputAction.CallbackContext context)
    {
        if (!Engine.isEngineRunning) return;
        beaconlights.SetActive(true);
    }
    
    private void OnBeaconlightsOff(InputAction.CallbackContext context)
    {
        if (!Engine.isEngineRunning) return;
        beaconlights.SetActive(false);
    }
}
