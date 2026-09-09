using UnityEngine;
using UnityEngine.InputSystem;

public class Horn : MonoBehaviour
{
    [Header("Input")] [SerializeField] private InputActionReference hornButton;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hornClip;

    private void OnEnable()
    {
        hornButton.action.performed += OnHornButton;
    }

    private void OnDisable()
    {
        hornButton.action.performed -= OnHornButton;
    }

    private void OnHornButton(InputAction.CallbackContext context)
    {
        audioSource.clip = hornClip;
        audioSource.Play();
    }
}
