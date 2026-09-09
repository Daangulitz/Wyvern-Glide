using UnityEngine;
using UnityEngine.InputSystem;

public class ShowMenuButton : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;

    [SerializeField] private GameObject menu;
    
    private InputAction openMenuButton;

    private void OnEnable()
    {
        openMenuButton = inputActions.FindActionMap("RightHandle").FindAction("ArmAutoThrottle");
        openMenuButton.performed += OnOpenMenuButton;
        openMenuButton.Enable();
    }

    private void OnDisable()
    {
        openMenuButton.performed -= OnOpenMenuButton;
        openMenuButton.Disable();
    }

    private void OnOpenMenuButton(InputAction.CallbackContext context)
    {
        menu.SetActive(true);
    }
}
