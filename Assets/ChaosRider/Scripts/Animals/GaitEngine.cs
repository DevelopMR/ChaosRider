using UnityEngine;

namespace ChaosRider.Animals
{
    [RequireComponent(typeof(Rigidbody))]
    public class GaitEngine : MonoBehaviour
    {
        private struct RuntimeGait
        {
            public GaitType gaitType;
            public float cycleDurationAtLowSpeed;
            public float cycleDurationAtHighSpeed;
            public float stanceFraction;
            public float frontLeftPhaseOffset;
            public float frontRightPhaseOffset;
            public float rearLeftPhaseOffset;
            public float rearRightPhaseOffset;
            public float supportForce;
            public float driveForce;
            public float frontSteerForce;
            public float rearCounterSteerForce;
            public float frontSupportBias;
            public float rearSupportBias;
            public float rollTorque;
            public float pitchTorque;
            public float cadenceRollTorque;
            public float cadencePitchTorque;
            public float cadenceSurgeForce;
            public float rideVertical;
            public float rideForeAft;
            public float ridePitch;
            public float rideRoll;
        }

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
        private GaitType currentGait = GaitType.Idle;

        public float GaitPhase => gaitPhase;
        public GaitType CurrentGait => currentGait;
        public string CurrentGaitLabel => currentGait.ToString().Replace("Dog", string.Empty);
        public float RideVerticalSignal { get; private set; }
        public float RideForeAftSignal { get; private set; }
        public float RidePitchSignal { get; private set; }
        public float RideRollSignal { get; private set; }
        public float RideCouplingStrength { get; private set; }

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
            var planarSpeed = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up).magnitude;
            currentGait = SelectGait(speedIntent, planarSpeed);
            ApplyBodyTension(isGrounded, speedIntent, throttleInput);

            if (!isGrounded)
            {
                ClearRideSignals();
                ApplyIdleSettling();
                return;
            }

            if (currentGait == GaitType.Idle)
            {
                ClearRideSignals();
                ApplyIdleTurning(steeringInput);
                ApplyIdleSettling();
                return;
            }

            var runtimeGait = BuildRuntimeGait(currentGait);
            var cycleDuration = Mathf.Lerp(runtimeGait.cycleDurationAtLowSpeed, runtimeGait.cycleDurationAtHighSpeed, speedIntent);
            gaitPhase = Mathf.Repeat(gaitPhase + Time.fixedDeltaTime / Mathf.Max(0.05f, cycleDuration), 1f);

            var frontLoad = 0f;
            var rearLoad = 0f;
            var leftLoad = 0f;
            var rightLoad = 0f;

            ApplyLeg(runtimeGait, VirtualLeg.FrontLeft, runtimeGait.frontLeftPhaseOffset, true, true, throttleInput, steeringInput, speedIntent, ref frontLoad, ref leftLoad);
            ApplyLeg(runtimeGait, VirtualLeg.FrontRight, runtimeGait.frontRightPhaseOffset, true, false, throttleInput, steeringInput, speedIntent, ref frontLoad, ref rightLoad);
            ApplyLeg(runtimeGait, VirtualLeg.RearLeft, runtimeGait.rearLeftPhaseOffset, false, true, throttleInput, steeringInput, speedIntent, ref rearLoad, ref leftLoad);
            ApplyLeg(runtimeGait, VirtualLeg.RearRight, runtimeGait.rearRightPhaseOffset, false, false, throttleInput, steeringInput, speedIntent, ref rearLoad, ref rightLoad);

            var rollImbalance = rightLoad - leftLoad;
            var pitchImbalance = frontLoad - rearLoad;

            body.AddTorque(transform.forward * (-rollImbalance * runtimeGait.rollTorque), ForceMode.Acceleration);
            body.AddTorque(transform.right * (pitchImbalance * runtimeGait.pitchTorque), ForceMode.Acceleration);
            ApplyTorsoCadence(runtimeGait, speedIntent, throttleInput);
            UpdateRideSignals(runtimeGait, speedIntent);
        }

        private void ApplyLeg(RuntimeGait runtimeGait, VirtualLeg leg, float phaseOffset, bool isFront, bool isLeft, float throttleInput, float steeringInput, float speedIntent, ref float foreAftLoad, ref float lateralLoad)
        {
            var legPhase = Mathf.Repeat(gaitPhase + phaseOffset, 1f);
            var inStance = legPhase < runtimeGait.stanceFraction;
            var contactPoint = GetContactPoint(isFront, isLeft);

            if (drawContactPoints)
            {
                Debug.DrawRay(contactPoint, Vector3.up * 0.4f, inStance ? Color.green : Color.gray);
            }

            if (!inStance)
            {
                return;
            }

            var stanceT = legPhase / runtimeGait.stanceFraction;
            var loadPulse = EvaluateLoadPulse(stanceT);
            var supportBias = isFront ? runtimeGait.frontSupportBias : runtimeGait.rearSupportBias;
            var driveBias = isFront ? Mathf.Lerp(0.35f, 0.45f, 1f - animalProfile.hindDriveBias) : animalProfile.hindDriveBias;

            var supportForce = Vector3.up * runtimeGait.supportForce * loadPulse * supportBias;
            body.AddForceAtPosition(supportForce, contactPoint, ForceMode.Acceleration);

            var driveForceMagnitude = throttleInput >= 0f
                ? runtimeGait.driveForce * throttleInput * loadPulse * driveBias
                : gaitProfile.brakingForce * throttleInput * loadPulse * supportBias;
            body.AddForceAtPosition(transform.forward * driveForceMagnitude, contactPoint, ForceMode.Acceleration);

            var steerScale = Mathf.Lerp(gaitProfile.lowSpeedSteerScale, 1f, speedIntent);
            var steerStrength = steeringInput * loadPulse * steerScale;
            if (isFront)
            {
                body.AddForceAtPosition(transform.right * runtimeGait.frontSteerForce * steerStrength, contactPoint, ForceMode.Acceleration);
            }
            else
            {
                body.AddForceAtPosition(-transform.right * runtimeGait.rearCounterSteerForce * steerStrength, contactPoint, ForceMode.Acceleration);
            }

            foreAftLoad += loadPulse * supportBias;
            lateralLoad += isLeft ? -loadPulse : loadPulse;
        }

        private Vector3 GetContactPoint(bool isFront, bool isLeft)
        {
            var halfLength = animalProfile.torsoLength * 0.5f;
            var frontZ = Mathf.Lerp(halfLength * 0.4f, halfLength * 0.62f, animalProfile.shoulderBias);
            var rearZ = -halfLength * 0.48f;
            var z = isFront ? frontZ : rearZ;

            var trackWidth = isFront ? animalProfile.frontTrackWidth : animalProfile.rearTrackWidth;
            var x = (isLeft ? -1f : 1f) * trackWidth * 0.5f;
            var y = -animalProfile.contactDepth;

            return transform.TransformPoint(new Vector3(x, y, z));
        }

        private float EvaluateLoadPulse(float stanceT)
        {
            // A softer pulse reads more like weight transfer and less like a trampoline pop.
            var arch = Mathf.Sin(stanceT * Mathf.PI);
            return Mathf.SmoothStep(0.25f, 0.92f, arch);
        }

        private GaitType SelectGait(float speedIntent, float planarSpeed)
        {
            if (speedIntent < gaitProfile.idleThreshold && planarSpeed <= gaitProfile.idlePivotPlanarSpeedThreshold)
            {
                return GaitType.Idle;
            }

            if (planarSpeed < gaitProfile.walkToTrotSpeed)
            {
                return GaitType.DogWalk;
            }

            if (planarSpeed < gaitProfile.trotToCanterSpeed)
            {
                return GaitType.DogTrot;
            }

            if (planarSpeed < gaitProfile.canterToGallopSpeed)
            {
                return GaitType.DogCanter;
            }

            return GaitType.DogGallop;
        }

        private RuntimeGait BuildRuntimeGait(GaitType gaitType)
        {
            var dogTrot = new RuntimeGait
            {
                gaitType = GaitType.DogTrot,
                cycleDurationAtLowSpeed = gaitProfile.cycleDurationAtLowSpeed,
                cycleDurationAtHighSpeed = gaitProfile.cycleDurationAtHighSpeed,
                stanceFraction = gaitProfile.stanceFraction,
                frontLeftPhaseOffset = gaitProfile.frontLeftPhaseOffset,
                frontRightPhaseOffset = gaitProfile.frontRightPhaseOffset,
                rearLeftPhaseOffset = gaitProfile.rearLeftPhaseOffset,
                rearRightPhaseOffset = gaitProfile.rearRightPhaseOffset,
                supportForce = gaitProfile.supportForce,
                driveForce = gaitProfile.driveForce,
                frontSteerForce = gaitProfile.frontSteerForce,
                rearCounterSteerForce = gaitProfile.rearCounterSteerForce,
                frontSupportBias = gaitProfile.frontSupportBias,
                rearSupportBias = gaitProfile.rearSupportBias,
                rollTorque = gaitProfile.rollTorque,
                pitchTorque = gaitProfile.pitchTorque,
                cadenceRollTorque = gaitProfile.cadenceRollTorque,
                cadencePitchTorque = gaitProfile.cadencePitchTorque,
                cadenceSurgeForce = gaitProfile.cadenceSurgeForce,
                rideVertical = 1f,
                rideForeAft = 0.45f,
                ridePitch = 0.75f,
                rideRoll = 0.85f,
            };

            return gaitType switch
            {
                GaitType.DogWalk => ScaleGait(dogTrot, GaitType.DogWalk, 1.35f, 1.55f, 0.74f, 0.25f, 0.75f, 0f, 0.5f, 0.55f, 0.75f, 0.7f, 0.45f, 0.55f, 0.2f),
                GaitType.DogCanter => ScaleGait(dogTrot, GaitType.DogCanter, 1.1f, 0.9f, 0.44f, 0.55f, 0.3f, 0.3f, 0f, 1.25f, 1.55f, 1.25f, 1.45f, 1.8f, 0.95f),
                GaitType.DogGallop => ScaleGait(dogTrot, GaitType.DogGallop, 0.78f, 0.65f, 0.34f, 0.5f, 0.65f, 0.18f, 0f, 1.45f, 2.15f, 1.05f, 1.85f, 2.6f, 0.75f),
                _ => dogTrot,
            };
        }

        private static RuntimeGait ScaleGait(RuntimeGait source, GaitType gaitType, float lowCycleScale, float highCycleScale, float stanceFraction, float frontLeft, float frontRight, float rearLeft, float rearRight, float supportScale, float driveScale, float rollScale, float pitchScale, float surgeScale, float rideRollScale)
        {
            source.gaitType = gaitType;
            source.cycleDurationAtLowSpeed *= lowCycleScale;
            source.cycleDurationAtHighSpeed *= highCycleScale;
            source.stanceFraction = stanceFraction;
            source.frontLeftPhaseOffset = frontLeft;
            source.frontRightPhaseOffset = frontRight;
            source.rearLeftPhaseOffset = rearLeft;
            source.rearRightPhaseOffset = rearRight;
            source.supportForce *= supportScale;
            source.driveForce *= driveScale;
            source.rollTorque *= rollScale;
            source.pitchTorque *= pitchScale;
            source.cadenceSurgeForce *= surgeScale;
            source.rideRoll *= rideRollScale;
            source.rideForeAft *= surgeScale;
            source.ridePitch *= pitchScale;
            return source;
        }

        private void ApplyTorsoCadence(RuntimeGait runtimeGait, float speedIntent, float throttleInput)
        {
            var cadence = gaitPhase * Mathf.PI * 2f;
            var diagonalBias = Mathf.Sin(cadence);
            var foreAftBias = Mathf.Sin(cadence - Mathf.PI * 0.25f);
            var gaitScale = speedIntent * Mathf.Clamp01(Mathf.Abs(throttleInput));

            body.AddTorque(transform.forward * (-diagonalBias * runtimeGait.cadenceRollTorque * gaitScale), ForceMode.Acceleration);
            body.AddTorque(transform.right * (foreAftBias * runtimeGait.cadencePitchTorque * gaitScale), ForceMode.Acceleration);
            body.AddForce(transform.forward * (Mathf.Max(0f, foreAftBias) * runtimeGait.cadenceSurgeForce * gaitScale), ForceMode.Acceleration);
        }

        private void UpdateRideSignals(RuntimeGait runtimeGait, float speedIntent)
        {
            var cadence = gaitPhase * Mathf.PI * 2f;
            RideCouplingStrength = speedIntent;
            RideVerticalSignal = (Mathf.Abs(Mathf.Sin(cadence)) - 0.5f) * runtimeGait.rideVertical;
            RideForeAftSignal = Mathf.Sin(cadence - Mathf.PI * 0.25f) * runtimeGait.rideForeAft;
            RidePitchSignal = Mathf.Sin(cadence - Mathf.PI * 0.25f) * runtimeGait.ridePitch;
            RideRollSignal = -Mathf.Sin(cadence) * runtimeGait.rideRoll;
        }

        private void ClearRideSignals()
        {
            RideCouplingStrength = 0f;
            RideVerticalSignal = 0f;
            RideForeAftSignal = 0f;
            RidePitchSignal = 0f;
            RideRollSignal = 0f;
        }

        private void ApplyBodyTension(bool isGrounded, float speedIntent, float throttleInput)
        {
            var localUp = transform.InverseTransformDirection(Vector3.up);
            var uprightError = new Vector3(localUp.z, 0f, -localUp.x);
            var localAngularVelocity = transform.InverseTransformDirection(body.angularVelocity);

            var springScale = isGrounded ? 1f : 0.35f;
            var dampingScale = Mathf.Lerp(1.15f, 0.8f, speedIntent);

            var correctiveTorqueLocal = new Vector3(
                uprightError.x * gaitProfile.uprightSpring - localAngularVelocity.x * gaitProfile.uprightDamping * dampingScale,
                0f,
                uprightError.z * gaitProfile.uprightSpring - localAngularVelocity.z * gaitProfile.uprightDamping * dampingScale);

            body.AddRelativeTorque(correctiveTorqueLocal * springScale, ForceMode.Acceleration);

            var localVelocity = transform.InverseTransformDirection(body.linearVelocity);
            var dampingForceLocal = new Vector3(
                -localVelocity.x * gaitProfile.lateralDamping,
                -Mathf.Min(0f, localVelocity.y) * gaitProfile.verticalDamping,
                -localVelocity.z * gaitProfile.longitudinalDamping * Mathf.Lerp(1.4f, 0.55f, Mathf.Clamp01(Mathf.Abs(throttleInput))));

            body.AddRelativeForce(dampingForceLocal * springScale, ForceMode.Acceleration);
        }

        private void ApplyIdleTurning(float steeringInput)
        {
            if (Mathf.Abs(steeringInput) < 0.01f)
            {
                return;
            }

            var planarVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
            if (planarVelocity.magnitude <= gaitProfile.idlePivotPlanarSpeedThreshold)
            {
                var yawStep = steeringInput * gaitProfile.idlePivotDegreesPerSecond * Time.fixedDeltaTime;
                var pivotRotation = Quaternion.AngleAxis(yawStep, Vector3.up) * body.rotation;
                body.MoveRotation(pivotRotation);

                var localAngularVelocity = transform.InverseTransformDirection(body.angularVelocity);
                var correctedAngularVelocity = new Vector3(
                    localAngularVelocity.x * 0.2f,
                    localAngularVelocity.y,
                    localAngularVelocity.z * 0.2f);
                body.angularVelocity = transform.TransformDirection(correctedAngularVelocity);
                body.linearVelocity = new Vector3(body.linearVelocity.x * 0.96f, body.linearVelocity.y, body.linearVelocity.z * 0.96f);
            }

            var yawDamping = -body.angularVelocity.y * 0.35f;
            body.AddTorque(Vector3.up * (steeringInput * gaitProfile.idleTurnTorque + yawDamping), ForceMode.Acceleration);
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
