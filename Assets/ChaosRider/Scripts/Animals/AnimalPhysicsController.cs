using UnityEngine;
using UnityEngine.InputSystem;

namespace ChaosRider.Animals
{
    [RequireComponent(typeof(Rigidbody))]
    public class AnimalPhysicsController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private AnimalDefinition definition;

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
        [SerializeField] private float buckImpulse = 8f;
        [SerializeField] private float buckTorque = 18f;
        [SerializeField] private float lateralKick = 4f;
        [SerializeField] private float chaosJitterTorque = 6f;
        [SerializeField] private Vector2 buckIntervalRange = new Vector2(0.3f, 0.75f);

        [Header("Debug")]
        [SerializeField] private bool drawVelocityRay = true;

        private Rigidbody body;
        private float nextBuckTime;

        public float LastImpactForce { get; private set; }
        public float PeakImpactForce { get; private set; }
        public float NormalizedSpeed { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            ApplyDefinition();
            ScheduleNextBuck();
        }

        private void FixedUpdate()
        {
            var throttle = ReadThrottle();
            var steering = ReadSteering();

            ApplyDrive(throttle);
            ApplySteering(steering);
            ApplyChaos(throttle);
            ClampForwardSpeed();
        }

        private void Update()
        {
            if (!drawVelocityRay)
            {
                return;
            }

            Debug.DrawRay(transform.position + Vector3.up * 1.2f, body.linearVelocity, Color.red);
        }

        private void OnCollisionEnter(Collision collision)
        {
            var impactForce = collision.relativeVelocity.magnitude * body.mass;
            LastImpactForce = impactForce;
            PeakImpactForce = Mathf.Max(PeakImpactForce, impactForce);
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
            var forwardVelocity = Vector3.Dot(body.linearVelocity, transform.forward);
            NormalizedSpeed = Mathf.Clamp01(Mathf.Abs(forwardVelocity) / Mathf.Max(0.01f, maxForwardSpeed));

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
            var steerScale = Mathf.Lerp(1.2f, 0.35f, NormalizedSpeed);
            var torque = turnTorque * steering * steerScale * Time.fixedDeltaTime;
            body.AddTorque(Vector3.up * torque, ForceMode.VelocityChange);

            if (Mathf.Abs(steering) > 0.01f)
            {
                var sidewaysVelocity = Vector3.Dot(body.linearVelocity, transform.right);
                body.AddForce(-transform.right * sidewaysVelocity * steeringAssist, ForceMode.Acceleration);
            }
        }

        private void ApplyChaos(float throttle)
        {
            if (Time.time >= nextBuckTime)
            {
                var impulseScale = Mathf.Lerp(0.85f, 1.35f, Random.value);
                var lateralDirection = Random.value > 0.5f ? 1f : -1f;

                body.AddForce(Vector3.up * buckImpulse * impulseScale, ForceMode.Impulse);
                body.AddForce(transform.right * lateralKick * lateralDirection, ForceMode.Impulse);
                body.AddTorque(transform.right * buckTorque * lateralDirection, ForceMode.Impulse);
                body.AddTorque(transform.forward * (buckTorque * 0.35f * -lateralDirection), ForceMode.Impulse);

                ScheduleNextBuck();
            }

            var jitter = Mathf.Lerp(0.6f, 1.3f, Mathf.Abs(throttle));
            var randomTorque = new Vector3(
                Random.Range(-chaosJitterTorque, chaosJitterTorque),
                Random.Range(-chaosJitterTorque * 0.6f, chaosJitterTorque * 0.6f),
                Random.Range(-chaosJitterTorque, chaosJitterTorque));

            body.AddRelativeTorque(randomTorque * jitter * Time.fixedDeltaTime, ForceMode.Acceleration);
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
