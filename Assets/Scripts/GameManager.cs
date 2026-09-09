using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{ 
    public static GameManager Instance;

    public static int zombieskilled;

    public static int Money = 20;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Addscore()
    {
        zombieskilled++;
    }

    public void StartGame()
    {
        SceneManager.LoadScene("Level 1");
        Fuel.SetCurrentFuelAmount();
    }

    public static void GameOver()
    {
        Debug.Log("Game Over");
        Money += zombieskilled/2;
        SceneManager.LoadScene("Shop");
    }


}
