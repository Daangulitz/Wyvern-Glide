using UnityEngine;
using UnityEngine.InputSystem;
using YawVR;

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

    [Header("Counter Steer")]
    public bool avoidanceEnabled = true;
    [Tooltip("Layers that count as obstacles. EXCLUDE the dragon's own layer.")]
    public LayerMask obstacleMask = ~0;
    [Tooltip("Radius of the 'fatness' probe used to look ahead for walls.")]
    public float probeRadius = 3f;
    public float minLookahead = 25f;
    [Tooltip("Seconds of flight time to look ahead (distance = speed * this).")]
    public float lookaheadTime = 1.5f;
    [Tooltip("Max deg/sec the system can steer at full urgency. Keep above pitchSpeed/rollSpeed.")]
    public float avoidTurnRate = 120f;
    [Tooltip("0 = player input untouched, 1 = player input fully ignored at full urgency.")]
    [Range(0f, 1f)] public float inputSuppression = 0.8f;

    [Header("Floor / Ceiling")]
    public float floorSoftDistance = 25f;        // starts pushing the nose up inside this height
    [Range(0f, 1f)] public float floorEscapeY = 0.3f;   // forward.y the system forces at ground contact (~17 deg climb)
    public bool useVirtualCeiling = true;        // there is no real ceiling, so use a height limit
    public float virtualCeilingY = 300f;         // world Y you must not exceed
    public float ceilingSoftDistance = 40f;      // starts pushing the nose down inside this range (real and virtual)
    [Range(0f, 1f)] public float ceilingEscapeY = 0.25f; // downward forward.y forced at the limit (~15 deg dive)

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float flyingPitchThreshold = 2f; // deg, avoids flicker near level

    [Header("Debug")]
    public GameObject debugDragon; // active = cleared to move, inactive = still blocked
    public bool drawGizmos = true;

    private Quaternion _calibrationRotation = Quaternion.identity;
    private Quaternion _currentControllerRotation = Quaternion.identity;
    private bool _hasCalibrated = false;
    private bool _pendingCalibration = false;
    private bool _calibratedWithoutPose = false; // button pressed while no controller rotation was available

    private MotionCompensation _motionCompensation;

    private void Reset()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        if (controllerRotationAction != null) controllerRotationAction.action.Enable();

        if (calibrateAction != null)
        {
            calibrateAction.action.Enable();
            calibrateAction.action.performed += OnCalibratePerformed;
        }
    }

    private void OnDisable()
    {
        if (controllerRotationAction != null) controllerRotationAction.action.Disable();

        if (calibrateAction != null)
        {
            calibrateAction.action.performed -= OnCalibratePerformed;
            calibrateAction.action.Disable();
        }
    }

    private void OnCalibratePerformed(InputAction.CallbackContext ctx)
    {
        // Pressing the button always unlocks the dragon, even without a controller rotation.
        if (controllerRotationAction != null)
            _currentControllerRotation = controllerRotationAction.action.ReadValue<Quaternion>();
        else
            _currentControllerRotation = Quaternion.identity;

        Calibrate();
        _pendingCalibration = false;
    }

    private void Start()
    {
        if (calibrateOnStart)
        {
            _pendingCalibration = true;
            _hasCalibrated = false;
        }

        if (animator == null) animator = GetComponent<Animator>();

        if (_motionCompensation == null)
        {
            _motionCompensation = FindAnyObjectByType<MotionCompensation>();
            _motionCompensation.UpdateOffset();
        }
    }

    // Debug dragon mirrors the state: active = everything OK (calibrated + valid pose), inactive = something wrong.
    private void SetDebugState(bool ok)
    {
        if (debugDragon != null && debugDragon.activeSelf != ok)
            debugDragon.SetActive(ok);
    }

    private static bool HasPoseData(Quaternion q)
    {
        return (q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w) > 0.5f;
    }

    private void Calibrate()
    {
        bool hasPose = HasPoseData(_currentControllerRotation);

        // No rotation available -> neutral stick; fly straight forward until real data shows up.
        _calibrationRotation = hasPose ? _currentControllerRotation : Quaternion.identity;
        _calibratedWithoutPose = !hasPose;
        _hasCalibrated = true;
    }

    // ------------------------------------------------------------------
    // Counter steer
    // ------------------------------------------------------------------

    private static float Smooth(float t)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
    }

    private float FeelerDistance(Vector3 dir, float length)
    {
        if (Physics.Raycast(transform.position, dir, out RaycastHit h, length, obstacleMask, QueryTriggerInteraction.Ignore))
            return h.distance;
        return length;
    }

    /// <summary>
    /// Returns urgency 0..1 and the direction the dragon should be pointing to stay safe.
    /// 0 = nothing to avoid, 1 = about to hit something.
    /// </summary>
    private float ComputeAvoidance(out Vector3 avoidDir)
    {
        Vector3 pos = transform.position;
        Vector3 fwd = transform.forward;
        Vector3 dir = fwd;
        float urgency = 0f;

        // ---- 1. Walls / anything straight ahead (sphere cast along flight path) ----
        float look = Mathf.Max(minLookahead, currentSpeed * lookaheadTime);
        if (Physics.SphereCast(pos, probeRadius, fwd, out RaycastHit hit, look, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            urgency = Smooth(1f - hit.distance / look);

            // Slide along the surface instead of bouncing straight back.
            Vector3 slide = Vector3.ProjectOnPlane(fwd, hit.normal);

            if (slide.sqrMagnitude < 0.05f)
            {
                // Head-on: no obvious slide direction, so turn toward whichever side is more open.
                Vector3 rightRay = Quaternion.AngleAxis(45f, transform.up) * fwd;
                Vector3 leftRay = Quaternion.AngleAxis(-45f, transform.up) * fwd;
                slide = FeelerDistance(rightRay, look) >= FeelerDistance(leftRay, look) ? rightRay : leftRay;
            }

            dir = slide.normalized;
        }

        // ---- 2. Floor and ceiling: limit how steeply we may point as we get close ----
        float minY = -1f, maxY = 1f, vertUrgency = 0f;

        if (Physics.Raycast(pos, Vector3.down, out RaycastHit floorHit, floorSoftDistance, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            float fu = Smooth(1f - floorHit.distance / floorSoftDistance);
            minY = Mathf.Lerp(-1f, floorEscapeY, fu);
            vertUrgency = fu;
        }

        float cu = 0f;
        if (Physics.Raycast(pos, Vector3.up, out RaycastHit ceilHit, ceilingSoftDistance, obstacleMask, QueryTriggerInteraction.Ignore))
            cu = Smooth(1f - ceilHit.distance / ceilingSoftDistance);

        if (useVirtualCeiling)
            cu = Mathf.Max(cu, Smooth(Mathf.InverseLerp(virtualCeilingY - ceilingSoftDistance, virtualCeilingY, pos.y)));

        if (cu > 0f)
        {
            maxY = Mathf.Lerp(1f, -ceilingEscapeY, cu);
            vertUrgency = Mathf.Max(vertUrgency, cu);
        }

        if (maxY < minY) maxY = minY; // squeezed between floor and ceiling: floor wins

        if (dir.y < minY || dir.y > maxY)
        {
            float y = Mathf.Clamp(dir.y, minY, maxY);

            Vector3 flat = new Vector3(dir.x, 0f, dir.z);
            if (flat.sqrMagnitude < 0.0001f) flat = new Vector3(fwd.x, 0f, fwd.z);
            if (flat.sqrMagnitude < 0.0001f) flat = new Vector3(transform.up.x, 0f, transform.up.z);
            if (flat.sqrMagnitude < 0.0001f) flat = Vector3.forward;

            dir = flat.normalized * Mathf.Sqrt(1f - y * y) + Vector3.up * y;
            urgency = Mathf.Max(urgency, vertUrgency);
        }

        avoidDir = dir;
        return urgency;
    }

    private void FixedUpdate()
    {
        if (rb == null)
        {
            SetDebugState(false);
            return;
        }

        // Read the controller rotation if we have an action; otherwise there is simply no stick input.
        bool hasPose = false;
        if (controllerRotationAction != null)
        {
            _currentControllerRotation = controllerRotationAction.action.ReadValue<Quaternion>();
            hasPose = HasPoseData(_currentControllerRotation);
        }

        // Auto-calibrate on start, but only once the controller actually delivers a rotation.
        if (_pendingCalibration && hasPose)
        {
            Calibrate();
            _pendingCalibration = false;
        }

        // Button was pressed before any rotation existed: calibrate as soon as real data appears.
        if (_hasCalibrated && _calibratedWithoutPose && hasPose)
        {
            Calibrate();
        }

        // calibrated -> debug dragon on, not calibrated -> off
        SetDebugState(_hasCalibrated);

        // locked until the calibrate button (or auto-calibration) releases it
        if (!_hasCalibrated)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        // No rotation available = neutral stick (straight forward flight).
        Quaternion relative = hasPose
            ? Quaternion.Inverse(_calibrationRotation) * _currentControllerRotation
            : Quaternion.identity;

        // Where is the controller's "tip" pointing, relative to neutral?
        // Direct joystick-style read: pulling the tip up = climb, tilting sideways = roll.
        Vector3 stickDir = relative * Vector3.forward;

        float maxTiltRad = maxTiltAngle * Mathf.Deg2Rad;
        float pitchInput = Mathf.Clamp(stickDir.y / Mathf.Sin(maxTiltRad), -1f, 1f);
        float rollInput = Mathf.Clamp(stickDir.x / Mathf.Sin(maxTiltRad), -1f, 1f);

        // Counter steer: work out how much danger there is and fade out player input accordingly.
        float urgency = 0f;
        Vector3 avoidDir = transform.forward;
        if (avoidanceEnabled)
        {
            urgency = ComputeAvoidance(out avoidDir);
            float inputScale = 1f - urgency * inputSuppression;
            pitchInput *= inputScale;
            rollInput *= inputScale;
        }

        transform.Rotate(pitchInput * pitchSpeed * Time.fixedDeltaTime,
            rollInput * yawFromRoll * Time.fixedDeltaTime,
            -rollInput * rollSpeed * Time.fixedDeltaTime,
            Space.Self);

        // Steer toward the safe direction (keeps current roll as much as possible).
        if (urgency > 0f)
        {
            Quaternion target = Quaternion.FromToRotation(transform.forward, avoidDir) * transform.rotation;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target,
                avoidTurnRate * urgency * Time.fixedDeltaTime);
        }

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

        YawController.Instance.TrackerObject.SetRotation(transform.localEulerAngles);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        float look = Mathf.Max(minLookahead, currentSpeed * lookaheadTime);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + transform.forward * look, probeRadius);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * look);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * floorSoftDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * ceilingSoftDistance);

        if (useVirtualCeiling)
        {
            Vector3 c = new Vector3(transform.position.x, virtualCeilingY, transform.position.z);
            Gizmos.DrawWireCube(c, new Vector3(100f, 0.1f, 100f));
        }
    }
}