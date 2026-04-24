using UnityEngine;

namespace ChaosRider.Animals
{
    [RequireComponent(typeof(Rigidbody))]
    public class GaitEngine : MonoBehaviour
    {
        private enum VirtualLeg
        {
            FrontLeft = 0,
            FrontRight = 1,
            RearLeft = 2,
            RearRight = 3,
        }

        [Header("Profiles")]
        [SerializeField] private AnimalProfile animalProfile = new AnimalProfile();
        [SerializeField] private GaitProfile gaitProfile = new GaitProfile();

        [Header("Debug")]
        [SerializeField] private bool drawContactPoints = true;

        private Rigidbody body;
        private float gaitPhase;

        public float GaitPhase => gaitPhase;
        public GaitType CurrentGait => gaitProfile.gaitType;

        public void Configure(Rigidbody targetBody)
        {
            body = targetBody;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        public void Step(float throttleInput, float steeringInput, bool isGrounded)
        {
            if (body == null)
            {
                return;
            }

            var speedIntent = Mathf.Clamp01(Mathf.Abs(throttleInput));

            if (!isGrounded || speedIntent < gaitProfile.idleThreshold)
            {
                ApplyIdleSettling();
                return;
            }

            var cycleDuration = Mathf.Lerp(gaitProfile.cycleDurationAtLowSpeed, gaitProfile.cycleDurationAtHighSpeed, speedIntent);
            gaitPhase = Mathf.Repeat(gaitPhase + Time.fixedDeltaTime / Mathf.Max(0.05f, cycleDuration), 1f);

            var frontLoad = 0f;
            var rearLoad = 0f;
            var leftLoad = 0f;
            var rightLoad = 0f;

            ApplyLeg(VirtualLeg.FrontLeft, gaitProfile.frontLeftPhaseOffset, true, true, throttleInput, steeringInput, speedIntent, ref frontLoad, ref leftLoad);
            ApplyLeg(VirtualLeg.FrontRight, gaitProfile.frontRightPhaseOffset, true, false, throttleInput, steeringInput, speedIntent, ref frontLoad, ref rightLoad);
            ApplyLeg(VirtualLeg.RearLeft, gaitProfile.rearLeftPhaseOffset, false, true, throttleInput, steeringInput, speedIntent, ref rearLoad, ref leftLoad);
            ApplyLeg(VirtualLeg.RearRight, gaitProfile.rearRightPhaseOffset, false, false, throttleInput, steeringInput, speedIntent, ref rearLoad, ref rightLoad);

            var rollImbalance = rightLoad - leftLoad;
            var pitchImbalance = frontLoad - rearLoad;

            body.AddTorque(transform.forward * (-rollImbalance * gaitProfile.rollTorque), ForceMode.Acceleration);
            body.AddTorque(transform.right * (pitchImbalance * gaitProfile.pitchTorque), ForceMode.Acceleration);
        }

        private void ApplyLeg(VirtualLeg leg, float phaseOffset, bool isFront, bool isLeft, float throttleInput, float steeringInput, float speedIntent, ref float foreAftLoad, ref float lateralLoad)
        {
            var legPhase = Mathf.Repeat(gaitPhase + phaseOffset, 1f);
            var inStance = legPhase < gaitProfile.stanceFraction;
            var contactPoint = GetContactPoint(isFront, isLeft);

            if (drawContactPoints)
            {
                Debug.DrawRay(contactPoint, Vector3.up * 0.4f, inStance ? Color.green : Color.gray);
            }

            if (!inStance)
            {
                return;
            }

            var stanceT = legPhase / gaitProfile.stanceFraction;
            var loadPulse = Mathf.Sin(stanceT * Mathf.PI);
            var supportBias = isFront ? gaitProfile.frontSupportBias : gaitProfile.rearSupportBias;
            var driveBias = isFront ? Mathf.Lerp(0.35f, 0.45f, 1f - animalProfile.hindDriveBias) : animalProfile.hindDriveBias;

            var supportForce = Vector3.up * gaitProfile.supportForce * loadPulse * supportBias;
            body.AddForceAtPosition(supportForce, contactPoint, ForceMode.Acceleration);

            var driveForceMagnitude = throttleInput >= 0f
                ? gaitProfile.driveForce * throttleInput * loadPulse * driveBias
                : gaitProfile.brakingForce * throttleInput * loadPulse * supportBias;
            body.AddForceAtPosition(transform.forward * driveForceMagnitude, contactPoint, ForceMode.Acceleration);

            var steerStrength = steeringInput * loadPulse * speedIntent;
            if (isFront)
            {
                body.AddForceAtPosition(transform.right * gaitProfile.frontSteerForce * steerStrength, contactPoint, ForceMode.Acceleration);
            }
            else
            {
                body.AddForceAtPosition(-transform.right * gaitProfile.rearCounterSteerForce * steerStrength, contactPoint, ForceMode.Acceleration);
            }

            foreAftLoad += loadPulse * supportBias;
            lateralLoad += isLeft ? -loadPulse : loadPulse;
        }

        private Vector3 GetContactPoint(bool isFront, bool isLeft)
        {
            var halfLength = animalProfile.torsoLength * 0.5f;
            var frontZ = Mathf.Lerp(halfLength * 0.45f, halfLength * 0.75f, animalProfile.shoulderBias);
            var rearZ = -halfLength * 0.65f;
            var z = isFront ? frontZ : rearZ;

            var trackWidth = isFront ? animalProfile.frontTrackWidth : animalProfile.rearTrackWidth;
            var x = (isLeft ? -1f : 1f) * trackWidth * 0.5f;
            var y = -animalProfile.contactDepth;

            return transform.TransformPoint(new Vector3(x, y, z));
        }

        private void ApplyIdleSettling()
        {
            if (body.linearVelocity.y > 0.1f)
            {
                return;
            }

            body.AddForce(Vector3.down * gaitProfile.settleForce, ForceMode.Acceleration);
        }
    }
}
