using UnityEngine;
using UnityEngine.InputSystem;

namespace ChaosRider.Animals
{
    [RequireComponent(typeof(Rigidbody))]
    public class AnimalPhysicsController : MonoBehaviour
    {
        public delegate void ImpactEventHandler(float impactForce, Collision collision);

        [Header("Config")]
        [SerializeField] private AnimalDefinition definition;
        [SerializeField] private bool useGaitEngine = true;

        [Header("Fallback Tuning")]
        [SerializeField] private float mass = 650f;
        [SerializeField] private float drag = 0.4f;
        [SerializeField] private float angularDrag = 1.5f;
        [SerializeField] private float maxForwardSpeed = 18f;
        [SerializeField] private float cruiseForce = 2600f;
        [SerializeField] private float chargeForce = 4200f;
        [SerializeField] private float brakeForce = 1800f;
        [SerializeField] private float reverseForce = 900f;
        [SerializeField] private float turnTorque = 320f;
        [SerializeField] private float steeringAssist = 0.2f;
        [SerializeField] private float highSpeedTurnTorqueMultiplier = 0.005f;
        [SerializeField] private float frontPivotTurnForce = 22f;
        [SerializeField] private float rearCounterTurnForce = 18f;
        [SerializeField] private float headingAlignmentTorque = 2.5f;
        [SerializeField] private float maxSteerLeadAngle = 28f;
        [SerializeField] private float highSpeedLeadAngleMultiplier = 0.1f;
        [SerializeField] private Vector3 frontSteeringPivotLocalOffset = new Vector3(0f, -0.15f, 1.2f);
        [SerializeField] private Vector3 rearSteeringPivotLocalOffset = new Vector3(0f, -0.15f, -0.85f);
        [SerializeField] private float buckImpulse = 8f;
        [SerializeField] private float buckTorque = 18f;
        [SerializeField] private float lateralKick = 4f;
        [SerializeField] private float chaosJitterTorque = 6f;
        [SerializeField] private Vector2 buckIntervalRange = new Vector2(0.3f, 0.75f);
        [SerializeField] private float groundedBuckMultiplier = 0.45f;
        [SerializeField] private float airControlMultiplier = 0.2f;
        [SerializeField] private float uprightForce = 10f;
        [SerializeField] private float groundAdhesionForce = 35f;
        [SerializeField] private float extraFallGravity = 18f;
        [SerializeField] private float groundCheckDistance = 1.2f;

        [Header("Debug")]
        [SerializeField] private bool drawVelocityRay = true;
        [SerializeField] private bool drawGroundRay = true;

        private Rigidbody body;
        private GaitEngine gaitEngine;
        private AnimalLocomotionController locomotionController;
        private float nextBuckTime;

        public float LastImpactForce { get; private set; }
        public float PeakImpactForce { get; private set; }
        public float NormalizedSpeed { get; private set; }
        public bool IsGrounded { get; private set; }
        public Rigidbody Body => body;
        public float ForwardSpeed => Vector3.Dot(body.linearVelocity, transform.forward);
        public event ImpactEventHandler Impacted;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            gaitEngine = GetComponent<GaitEngine>();
            locomotionController = GetComponent<AnimalLocomotionController>();
            ApplyDefinition();
            gaitEngine?.Configure(body);
            ScheduleNextBuck();
        }

        private void FixedUpdate()
        {
            var throttle = locomotionController != null ? locomotionController.DesiredThrottle : ReadThrottle();
            var steering = locomotionController != null ? locomotionController.DesiredSteering : ReadSteering();

            UpdateGrounding();
            UpdateVelocityState();

            if (useGaitEngine && gaitEngine != null)
            {
                gaitEngine.Step(throttle, steering, IsGrounded);
            }
            else
            {
                ApplyDrive(throttle);
                ApplySteering(steering);
                ApplyChaos(throttle);
            }

            ApplyStabilityForces();
            ClampForwardSpeed();
            UpdateVelocityState();
        }

        private void Update()
        {
            if (!drawVelocityRay)
            {
                return;
            }

            Debug.DrawRay(transform.position + Vector3.up * 1.2f, body.linearVelocity, Color.red);

            if (drawGroundRay)
            {
                Debug.DrawRay(transform.position + Vector3.up * 0.1f, Vector3.down * groundCheckDistance, IsGrounded ? Color.green : Color.yellow);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            var impactForce = collision.relativeVelocity.magnitude * body.mass;
            LastImpactForce = impactForce;
            PeakImpactForce = Mathf.Max(PeakImpactForce, impactForce);
            Impacted?.Invoke(impactForce, collision);
        }

        private void ApplyDefinition()
        {
            if (definition != null)
            {
                mass = definition.mass;
                drag = definition.drag;
                angularDrag = definition.angularDrag;
                maxForwardSpeed = definition.maxForwardSpeed;
                cruiseForce = definition.cruiseForce;
                chargeForce = definition.chargeForce;
                brakeForce = definition.brakeForce;
                reverseForce = definition.reverseForce;
                turnTorque = definition.turnTorque;
                steeringAssist = definition.steeringAssist;
                buckImpulse = definition.buckImpulse;
                buckTorque = definition.buckTorque;
                lateralKick = definition.lateralKick;
                chaosJitterTorque = definition.chaosJitterTorque;
                buckIntervalRange = definition.buckIntervalRange;
            }

            body.mass = mass;
            body.linearDamping = drag;
            body.angularDamping = angularDrag;
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void ApplyDrive(float throttle)
        {
            var forwardVelocity = ForwardSpeed;
            var force = cruiseForce;

            if (throttle > 0.15f)
            {
                force = Mathf.Lerp(cruiseForce, chargeForce, throttle);
            }
            else if (throttle < -0.15f)
            {
                force = -Mathf.Lerp(reverseForce, brakeForce, Mathf.Abs(throttle));
            }

            if (forwardVelocity < maxForwardSpeed || force < 0f)
            {
                body.AddForce(transform.forward * force * Time.fixedDeltaTime, ForceMode.Acceleration);
            }
        }

        private void ApplySteering(float steering)
        {
            var steeringMultiplier = IsGrounded ? 1f : airControlMultiplier;
            var steerScale = Mathf.Lerp(0.75f, highSpeedTurnTorqueMultiplier, NormalizedSpeed);
            var torque = turnTorque * steering * steerScale * steeringMultiplier * Time.fixedDeltaTime;
            body.AddTorque(Vector3.up * torque, ForceMode.VelocityChange);

            var planarVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
            var planarSpeed = planarVelocity.magnitude;

            if (Mathf.Abs(steering) > 0.01f && planarSpeed > 0.25f)
            {
                var sidewaysVelocity = Vector3.Dot(body.linearVelocity, transform.right);
                body.AddForce(-transform.right * sidewaysVelocity * steeringAssist * steeringMultiplier, ForceMode.Acceleration);

                var steerAngle = steering * maxSteerLeadAngle * Mathf.Lerp(0.75f, highSpeedLeadAngleMultiplier, NormalizedSpeed);
                var desiredDirection = Quaternion.AngleAxis(steerAngle, Vector3.up) * transform.forward;
                desiredDirection = Vector3.ProjectOnPlane(desiredDirection, Vector3.up).normalized;

                var desiredVelocity = desiredDirection * planarSpeed;
                var velocityCorrection = Vector3.ProjectOnPlane(desiredVelocity - planarVelocity, Vector3.up);
                var frontPivotWorld = transform.TransformPoint(frontSteeringPivotLocalOffset);
                var rearPivotWorld = transform.TransformPoint(rearSteeringPivotLocalOffset);
                body.AddForceAtPosition(velocityCorrection * frontPivotTurnForce * steeringMultiplier, frontPivotWorld, ForceMode.Acceleration);
                body.AddForceAtPosition(-velocityCorrection * rearCounterTurnForce * steeringMultiplier, rearPivotWorld, ForceMode.Acceleration);

                var signedAngle = Vector3.SignedAngle(
                    Vector3.ProjectOnPlane(transform.forward, Vector3.up),
                    desiredDirection,
                    Vector3.up);

                var alignmentTorque = signedAngle * headingAlignmentTorque * Mathf.Lerp(0.02f, 0.18f, NormalizedSpeed) * Time.fixedDeltaTime;
                body.AddTorque(Vector3.up * alignmentTorque, ForceMode.VelocityChange);
            }
        }

        private void ApplyChaos(float throttle)
        {
            if (Time.time >= nextBuckTime)
            {
                var impulseScale = Mathf.Lerp(0.85f, 1.35f, Random.value);
                var lateralDirection = Random.value > 0.5f ? 1f : -1f;
                var upwardScale = IsGrounded ? groundedBuckMultiplier : groundedBuckMultiplier * 0.2f;

                body.AddForce(Vector3.up * buckImpulse * impulseScale * upwardScale, ForceMode.Impulse);
                body.AddForce(transform.right * lateralKick * lateralDirection, ForceMode.Impulse);
                body.AddTorque(transform.right * buckTorque * lateralDirection, ForceMode.Impulse);
                body.AddTorque(transform.forward * (buckTorque * 0.35f * -lateralDirection), ForceMode.Impulse);

                ScheduleNextBuck();
            }

            var jitter = Mathf.Lerp(0.6f, 1.3f, Mathf.Abs(throttle)) * (IsGrounded ? 1f : airControlMultiplier);
            var randomTorque = new Vector3(
                Random.Range(-chaosJitterTorque, chaosJitterTorque),
                Random.Range(-chaosJitterTorque * 0.6f, chaosJitterTorque * 0.6f),
                Random.Range(-chaosJitterTorque, chaosJitterTorque));

            body.AddRelativeTorque(randomTorque * jitter * Time.fixedDeltaTime, ForceMode.Acceleration);
        }

        private void ApplyStabilityForces()
        {
            var currentUp = transform.up;
            var uprightTorque = Vector3.Cross(currentUp, Vector3.up) * uprightForce;
            body.AddTorque(uprightTorque, ForceMode.Acceleration);

            if (IsGrounded && body.linearVelocity.y <= 1.5f)
            {
                body.AddForce(Vector3.down * groundAdhesionForce, ForceMode.Acceleration);
                return;
            }

            body.AddForce(Physics.gravity.normalized * extraFallGravity, ForceMode.Acceleration);
        }

        private void UpdateGrounding()
        {
            var rayOrigin = transform.position + Vector3.up * 0.1f;
            IsGrounded = Physics.Raycast(rayOrigin, Vector3.down, groundCheckDistance, ~0, QueryTriggerInteraction.Ignore);
        }

        private void UpdateVelocityState()
        {
            NormalizedSpeed = Mathf.Clamp01(Mathf.Abs(ForwardSpeed) / Mathf.Max(0.01f, maxForwardSpeed));
        }

        private void ClampForwardSpeed()
        {
            var planarVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);

            if (planarVelocity.magnitude <= maxForwardSpeed)
            {
                return;
            }

            var verticalVelocity = Vector3.Project(body.linearVelocity, Vector3.up);
            body.linearVelocity = planarVelocity.normalized * maxForwardSpeed + verticalVelocity;
        }

        private void ScheduleNextBuck()
        {
            nextBuckTime = Time.time + Random.Range(buckIntervalRange.x, buckIntervalRange.y);
        }

        private static float ReadThrottle()
        {
            var throttle = 0f;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                {
                    throttle += 1f;
                }

                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                {
                    throttle -= 1f;
                }
            }

            return Mathf.Clamp(throttle, -1f, 1f);
        }

        private static float ReadSteering()
        {
            var steering = 0f;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                {
                    steering -= 1f;
                }

                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                {
                    steering += 1f;
                }
            }

            return Mathf.Clamp(steering, -1f, 1f);
        }
    }
}
