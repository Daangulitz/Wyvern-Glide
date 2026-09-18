using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class SimpleFlight : MonoBehaviour
{
    public Rigidbody rb;

    [Header("VR Controller (Input System)")]
    public InputActionReference controllerRotationAction; // bind to <XRController>{RightHand}/deviceRotation
    public bool calibrateOnStart = true;
    public InputActionReference calibrateAction; // bind to grip/primaryButton press

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

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float flyingPitchThreshold = 2f; // deg, avoids flicker near level

    [Header("Debug")]
    public GameObject debugDragon; // active = cleared to move, inactive = still blocked

    private Quaternion _calibrationRotation = Quaternion.identity;
    private Quaternion _currentControllerRotation = Quaternion.identity;
    private bool _hasCalibrated = false;
    private bool _pendingCalibration = false;

    void Reset()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        if (controllerRotationAction != null)
            controllerRotationAction.action.Enable();

        if (calibrateAction != null)
        {
            calibrateAction.action.Enable();
            calibrateAction.action.performed += OnCalibratePerformed;
        }
    }

    void OnDisable()
    {
        if (controllerRotationAction != null)
            controllerRotationAction.action.Disable();

        if (calibrateAction != null)
        {
            calibrateAction.action.performed -= OnCalibratePerformed;
            calibrateAction.action.Disable();
        }
    }

    void OnCalibratePerformed(InputAction.CallbackContext ctx)
    {
        if (IsValidRotation(_currentControllerRotation))
        {
            Calibrate();
            _hasCalibrated = true;
        }
    }

    void Start()
    {
        if (calibrateOnStart)
        {
            _pendingCalibration = true;
            _hasCalibrated = false;
        }

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void Calibrate()
    {
        _calibrationRotation = _currentControllerRotation;
        _hasCalibrated = true;
    }

    // A genuine unit quaternion has (x²+y²+z²+w²) ≈ 1. Garbage/uninitialized tracking data won't.
    bool IsValidRotation(Quaternion q)
    {
        float sqrMag = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
        return sqrMag > 0.9f && sqrMag < 1.1f;
    }

    void FixedUpdate()
    {
        if (rb == null || controllerRotationAction == null) return;
        if (_hasCalibrated == false) return; // don't move until we've calibrated

        _currentControllerRotation = controllerRotationAction.action.ReadValue<Quaternion>();

        bool poseIsValid = IsValidRotation(_currentControllerRotation);

        if (_pendingCalibration && poseIsValid)
        {
            Calibrate();
            _pendingCalibration = false;
        }

        // debug: dragon active only once we're actually cleared to move
        if (debugDragon != null)
            debugDragon.SetActive(_hasCalibrated && poseIsValid);

        // don't move at all until we've calibrated against a real, valid pose
        if (!_hasCalibrated || !poseIsValid)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        Quaternion relative = Quaternion.Inverse(_calibrationRotation) * _currentControllerRotation;

        // Where is the controller's "tip" pointing, relative to neutral?
        // Direct joystick-style read: pulling the tip up = climb, tilting sideways = roll.
        Vector3 stickDir = relative * Vector3.forward;

        float maxTiltRad = maxTiltAngle * Mathf.Deg2Rad;
        float pitchInput = Mathf.Clamp(stickDir.y / Mathf.Sin(maxTiltRad), -1f, 1f);
        float rollInput = Mathf.Clamp(stickDir.x / Mathf.Sin(maxTiltRad), -1f, 1f);

        transform.Rotate(pitchInput * pitchSpeed * Time.fixedDeltaTime,
            rollInput * yawFromRoll * Time.fixedDeltaTime,
            -rollInput * rollSpeed * Time.fixedDeltaTime,
            Space.Self);

        // compute pitch angle in degrees: positive = nose up, negative = nose down
        float pitchRad = Mathf.Asin(Mathf.Clamp(transform.forward.y, -1f, 1f));
        float pitchDeg = pitchRad * Mathf.Rad2Deg;

        // Update animator: true = climbing, false = diving.
        // Hysteresis band around 0 (±flyingPitchThreshold) avoids flicker near level flight.
        if (animator != null)
        {
            if (pitchDeg > flyingPitchThreshold)
                animator.SetBool("Flying", true);
            else if (pitchDeg < -flyingPitchThreshold)
                animator.SetBool("Flying", false);
            // else: keep whatever the last state was
        }

        // pitchFactor: positive when diving (pitchDeg < 0), negative when climbing (pitchDeg > 0)
        float pitchFactor = -pitchDeg / 90f;

        currentSpeed += pitchFactor * pitchInfluence * speedChangeRate * Time.fixedDeltaTime;

        if (Mathf.Abs(pitchDeg) <= levelThresholdDeg)
        {
            currentSpeed -= passiveDecay * Time.fixedDeltaTime;
        }

        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);

        // no gravity fighting: pure forward-vector flight, level flight stays level
        rb.linearVelocity = transform.forward * currentSpeed;
    }
}