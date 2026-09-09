using UnityEngine;
using UnityEngine.InputSystem;

public class Engine : MonoBehaviour
{
    public static Engine Instance { get; private set; }
    
    [Header("Input")]
    [SerializeField] private InputActionReference engineToggleAction;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip engineStartClip;
    [SerializeField] private AudioClip engineLoopClip;
    [SerializeField] private AudioClip engineStopClip;
    
    [Header("Lights")]
    [SerializeField] private GameObject[] lights;
    
    private bool[] lightStates;
    public static bool isEngineRunning = false;
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    private void OnEnable()
    {
        if (engineToggleAction != null)
        {
            engineToggleAction.action.Enable();
            engineToggleAction.action.performed += OnToggleEngine;
        }
        
        lightStates = new bool[lights.Length];
    }

    private void OnDisable()
    {
        if (engineToggleAction != null)
        {
            engineToggleAction.action.performed -= OnToggleEngine;
            engineToggleAction.action.Disable();
        }
    }

    private void OnToggleEngine(InputAction.CallbackContext context)
    {
        if (!isEngineRunning)
        {
            StartEngine();
        }
        else
        {
            InternalTurnEngineOff();
        }
    }

    private void StartEngine()
    {
        isEngineRunning = true;
        
        audioSource.Stop();
        audioSource.loop = false;
        audioSource.clip = engineStartClip;
        audioSource.Play();
        
        Invoke(nameof(PlayLoopingEngineSound), engineStartClip.length);
        
        for (int i = 0; i < lights.Length; i++)
        {
            lights[i].SetActive(lightStates[i]);
        }
    }
    
    private void PlayLoopingEngineSound()
    {
        if (!isEngineRunning) return;

        audioSource.clip = engineLoopClip;
        audioSource.loop = true;
        audioSource.Play();
    }

    public static void TurnEngineOff()
    {
        if (Instance != null) Instance.InternalTurnEngineOff();
    }

    private void InternalTurnEngineOff()
    {
        if (!isEngineRunning) return;

        isEngineRunning = false;

        for (int i = 0; i < lights.Length; i++)
        {
            lightStates[i] = lights[i].activeSelf;
            lights[i].SetActive(false);
        }

        audioSource.Stop();
        audioSource.loop = false;
        audioSource.clip = engineStopClip;
        audioSource.Play();
        
        CancelInvoke(nameof(PlayLoopingEngineSound));
    }
}