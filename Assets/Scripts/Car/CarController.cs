using UnityEngine;
using UnityEngine.InputSystem;
using YawVR;

public class CarController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform steeringWheel;
    [SerializeField] private DashBoardController dashController;
    [SerializeField] private Rigidbody rb;

    [Header("Engine")]
    [SerializeField] private AnimationCurve torqueCurve;
    [SerializeField] private float maxRPM = 7000f;
    [SerializeField] private float idleRPM = 800f;
    [SerializeField] private float maxMotorTorque = 1500f;
    [SerializeField] private float finalDriveRatio = 4.1f;

    [Header("Audio")]
    [SerializeField] private AudioSource engineAudioSource;
    [SerializeField] private AudioSource grindingAudioSource;
    [SerializeField] private AudioClip gearGrindClip;

    [Header("Input Actions (G29 Compatible)")]
    [SerializeField] private InputActionReference steeringInput;
    [SerializeField] private InputActionReference accelerateInput;
    [SerializeField] private InputActionReference brakeInput;
    [SerializeField] private InputActionReference gearUp;
    [SerializeField] private InputActionReference gearDown;

    [Header("Wheel Colliders")]
    [SerializeField] private WheelCollider frontLeftWheel;
    [SerializeField] private WheelCollider frontRightWheel;
    [SerializeField] private WheelCollider rearLeftWheel;
    [SerializeField] private WheelCollider rearRightWheel;

    [Header("Steering")]
    [SerializeField] private float maxSteerAngle = 30f;
    [SerializeField] private float maxWheelRotation = 450f; 

    [Header("Transmission")]
    [SerializeField] private float[] gearRatios = { 0f, -1.5f, 2.5f, 1.8f, 1.2f };
    [SerializeField] private float[] minSpeedsForGears = { 0f, 0f, 0f, 25f, 55f };

    private int currentGearIndex = 0;
    private float engineRPM;
    private bool isGrinding;

    #region Unity Lifecycle

    private void OnEnable()
    {
        EnableActions(true);
        gearUp.action.performed += HandleGearUp;
        gearDown.action.performed += HandleGearDown;
    }

    private void OnDisable()
    {
        gearUp.action.performed -= HandleGearUp;
        gearDown.action.performed -= HandleGearDown;
        EnableActions(false);
    }

    private void EnableActions(bool state)
    {
        if (state)
        {
            steeringInput.action.Enable();
            accelerateInput.action.Enable();
            brakeInput.action.Enable();
            gearUp.action.Enable();
            gearDown.action.Enable();
        }
        else
        {
            steeringInput.action.Disable();
            accelerateInput.action.Disable();
            brakeInput.action.Disable();
            gearUp.action.Disable();
            gearDown.action.Disable();
        }
    }

    private void LateUpdate()
    {
        if (YawController.Instance() != null)
        {
            YawController.Instance().TrackerObject.transform.rotation = transform.rotation;
        }
    }

    private void FixedUpdate()
    {
        // Check if engine exists and is running (assuming Engine is a static class)
        // If Engine script is missing, this line might need adjustment
        if (!Engine.isEngineRunning) return;

        if (currentGearIndex == 0) StopGrindingSound();
        
        // G29 Normalization: Pedals often range -1 to 1. 
        // We use InverseLerp to map -1 to 0 and 1 to 1.
        float rawSteer = steeringInput.action.ReadValue<float>();
        float rawThrottle = accelerateInput.action.ReadValue<float>();
        float rawBrake = brakeInput.action.ReadValue<float>();

        // Normalize inputs for G29 hardware
        float throttle = Mathf.InverseLerp(-1f, 1f, rawThrottle);
        float brake = Mathf.InverseLerp(-1f, 1f, rawBrake);

        HandleSteering(rawSteer);
        HandleBraking(brake);

        if (!isGrinding)
        {
            HandleEngine(throttle);
        }
    }

    #endregion

    #region Vehicle Logic

    private void HandleSteering(float steerInput)
    {
        float steerAngle = steerInput * maxSteerAngle;
        frontLeftWheel.steerAngle = steerAngle;
        frontRightWheel.steerAngle = steerAngle;

        // Visual rotation of the steering wheel model
        float visualRotation = steerInput * maxWheelRotation;
        steeringWheel.localRotation = Quaternion.Euler(0f, 0f, -visualRotation);
    }

    private void HandleBraking(float brakeValue)
    {
        float brakeTorque = brakeValue * maxMotorTorque;
        rearLeftWheel.brakeTorque = brakeTorque;
        rearRightWheel.brakeTorque = brakeTorque;
        
        // Apply slight braking to front for stability
        frontLeftWheel.brakeTorque = brakeTorque * 0.5f;
        frontRightWheel.brakeTorque = brakeTorque * 0.5f;
    }

    private void HandleEngine(float throttle)
    {
        float speedKmh = rb.linearVelocity.magnitude * 3.6f;

        // Auto-Neutral stall logic
        if (currentGearIndex > 2 && speedKmh < minSpeedsForGears[currentGearIndex] - 10f)
        {
            ForceNeutral();
            return;
        }

        float wheelRPM = Mathf.Abs((rearLeftWheel.rpm + rearRightWheel.rpm) * 0.5f);
        float targetRPM = wheelRPM * Mathf.Abs(gearRatios[currentGearIndex]) * finalDriveRatio;

        engineRPM = Mathf.Lerp(engineRPM, Mathf.Max(targetRPM, idleRPM), 0.1f);

        float normalizedRPM = Mathf.Clamp01(engineRPM / maxRPM);
        float torqueMultiplier = torqueCurve.Evaluate(normalizedRPM);

        float motorTorque = throttle * maxMotorTorque * gearRatios[currentGearIndex] * torqueMultiplier;

        rearLeftWheel.motorTorque = motorTorque;
        rearRightWheel.motorTorque = motorTorque;

        // Audio pitch based on RPM
        if (engineAudioSource != null)
        {
            engineAudioSource.pitch = Mathf.Lerp(0.8f, 2.5f, normalizedRPM);
        }

        DrainFuel(motorTorque);
    }

    #endregion

    #region Gear Logic

    private void HandleGearUp(InputAction.CallbackContext ctx)
    {
        int next = currentGearIndex;
        if (currentGearIndex == 1) next = 0; // R -> N
        else if (currentGearIndex == 0) next = 2; // N -> 1st
        else if (currentGearIndex < gearRatios.Length - 1) next++;

        TrySetGear(next);
    }

    private void HandleGearDown(InputAction.CallbackContext ctx)
    {
        int next = currentGearIndex;
        if (currentGearIndex > 2) next--; // 3rd -> 2nd
        else if (currentGearIndex == 2) next = 0; // 1st -> N
        else if (currentGearIndex == 0) next = 1; // N -> R

        TrySetGear(next);
    }

    private void TrySetGear(int gearIndex)
    {
        float speed = rb.linearVelocity.magnitude * 3.6f;

        // Prevent shifting into high gears at low speed
        if (gearIndex >= 2 && speed < minSpeedsForGears[gearIndex])
        {
            PlayGrindingSound();
            return;
        }

        // Prevent shifting into Reverse while moving fast
        if (gearIndex == 1 && speed > 10f)
        {
            PlayGrindingSound();
            return;
        }

        StopGrindingSound();
        currentGearIndex = gearIndex;
        if (dashController != null) dashController.SetGearMessage(GearLabel(gearIndex));
    }

    private void ForceNeutral()
    {
        PlayGrindingSound();
        currentGearIndex = 0;
        if (dashController != null) dashController.SetGearMessage("N");
    }

    private string GearLabel(int index)
    {
        return index switch
        {
            0 => "N",
            1 => "R",
            2 => "1st",
            3 => "2nd",
            4 => "3rd",
            _ => "?"
        };
    }

    #endregion

    #region Audio / Fuel

    private void PlayGrindingSound()
    {
        if (isGrinding || grindingAudioSource == null || gearGrindClip == null) return;
        
        grindingAudioSource.clip = gearGrindClip;
        grindingAudioSource.Play();
        isGrinding = true;

        Invoke(nameof(StopGrindingSound), 1f);
    }

    private void StopGrindingSound()
    {
        isGrinding = false;
        if (grindingAudioSource != null && grindingAudioSource.isPlaying)
            grindingAudioSource.Stop();
    }

    private void DrainFuel(float motorTorque)
    {
        // Assuming Fuel class has a static field currentFuelAmount
        if (Fuel.currentFuelAmount <= 0f)
        {
            Engine.TurnEngineOff();
            return;
        }

        Fuel.currentFuelAmount -= Mathf.Abs(motorTorque) * 0.00005f * Time.fixedDeltaTime;
    }

    #endregion
}