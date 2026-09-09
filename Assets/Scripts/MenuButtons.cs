using UnityEngine;
using YawVR;

public class MenuButtons : MonoBehaviour
{
    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private GameObject optionsCanvas;
    
    public void ResumeButton()
    {
        menuCanvas.SetActive(false);
        // TODO: Make the menu pause the game
    }
    
    /// <summary>
    /// Emergency stop without parking (ABORT)
    /// </summary>
    public void ExitButton()
    {
        Debug.Log("Exit");
    }
    
    public void OptionsButton()
    {
        optionsCanvas.SetActive(true);
        menuCanvas.SetActive(false);
    }
    
    /// <summary>
    /// Graceful shutdown with parking
    /// </summary>
    public void QuitButton()
    {
        if (YawController.Instance().State == ControllerState.Started)
        {
            YawController.Instance().StopDevice(
                park: true,
                onSuccess: () =>
                {
                    Debug.Log("Yaw parked and stopped");
                    Application.Quit();
                },
                onError: error =>
                {
                    Debug.LogError("Parked stop failed: " + error);
                    Application.Quit();
                }
            );
        }
        else
        {
            Application.Quit();
        }
    }
}
