using UnityEngine;
using UnityEngine.InputSystem;

public class Spotlights : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;

    [SerializeField] private GameObject spotlights;
    
    private InputAction spotlightsOn;
    private InputAction spotlightsOff;

    private void OnEnable()
    {
        spotlightsOn = inputActions.FindActionMap("SwitchPanel").FindAction("NavLightsOn");
        spotlightsOff = inputActions.FindActionMap("SwitchPanel").FindAction("NavLightsOff");
    
        spotlightsOn.performed += OnSpotlightsOn;
        spotlightsOff.performed += OnSpotlightsOff;

        spotlightsOn.Enable();
        spotlightsOff.Enable();
    }

    private void OnDisable()
    {
        spotlightsOn.performed -= OnSpotlightsOn;
        spotlightsOff.performed -= OnSpotlightsOff;

        spotlightsOn.Disable();
        spotlightsOff.Disable();
    }

    private void OnSpotlightsOn(InputAction.CallbackContext context)
    {
        if (!Engine.isEngineRunning) return;
        spotlights.SetActive(true);
    }
    
    private void OnSpotlightsOff(InputAction.CallbackContext context)
    {
        if (!Engine.isEngineRunning) return;
        spotlights.SetActive(false);
    }
}
