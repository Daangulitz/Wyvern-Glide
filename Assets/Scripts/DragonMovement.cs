using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class SimpleFlight : MonoBehaviour
{
    public Rigidbody rb;

    [Header("VR Controller")]
    public Transform controllerTransform;   // assign the tracked controller (right hand)
    public bool calibrateOnStart = true;
    public InputActionReference calibrateAction; // bind to grip/trigger/button in the Input Actions asset

    [Header("Speed")]
    public float currentSpeed = 30f;
    public float minSpeed = 5f;
    public float maxSpeed = 120f;
    public float speedChangeRate = 30f;
    public float pitchInfluence = 1f;
    public float passiveDecay = 2f;

    [Header("Controls")]
    public float pitchSpeed = 45f;
    public float rollSpeed = 60f;
    public float yawFromRoll = 30f;
    public float levelThresholdDeg = 5f;
    [Range(10f, 90f)] public float maxTiltAngle = 45f;

    private Quaternion _calibrationRotation = Quaternion.identity;

    void Reset()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        if (calibrateAction != null)
        {
            calibrateAction.action.Enable();
            calibrateAction.action.performed += OnCalibratePerformed;
        }
    }

    void OnDisable()
    {
        if (calibrateAction != null)
        {
            calibrateAction.action.performed -= OnCalibratePerformed;
            calibrateAction.action.Disable();
        }
    }

    void OnCalibratePerformed(InputAction.CallbackContext ctx)
    {
        Calibrate();
    }

    void Start()
    {
        if (calibrateOnStart) Calibrate();
    }

    public void Calibrate()
    {
        if (controllerTransform != null)
            _calibrationRotation = controllerTransform.rotation;
    }

    void FixedUpdate()
    {
        if (rb == null || controllerTransform == null) return;

        Quaternion relative = Quaternion.Inverse(_calibrationRotation) * controllerTransform.rotation;

        Vector3 relativeEuler = relative.eulerAngles;
        float pitchDegRaw = NormalizeAngle(relativeEuler.x);
        float rollDegRaw = NormalizeAngle(relativeEuler.z);

        float pitchInput = Mathf.Clamp(-pitchDegRaw / maxTiltAngle, -1f, 1f);
        float rollInput = Mathf.Clamp(-rollDegRaw / maxTiltAngle, -1f, 1f);

        transform.Rotate(pitchInput * pitchSpeed * Time.fixedDeltaTime,
                         rollInput * yawFromRoll * Time.fixedDeltaTime,
                         -rollInput * rollSpeed * Time.fixedDeltaTime,
                         Space.Self);

        float pitchRad = Mathf.Asin(Mathf.Clamp(transform.forward.y, -1f, 1f));
        float pitchDeg = pitchRad * Mathf.Rad2Deg;
        float pitchFactor = -pitchDeg / 90f;

        currentSpeed += pitchFactor * pitchInfluence * speedChangeRate * Time.fixedDeltaTime;

        if (Mathf.Abs(pitchDeg) <= levelThresholdDeg)
            currentSpeed -= passiveDecay * Time.fixedDeltaTime;

        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);

        float verticalVel = Vector3.Dot(rb.linearVelocity, Vector3.up);
        rb.linearVelocity = transform.forward * currentSpeed + Vector3.up * verticalVel;
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        return angle;
    }
}