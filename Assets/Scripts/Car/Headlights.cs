using UnityEngine;
using UnityEngine.InputSystem;

public class Headlights : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;

    [SerializeField] private GameObject headlights;
    
    private InputAction headlightOn;
    private InputAction headlightOff;

    private void OnEnable()
    {
        headlightOn = inputActions.FindActionMap("SwitchPanel").FindAction("StrobeLightsOn");
        headlightOff = inputActions.FindActionMap("SwitchPanel").FindAction("StrobeLightsOff");
    
        headlightOn.performed += OnHeadlightsOn;
        headlightOff.performed += OnHeadlightsOff;

        headlightOn.Enable();
        headlightOff.Enable();
    }

    private void OnDisable()
    {
        headlightOn.performed -= OnHeadlightsOn;
        headlightOff.performed -= OnHeadlightsOff;

        headlightOn.Disable();
        headlightOff.Disable();
    }

    private void OnHeadlightsOn(InputAction.CallbackContext context)
    {
        if (!Engine.isEngineRunning) return;
        headlights.SetActive(true);
    }
    
    private void OnHeadlightsOff(InputAction.CallbackContext context)
    {
        if (!Engine.isEngineRunning) return;
        headlights.SetActive(false);
    }
}
