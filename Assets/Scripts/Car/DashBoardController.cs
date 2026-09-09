using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DashBoardController : MonoBehaviour
{
    [SerializeField] private GameObject HUD;
    [SerializeField] private TMP_Text gearText;
    [SerializeField] private Rigidbody carRigidbody;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private Slider fuelslider;
    
    private float currentSpeed = 0;


    private void Start()
    {
        fuelslider.maxValue = Fuel.maxfuelAmount;
    }

    public void SetGearMessage(string currentGear)
    {
        char firstChar = currentGear[0];
        gearText.text = firstChar.ToString();
    }

    public void SetFuelmax(float currentFuel)
    {
        fuelslider.maxValue = Fuel.maxfuelAmount;
    }

    public void Update()
    {
        
        float speed = carRigidbody.linearVelocity.magnitude * 3.6f;
        float direction = Vector3.Dot(carRigidbody.linearVelocity, transform.forward);

        int displayedSpeed = Mathf.RoundToInt(speed);

        speedText.text = direction < 0 ? $"-{displayedSpeed}" : $"{displayedSpeed}";

        fuelslider.value = Fuel.currentFuelAmount;
    }
}
