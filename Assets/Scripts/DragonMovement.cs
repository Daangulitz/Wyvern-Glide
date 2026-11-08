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
    [Tooltip("The calibrate button only works once, so extra presses can't reset the movement. Untick for testing.")]
    public bool calibrateButtonOnlyOnce = true;

    /// <summary>Fired the first time the dragon gets calibrated (unlocked). Use it to resume the Yaw after it was parked.</summary>
    public static event System.Action OnCalibrated;

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

    [Header("Easy Flight")]
    [Tooltip("Bank-and-turn controls: tilt = bank angle, wings and nose level out by themselves. Off = the old rotate-forever controls.")]
    public bool easyFlight = true;
    [Tooltip("Flip this if people say up/down feels reversed.")]
    public bool invertPitch = false;
    [Tooltip("Max bank (sideways lean) in degrees. rollSpeed = how fast the dragon leans (turning is set by turnSpeed).")]
    public float turnSpeed = 70f;
    [Range(10f, 80f)] public float maxBankAngle = 45f;
    [Tooltip("The nose can never go steeper than this (no loops, no flipping upside down).")]
    [Range(10f, 80f)] public float maxPitchAngle = 60f;
    [Tooltip("Deg/sec the nose levels out when the controller is held neutral. 0 = stays where it is.")]
    public float autoLevelPitchSpeed = 20f;
    [Tooltip("Part of the tilt range around neutral that is ignored, so a shaky hand doesn't steer.")]
    [Range(0f, 0.5f)] public float inputDeadzone = 0.12f;
    [Tooltip("1 = linear, higher = finer control near neutral.")]
    [Range(1f, 3f)] public float inputCurve = 1.4f;
    [Tooltip("Higher = reacts faster, lower = smoother.")]
    public float inputSmoothing = 6f;

    [Header("Counter Steer")]
    public bool avoidanceEnabled = true;
    [Tooltip("Layers that count as obstacles. EXCLUDE the dragon's own layer.")]
    public LayerMask obstacleMask = ~0;
    [Tooltip("Length of the front line.")]
    public float frontDistance = 60f;
    [Tooltip("Length of the two side lines.")]
    public float sideDistance = 40f;
    [Tooltip("How far the side lines point away from forward (deg).")]
    [Range(10f, 90f)] public float sideAngle = 45f;
    [Tooltip("How far (deg) the system aims away from the wall at full urgency.")]
    [Range(10f, 120f)] public float maxAvoidAngle = 70f;
    [Tooltip("Max turn the help may make in one go, measured from the heading when it started. 90 = until the dragon flies sideways to its first heading (alongside a wall it flew straight at).")]
    [Range(10f, 180f)] public float maxTotalAvoidTurn = 90f;
    [Tooltip("Side lines have priority. When only the front line hits (or both sides are equally close), always turn left instead of picking the side with the most room.")]
    public bool frontHitAlwaysTurnsLeft = true;
    [Tooltip("Max deg/sec the system can steer at full urgency. Keep above pitchSpeed/rollSpeed.")]
    public float avoidTurnRate = 120f;
    [Tooltip("0 = player input untouched, 1 = player input fully ignored at full urgency.")]
    [Range(0f, 1f)] public float inputSuppression = 0.8f;

    [Header("Floor / Ceiling")]
    public float floorSoftDistance = 25f;        // the one line pointing down; starts pushing the nose up inside this height
    [Range(0f, 1f)] public float floorEscapeY = 0.3f;   // forward.y the system forces at ground contact (~17 deg climb)
    public bool useVirtualCeiling = true;        // there is no real ceiling, so use a height limit
    public float virtualCeilingY = 300f;         // world Y you must not exceed
    public float ceilingSoftDistance = 40f;      // starts pushing the nose down inside this range below the limit
    [Range(0f, 1f)] public float ceilingEscapeY = 0.25f; // downward forward.y forced at the limit (~15 deg dive)

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float flyingPitchThreshold = 2f; // deg, avoids flicker near level

    [Header("Debug")]
    public GameObject debugDragon; // active = cleared to move, inactive = still blocked
    public bool drawGizmos = true;
    [Tooltip("Logs once per second why the dragon steers the way it does (pose lost? avoidance active? height?).")]
    public bool debugLogState = false;
    private float _nextDebugLog = 0f;
    private Vector3 _avoidStartHeading = Vector3.forward; // flat heading when the wall help started
    private bool _avoidActive = false;

    private Quaternion _calibrationRotation = Quaternion.identity;
    private Quaternion _currentControllerRotation = Quaternion.identity;
    private bool _hasCalibrated = false;
    private bool _pendingCalibration = false;
    private float _smoothPitchInput = 0f;
    private float _smoothRollInput = 0f;
    private bool _calibrateButtonUsed = false;
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
        // Only the first press counts, so pressing again can't reset the movement.
        if (calibrateButtonOnlyOnce && _calibrateButtonUsed) return;
        _calibrateButtonUsed = true;

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

        bool wasCalibrated = _hasCalibrated;
        _hasCalibrated = true;
        if (!wasCalibrated) OnCalibrated?.Invoke();
    }

    // ------------------------------------------------------------------
    // Counter steer: a ring of lines around the dragon for walls + 1 line down for the floor
    // ------------------------------------------------------------------

    private static float Smooth(float t)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
    }

    private readonly RaycastHit[] _hits = new RaycastHit[8];

    // Returns the distance to the nearest obstacle, or length when nothing is hit.
    // The dragon's own colliders (wings, body) are ignored.
    private float LineDistance(Vector3 dir, float length)
    {
        int n = Physics.RaycastNonAlloc(transform.position, dir, _hits, length, obstacleMask, QueryTriggerInteraction.Ignore);
        float best = length;

        for (int i = 0; i < n; i++)
        {
            RaycastHit h = _hits[i];
            if (h.rigidbody == rb || h.collider.transform.IsChildOf(transform)) continue;
            if (h.distance < best) best = h.distance;
        }

        return best;
    }

    // Lines around the dragon: front (follows the nose), front-left/right (follow the nose, turned by sideAngle),
    // and straight left/right (flat, 90 degrees) so walls beside the dragon are seen too.
    private void GetWallLines(out Vector3 front, out Vector3 frontLeft, out Vector3 frontRight,
                              out Vector3 left, out Vector3 right)
    {
        front = transform.forward;
        frontRight = Quaternion.AngleAxis(sideAngle, Vector3.up) * front;
        frontLeft = Quaternion.AngleAxis(-sideAngle, Vector3.up) * front;

        Vector3 flat = new Vector3(front.x, 0f, front.z);
        if (flat.sqrMagnitude < 0.0001f) flat = new Vector3(transform.up.x, 0f, transform.up.z);
        if (flat.sqrMagnitude < 0.0001f) flat = Vector3.forward;
        flat.Normalize();

        right = Vector3.Cross(Vector3.up, flat); // 90 degrees to the right of the heading
        left = -right;
    }

    /// <summary>
    /// Returns urgency 0..1 and the direction the dragon should be pointing to stay safe.
    /// wallSteer: -1..1, positive = the system wants to steer right (wall on the left).
    /// vertPush: positive = the system wants the nose UP (floor), negative = DOWN (ceiling), 0 = none.
    /// </summary>
    private float ComputeAvoidance(out Vector3 avoidDir, out float wallSteer, out float vertPush)
    {
        Vector3 pos = transform.position;
        Vector3 fwd = transform.forward;
        Vector3 dir = fwd;
        float urgency;

        // ---- 1. Walls: lines all around the dragon ----
        GetWallLines(out Vector3 frontDir, out Vector3 flDir, out Vector3 frDir, out Vector3 leftDir, out Vector3 rightDir);

        float dF = LineDistance(frontDir, frontDistance);
        float dFL = LineDistance(flDir, sideDistance);
        float dFR = LineDistance(frDir, sideDistance);
        float dL = LineDistance(leftDir, sideDistance);
        float dR = LineDistance(rightDir, sideDistance);

        float pF = Smooth(1f - dF / frontDistance); // 0 = far away, 1 = touching
        float pLeft = Mathf.Max(Smooth(1f - dFL / sideDistance), Smooth(1f - dL / sideDistance));
        float pRight = Mathf.Max(Smooth(1f - dFR / sideDistance), Smooth(1f - dR / sideDistance));

        // Positive = steer right (wall is on the left), negative = steer left.
        float lateral = pLeft - pRight;

        if (pF > 0f)
        {
            // Priority: the side lines decide first. The front line only decides the direction
            // when the sides give no answer (no side hit, or both sides equally close).
            float side;
            if (Mathf.Abs(lateral) > 0.05f)
            {
                side = Mathf.Sign(lateral); // steer away from the closer side wall
            }
            else if (frontHitAlwaysTurnsLeft)
            {
                side = -1f; // only the front hit: always turn left
            }
            else
            {
                float roomLeft = Mathf.Min(dFL, dL);
                float roomRight = Mathf.Min(dFR, dR);
                side = roomLeft >= roomRight ? -1f : 1f;
            }

            // A close wall ahead still makes the turn stronger.
            lateral = side * Mathf.Max(Mathf.Abs(lateral), pF);
        }

        lateral = Mathf.Clamp(lateral, -1f, 1f);
        wallSteer = lateral;
        urgency = Mathf.Abs(lateral); // wall equally close on both sides (corridor) = no steering needed

        if (urgency > 0f)
        {
            // Remember the heading the help started from.
            Vector3 flatFwd = new Vector3(fwd.x, 0f, fwd.z);
            if (!_avoidActive && flatFwd.sqrMagnitude > 0.0001f)
            {
                _avoidStartHeading = flatFwd.normalized;
                _avoidActive = true;
            }

            dir = Quaternion.AngleAxis(lateral * maxAvoidAngle, Vector3.up) * fwd;

            // The help may never turn further than maxTotalAvoidTurn from where it started:
            // at 90 the dragon ends up alongside the wall instead of turning away from it completely.
            if (_avoidActive)
            {
                Vector3 flatDir = new Vector3(dir.x, 0f, dir.z);
                if (flatDir.sqrMagnitude > 0.0001f)
                {
                    float turned = Vector3.SignedAngle(_avoidStartHeading, flatDir, Vector3.up);
                    float limited = Mathf.Clamp(turned, -maxTotalAvoidTurn, maxTotalAvoidTurn);
                    if (!Mathf.Approximately(turned, limited))
                        dir = Quaternion.AngleAxis(limited - turned, Vector3.up) * dir;
                }
            }
        }
        else
        {
            _avoidActive = false; // nothing near any more: next time the help starts fresh
        }

        // ---- 2. Floor (1 line down) and the virtual ceiling: limit how steeply we may point ----
        float minY = -1f, maxY = 1f, floorU = 0f, ceilU = 0f;

        float dDown = LineDistance(Vector3.down, floorSoftDistance);
        if (dDown < floorSoftDistance)
        {
            floorU = Smooth(1f - dDown / floorSoftDistance);
            minY = Mathf.Lerp(-1f, floorEscapeY, floorU);
        }

        if (useVirtualCeiling)
        {
            ceilU = Smooth(Mathf.InverseLerp(virtualCeilingY - ceilingSoftDistance, virtualCeilingY, pos.y));
            if (ceilU > 0f)
                maxY = Mathf.Lerp(1f, -ceilingEscapeY, ceilU);
        }

        if (maxY < minY) maxY = minY;

        vertPush = 0f;
        if (dir.y < minY || dir.y > maxY)
        {
            bool floorWins = dir.y < minY;
            vertPush = floorWins ? 1f : -1f;
            float vertUrgency = floorWins ? floorU : ceilU;

            float y = Mathf.Clamp(dir.y, minY, maxY);

            Vector3 flat = new Vector3(dir.x, 0f, dir.z);
            if (flat.sqrMagnitude < 0.0001f) flat = new Vector3(fwd.x, 0f, fwd.z);
            if (flat.sqrMagnitude < 0.0001f) flat = new Vector3(transform.up.x, 0f, transform.up.z);
            if (flat.sqrMagnitude < 0.0001f) flat = Vector3.forward;

            dir = flat.normalized * Mathf.Sqrt(1f - y * y) + Vector3.up * y;
            urgency = Mathf.Max(urgency, vertUrgency);
            vertPush *= vertUrgency; // keep strength so the caller can scale input suppression
        }

        avoidDir = dir;
        return urgency;
    }

    // Deadzone + curve on a -1..1 tilt value.
    private float ShapeInput(float v)
    {
        float a = Mathf.Abs(v);
        if (a <= inputDeadzone) return 0f;

        a = (a - inputDeadzone) / (1f - inputDeadzone);
        a = Mathf.Pow(a, inputCurve);
        return Mathf.Sign(v) * a;
    }

    /// <summary>
    /// Beginner-friendly flight model:
    /// - tilt the controller sideways = lean (bank) the dragon, and it turns while leaning
    /// - controller neutral = wings level out by themselves
    /// - tilt up/down = nose up/down, limited to maxPitchAngle, and the nose levels out when neutral
    /// pitchInput / rollInput are -1..1 (positive pitchInput = nose down, same direction as the old controls).
    /// </summary>
    private void ApplyEasyFlight(float pitchInput, float rollInput)
    {
        float dt = Time.fixedDeltaTime;

        // Unity euler angles: x = nose DOWN in degrees, y = heading, z = bank (positive = leaning left).
        Vector3 e = transform.rotation.eulerAngles;
        float pitchDown = Mathf.DeltaAngle(0f, e.x);
        float yaw = e.y;
        float bank = Mathf.DeltaAngle(0f, e.z);

        // Pitch: input moves the nose, no input slowly levels it.
        pitchDown += pitchInput * pitchSpeed * dt;
        pitchDown = Mathf.MoveTowards(pitchDown, 0f, autoLevelPitchSpeed * (1f - Mathf.Abs(pitchInput)) * dt);
        pitchDown = Mathf.Clamp(pitchDown, -maxPitchAngle, maxPitchAngle);

        // Bank: tilting right leans right (negative z). Neutral = target 0 = wings level.
        float targetBank = -rollInput * maxBankAngle;
        bank = Mathf.MoveTowards(bank, targetBank, rollSpeed * dt);
        bank = Mathf.Clamp(bank, -maxBankAngle, maxBankAngle);

        // Direct steering: pointing the controller right turns right, left turns left (no pulling up/down needed).
        // The lean above is just for looks and feel.
        yaw += rollInput * turnSpeed * dt;

        transform.rotation = Quaternion.Euler(pitchDown, yaw, bank);
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

        if (easyFlight)
        {
            // deadzone + response curve + smoothing so small hand movements don't cause big swings
            pitchInput = ShapeInput(pitchInput);
            rollInput = ShapeInput(rollInput);
            if (invertPitch) pitchInput = -pitchInput;

            float k = 1f - Mathf.Exp(-inputSmoothing * Time.fixedDeltaTime);
            _smoothPitchInput = Mathf.Lerp(_smoothPitchInput, pitchInput, k);
            _smoothRollInput = Mathf.Lerp(_smoothRollInput, rollInput, k);
            pitchInput = _smoothPitchInput;
            rollInput = _smoothRollInput;
        }

        // Counter steer: work out how much danger there is and fade out player input accordingly.
        float urgency = 0f;
        float wallSteer = 0f;
        float vertPush = 0f;
        Vector3 avoidDir = transform.forward;
        if (avoidanceEnabled)
        {
            urgency = ComputeAvoidance(out avoidDir, out wallSteer, out vertPush);

            // Only fade out input that works AGAINST the help. Steering away from danger is never blocked,
            // and a wall ahead doesn't take away pitch control (or the other way around).
            if (wallSteer != 0f && rollInput * wallSteer < 0f)
                rollInput *= 1f - Mathf.Abs(wallSteer) * inputSuppression;

            // pitchInput: positive = nose down. vertPush: positive = the help wants the nose up.
            if (vertPush != 0f && pitchInput * vertPush > 0f)
                pitchInput *= 1f - Mathf.Abs(vertPush) * inputSuppression;
        }

        if (debugLogState && Time.time >= _nextDebugLog)
        {
            _nextDebugLog = Time.time + 1f;
            Debug.Log($"[SimpleFlight] pose={hasPose} calibrated={_hasCalibrated} height={transform.position.y:F0} " +
                      $"speed={currentSpeed:F0} urgency={urgency:F2} wallSteer={wallSteer:F2} vertPush={vertPush:F2} " +
                      $"pitchIn={pitchInput:F2} rollIn={rollInput:F2}");
        }

        if (easyFlight)
        {
            ApplyEasyFlight(pitchInput, rollInput);
        }
        else
        {
            transform.Rotate(pitchInput * pitchSpeed * Time.fixedDeltaTime,
                rollInput * yawFromRoll * Time.fixedDeltaTime,
                -rollInput * rollSpeed * Time.fixedDeltaTime,
                Space.Self);
        }

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

        Vector3 p = transform.position;
        GetWallLines(out Vector3 f, out Vector3 fl, out Vector3 fr, out Vector3 l, out Vector3 r);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(p, p + f * frontDistance);
        Gizmos.DrawLine(p, p + fl * sideDistance);
        Gizmos.DrawLine(p, p + fr * sideDistance);
        Gizmos.DrawLine(p, p + l * sideDistance);
        Gizmos.DrawLine(p, p + r * sideDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(p, p + Vector3.down * floorSoftDistance);

        if (useVirtualCeiling)
        {
            Gizmos.color = Color.red;
            Vector3 c = new Vector3(p.x, virtualCeilingY, p.z);
            Gizmos.DrawWireCube(c, new Vector3(100f, 0.1f, 100f));
        }
    }
}