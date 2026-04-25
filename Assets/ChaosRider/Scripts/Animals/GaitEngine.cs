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
            public float manualAuditionSpeed;
            public float manualCadenceBlend;
            public float manualRhythmStrength;
            public float manualSurgeBlend;
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
        [SerializeField] private bool useManualGaitSelection = true;
        [SerializeField] private GaitType selectedGait = GaitType.Idle;

        [Header("Debug")]
        [SerializeField] private bool drawContactPoints = true;

        private Rigidbody body;
        private float gaitPhase;
        private GaitType currentGait = GaitType.Idle;
        private float gaitSelectionVelocity;

        public float GaitPhase => gaitPhase;
        public GaitType CurrentGait => currentGait;
        public GaitType SelectedGait => selectedGait;
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

        public void SetSelectedGait(GaitType gaitType)
        {
            selectedGait = gaitType;
            currentGait = gaitType;
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

            var driveIntent = Mathf.Clamp(throttleInput, -1f, 1f);
            var speedIntent = Mathf.Clamp01(Mathf.Abs(driveIntent));
            var planarSpeed = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up).magnitude;
            gaitSelectionVelocity = Mathf.MoveTowards(
                gaitSelectionVelocity,
                planarSpeed,
                Mathf.Lerp(6f, 14f, speedIntent) * Time.fixedDeltaTime);

            currentGait = useManualGaitSelection
                ? selectedGait
                : SelectGait(speedIntent, planarSpeed, gaitSelectionVelocity);
            var runtimeGait = currentGait == GaitType.Idle ? default : BuildRuntimeGait(currentGait);
            var manualMotionIntent = useManualGaitSelection && currentGait != GaitType.Idle
                ? Mathf.Clamp01(runtimeGait.manualAuditionSpeed / Mathf.Max(0.01f, gaitProfile.canterToGallopSpeed))
                : speedIntent;
            var cadenceBlend = useManualGaitSelection && currentGait != GaitType.Idle
                ? runtimeGait.manualCadenceBlend
                : speedIntent;
            var rhythmStrength = useManualGaitSelection && currentGait != GaitType.Idle
                ? runtimeGait.manualRhythmStrength
                : speedIntent;
            var surgeBlend = useManualGaitSelection && currentGait != GaitType.Idle
                ? runtimeGait.manualSurgeBlend + Mathf.Max(0f, driveIntent) * 0.08f
                : speedIntent;
            var locomotionDriveIntent = useManualGaitSelection && currentGait != GaitType.Idle
                ? CalculateManualLocomotionDrive(runtimeGait, driveIntent)
                : driveIntent;
            var steeringMotionIntent = Mathf.Max(speedIntent, manualMotionIntent);
            ApplyBodyTension(isGrounded, Mathf.Max(speedIntent, manualMotionIntent), driveIntent);

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

            var cycleDuration = Mathf.Lerp(runtimeGait.cycleDurationAtLowSpeed, runtimeGait.cycleDurationAtHighSpeed, cadenceBlend);
            gaitPhase = Mathf.Repeat(gaitPhase + Time.fixedDeltaTime / Mathf.Max(0.05f, cycleDuration), 1f);
            ApplyManualAuditionDrive(runtimeGait, driveIntent);

            var frontLoad = 0f;
            var rearLoad = 0f;
            var leftLoad = 0f;
            var rightLoad = 0f;

            if (runtimeGait.gaitType == GaitType.DogCanter)
            {
                ApplyCanterContacts(runtimeGait, locomotionDriveIntent, steeringInput, steeringMotionIntent, ref frontLoad, ref rearLoad, ref leftLoad, ref rightLoad);
            }
            else if (runtimeGait.gaitType == GaitType.DogGallop)
            {
                ApplyGallopContacts(runtimeGait, locomotionDriveIntent, steeringInput, steeringMotionIntent, ref frontLoad, ref rearLoad, ref leftLoad, ref rightLoad);
            }
            else
            {
                ApplyLeg(runtimeGait, VirtualLeg.FrontLeft, runtimeGait.frontLeftPhaseOffset, true, true, locomotionDriveIntent, steeringInput, steeringMotionIntent, ref frontLoad, ref leftLoad);
                ApplyLeg(runtimeGait, VirtualLeg.FrontRight, runtimeGait.frontRightPhaseOffset, true, false, locomotionDriveIntent, steeringInput, steeringMotionIntent, ref frontLoad, ref rightLoad);
                ApplyLeg(runtimeGait, VirtualLeg.RearLeft, runtimeGait.rearLeftPhaseOffset, false, true, locomotionDriveIntent, steeringInput, steeringMotionIntent, ref rearLoad, ref leftLoad);
                ApplyLeg(runtimeGait, VirtualLeg.RearRight, runtimeGait.rearRightPhaseOffset, false, false, locomotionDriveIntent, steeringInput, steeringMotionIntent, ref rearLoad, ref rightLoad);
            }

            var rollImbalance = rightLoad - leftLoad;
            var pitchImbalance = frontLoad - rearLoad;

            body.AddTorque(transform.forward * (-rollImbalance * runtimeGait.rollTorque), ForceMode.Acceleration);
            body.AddTorque(transform.right * (pitchImbalance * runtimeGait.pitchTorque), ForceMode.Acceleration);
            ApplyTorsoCadence(runtimeGait, rhythmStrength, surgeBlend);
            UpdateRideSignals(runtimeGait, rhythmStrength);
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

        private void ApplyCanterContacts(RuntimeGait runtimeGait, float driveIntent, float steeringInput, float speedIntent, ref float frontLoad, ref float rearLoad, ref float leftLoad, ref float rightLoad)
        {
            var phase = Mathf.Repeat(gaitPhase, 1f);
            var outsideHind = PhasePulse(phase, 0.1f, 0.2f);
            var diagonalCarry = PhasePulse(phase, 0.4f, 0.22f);
            var leadFore = PhasePulse(phase, 0.7f, 0.2f);

            // Left lead canter: right hind, left hind + right fore, left fore, then suspension.
            ApplyPulseContact(runtimeGait, false, false, outsideHind, 0.78f, 0.65f, driveIntent, steeringInput, speedIntent, ref rearLoad, ref rightLoad);
            ApplyPulseContact(runtimeGait, false, true, diagonalCarry, 0.7f, 0.48f, driveIntent, steeringInput, speedIntent, ref rearLoad, ref leftLoad);
            ApplyPulseContact(runtimeGait, true, false, diagonalCarry, 0.86f, 0.12f, driveIntent, steeringInput, speedIntent, ref frontLoad, ref rightLoad);
            ApplyPulseContact(runtimeGait, true, true, leadFore, 0.94f, 0.02f, driveIntent, steeringInput, speedIntent, ref frontLoad, ref leftLoad);

            var leadCatchPoint = GetContactPoint(true, true);
            body.AddForceAtPosition(-transform.forward * runtimeGait.driveForce * leadFore * 0.08f, leadCatchPoint, ForceMode.Acceleration);
        }

        private void ApplyGallopContacts(RuntimeGait runtimeGait, float driveIntent, float steeringInput, float speedIntent, ref float frontLoad, ref float rearLoad, ref float leftLoad, ref float rightLoad)
        {
            var phase = Mathf.Repeat(gaitPhase, 1f);
            var outsideHind = PhasePulse(phase, 0.08f, 0.16f);
            var diagonalCarry = PhasePulse(phase, 0.38f, 0.18f);
            var leadFore = PhasePulse(phase, 0.68f, 0.16f);

            // Playtest calibration: the previous canter read correctly as our current gallop.
            ApplyPulseContact(runtimeGait, false, false, outsideHind, 1.0f, 1.15f, driveIntent, steeringInput, speedIntent, ref rearLoad, ref rightLoad);
            ApplyPulseContact(runtimeGait, false, true, diagonalCarry, 0.78f, 0.85f, driveIntent, steeringInput, speedIntent, ref rearLoad, ref leftLoad);
            ApplyPulseContact(runtimeGait, true, false, diagonalCarry, 0.95f, 0.22f, driveIntent, steeringInput, speedIntent, ref frontLoad, ref rightLoad);
            ApplyPulseContact(runtimeGait, true, true, leadFore, 1.1f, 0.05f, driveIntent, steeringInput, speedIntent, ref frontLoad, ref leftLoad);

            var leadCatchPoint = GetContactPoint(true, true);
            body.AddForceAtPosition(-transform.forward * runtimeGait.driveForce * leadFore * 0.18f, leadCatchPoint, ForceMode.Acceleration);
        }

        private void ApplyPulseContact(RuntimeGait runtimeGait, bool isFront, bool isLeft, float pulse, float supportScale, float driveScale, float driveIntent, float steeringInput, float speedIntent, ref float foreAftLoad, ref float lateralLoad)
        {
            if (pulse <= 0f)
            {
                return;
            }

            var contactPoint = GetContactPoint(isFront, isLeft);
            if (drawContactPoints)
            {
                Debug.DrawRay(contactPoint, Vector3.up * (0.25f + pulse * 0.35f), Color.cyan);
            }

            var supportBias = isFront ? runtimeGait.frontSupportBias : runtimeGait.rearSupportBias;
            body.AddForceAtPosition(Vector3.up * runtimeGait.supportForce * pulse * supportScale * supportBias, contactPoint, ForceMode.Acceleration);

            var forwardForce = driveIntent >= 0f
                ? runtimeGait.driveForce * driveIntent * pulse * driveScale
                : gaitProfile.brakingForce * driveIntent * pulse * supportScale;
            body.AddForceAtPosition(transform.forward * forwardForce, contactPoint, ForceMode.Acceleration);

            var steerScale = Mathf.Lerp(gaitProfile.lowSpeedSteerScale, 1f, speedIntent);
            var steerStrength = steeringInput * pulse * steerScale;
            if (isFront)
            {
                body.AddForceAtPosition(transform.right * runtimeGait.frontSteerForce * steerStrength, contactPoint, ForceMode.Acceleration);
            }
            else
            {
                body.AddForceAtPosition(-transform.right * runtimeGait.rearCounterSteerForce * steerStrength, contactPoint, ForceMode.Acceleration);
            }

            foreAftLoad += pulse * supportScale * supportBias;
            lateralLoad += isLeft ? -pulse * supportScale : pulse * supportScale;
        }

        private GaitType SelectGait(float speedIntent, float planarSpeed, float selectionSpeed)
        {
            if (speedIntent < gaitProfile.idleThreshold && planarSpeed <= gaitProfile.idlePivotPlanarSpeedThreshold)
            {
                return GaitType.Idle;
            }

            if (currentGait == GaitType.DogGallop && selectionSpeed > gaitProfile.canterToGallopSpeed * 0.8f)
            {
                return GaitType.DogGallop;
            }

            if (currentGait == GaitType.DogCanter && selectionSpeed > gaitProfile.trotToCanterSpeed * 0.8f)
            {
                return selectionSpeed >= gaitProfile.canterToGallopSpeed ? GaitType.DogGallop : GaitType.DogCanter;
            }

            if (currentGait == GaitType.DogTrot && selectionSpeed > gaitProfile.walkToTrotSpeed * 0.8f)
            {
                if (selectionSpeed >= gaitProfile.canterToGallopSpeed)
                {
                    return GaitType.DogGallop;
                }

                return selectionSpeed >= gaitProfile.trotToCanterSpeed ? GaitType.DogCanter : GaitType.DogTrot;
            }

            if (selectionSpeed < gaitProfile.walkToTrotSpeed)
            {
                return GaitType.DogWalk;
            }

            if (selectionSpeed < gaitProfile.trotToCanterSpeed)
            {
                return GaitType.DogTrot;
            }

            if (selectionSpeed < gaitProfile.canterToGallopSpeed)
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
                manualAuditionSpeed = 1.8f,
                manualCadenceBlend = 0.45f,
                manualRhythmStrength = 0.9f,
                manualSurgeBlend = 0.16f,
            };

            return gaitType switch
            {
                GaitType.DogWalk => ScaleGait(dogTrot, GaitType.DogWalk, 1.35f, 1.55f, 0.74f, 0.25f, 0.75f, 0f, 0.5f, 0.55f, 1.25f, 0.7f, 0.45f, 0.55f, 0.2f),
                GaitType.DogCanter => ScaleGait(dogTrot, GaitType.DogCanter, 1.22f, 1.02f, 0.6f, 0.68f, 0.36f, 0.36f, 0f, 0.82f, 0.9f, 0.24f, 0.22f, 0.28f, 0.16f),
                GaitType.DogGallop => ScaleGait(dogTrot, GaitType.DogGallop, 1.05f, 0.88f, 0.48f, 0.68f, 0.36f, 0.36f, 0f, 1.05f, 1.35f, 0.55f, 0.5f, 0.75f, 0.5f),
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
            source.cadenceRollTorque *= rollScale;
            source.cadencePitchTorque *= pitchScale;
            source.cadenceSurgeForce *= surgeScale;
            source.rideRoll *= rideRollScale;
            source.rideForeAft *= surgeScale;
            source.ridePitch *= pitchScale;
            ApplyManualAuditionDefaults(ref source);
            return source;
        }

        private static void ApplyManualAuditionDefaults(ref RuntimeGait runtimeGait)
        {
            switch (runtimeGait.gaitType)
            {
                case GaitType.DogWalk:
                    runtimeGait.manualAuditionSpeed = 0.9f;
                    runtimeGait.manualCadenceBlend = 0.22f;
                    runtimeGait.manualRhythmStrength = 0.75f;
                    runtimeGait.manualSurgeBlend = 0.1f;
                    break;
                case GaitType.DogCanter:
                    runtimeGait.manualAuditionSpeed = 3.0f;
                    runtimeGait.manualCadenceBlend = 0.58f;
                    runtimeGait.manualRhythmStrength = 0.45f;
                    runtimeGait.manualSurgeBlend = 0.12f;
                    break;
                case GaitType.DogGallop:
                    runtimeGait.manualAuditionSpeed = 4.9f;
                    runtimeGait.manualCadenceBlend = 0.7f;
                    runtimeGait.manualRhythmStrength = 0.95f;
                    runtimeGait.manualSurgeBlend = 0.28f;
                    break;
            }
        }

        private void ApplyTorsoCadence(RuntimeGait runtimeGait, float rhythmStrength, float surgeBlend)
        {
            if (runtimeGait.gaitType == GaitType.DogCanter)
            {
                ApplyCanterCadence(runtimeGait, rhythmStrength, surgeBlend);
                return;
            }

            if (runtimeGait.gaitType == GaitType.DogGallop)
            {
                ApplyGallopCadence(runtimeGait, rhythmStrength, surgeBlend);
                return;
            }

            var cadence = gaitPhase * Mathf.PI * 2f;
            var diagonalBias = Mathf.Sin(cadence);
            var foreAftBias = Mathf.Sin(cadence - Mathf.PI * 0.25f);
            var gaitScale = rhythmStrength;

            body.AddTorque(transform.forward * (-diagonalBias * runtimeGait.cadenceRollTorque * gaitScale), ForceMode.Acceleration);
            body.AddTorque(transform.right * (foreAftBias * runtimeGait.cadencePitchTorque * gaitScale), ForceMode.Acceleration);
            body.AddForce(transform.forward * (Mathf.Max(0f, foreAftBias) * runtimeGait.cadenceSurgeForce * surgeBlend), ForceMode.Acceleration);
        }

        private void UpdateRideSignals(RuntimeGait runtimeGait, float speedIntent)
        {
            if (runtimeGait.gaitType == GaitType.DogCanter)
            {
                UpdateCanterRideSignals(runtimeGait, speedIntent);
                return;
            }

            if (runtimeGait.gaitType == GaitType.DogGallop)
            {
                UpdateGallopRideSignals(runtimeGait, speedIntent);
                return;
            }

            var cadence = gaitPhase * Mathf.PI * 2f;
            RideCouplingStrength = speedIntent;
            RideVerticalSignal = (Mathf.Abs(Mathf.Sin(cadence)) - 0.5f) * runtimeGait.rideVertical;
            RideForeAftSignal = Mathf.Sin(cadence - Mathf.PI * 0.25f) * runtimeGait.rideForeAft;
            RidePitchSignal = Mathf.Sin(cadence - Mathf.PI * 0.25f) * runtimeGait.ridePitch;
            RideRollSignal = -Mathf.Sin(cadence) * runtimeGait.rideRoll;
        }

        private void ApplyCanterCadence(RuntimeGait runtimeGait, float rhythmStrength, float surgeBlend)
        {
            var phase = Mathf.Repeat(gaitPhase, 1f);
            var rearBeat = PhasePulse(phase, 0.08f, 0.24f);
            var diagonalBeat = PhasePulse(phase, 0.4f, 0.24f);
            var leadForeBeat = PhasePulse(phase, 0.72f, 0.22f);

            var roll = (diagonalBeat * 0.22f - leadForeBeat * 0.12f) * runtimeGait.cadenceRollTorque * rhythmStrength;
            var pitch = (rearBeat * 0.08f + diagonalBeat * 0.04f - leadForeBeat * 0.1f) * runtimeGait.cadencePitchTorque * rhythmStrength;
            var surge = (0.36f + rearBeat * 0.05f + diagonalBeat * 0.05f + leadForeBeat * 0.02f) * runtimeGait.cadenceSurgeForce * surgeBlend;

            body.AddTorque(transform.forward * -roll, ForceMode.Acceleration);
            body.AddTorque(transform.right * pitch, ForceMode.Acceleration);
            body.AddForce(transform.forward * surge, ForceMode.Acceleration);
        }

        private void UpdateCanterRideSignals(RuntimeGait runtimeGait, float speedIntent)
        {
            var phase = Mathf.Repeat(gaitPhase, 1f);
            var rearBeat = PhasePulse(phase, 0.08f, 0.24f);
            var diagonalBeat = PhasePulse(phase, 0.4f, 0.24f);
            var leadForeBeat = PhasePulse(phase, 0.72f, 0.22f);
            var lift = rearBeat * 0.18f + diagonalBeat * 0.28f + leadForeBeat * 0.22f;

            RideCouplingStrength = speedIntent;
            RideVerticalSignal = (lift - 0.16f) * runtimeGait.rideVertical * 0.18f;
            RideForeAftSignal = (rearBeat * 0.09f + diagonalBeat * 0.07f - leadForeBeat * 0.08f) * runtimeGait.rideForeAft;
            RidePitchSignal = (rearBeat * 0.07f + diagonalBeat * 0.03f - leadForeBeat * 0.08f) * runtimeGait.ridePitch;
            RideRollSignal = (leadForeBeat * 0.08f - diagonalBeat * 0.06f) * runtimeGait.rideRoll;
        }

        private void ApplyGallopCadence(RuntimeGait runtimeGait, float rhythmStrength, float surgeBlend)
        {
            var phase = Mathf.Repeat(gaitPhase, 1f);
            var rearBeat = PhasePulse(phase, 0.08f, 0.24f);
            var diagonalBeat = PhasePulse(phase, 0.4f, 0.24f);
            var leadForeBeat = PhasePulse(phase, 0.72f, 0.22f);

            var roll = (diagonalBeat * 0.45f - leadForeBeat * 0.25f) * runtimeGait.cadenceRollTorque * rhythmStrength;
            var pitch = (rearBeat * 0.18f + diagonalBeat * 0.08f - leadForeBeat * 0.22f) * runtimeGait.cadencePitchTorque * rhythmStrength;
            var surge = (0.45f + rearBeat * 0.12f + diagonalBeat * 0.1f + leadForeBeat * 0.05f) * runtimeGait.cadenceSurgeForce * surgeBlend;

            body.AddTorque(transform.forward * -roll, ForceMode.Acceleration);
            body.AddTorque(transform.right * pitch, ForceMode.Acceleration);
            body.AddForce(transform.forward * surge, ForceMode.Acceleration);
        }

        private void UpdateGallopRideSignals(RuntimeGait runtimeGait, float speedIntent)
        {
            var phase = Mathf.Repeat(gaitPhase, 1f);
            var rearBeat = PhasePulse(phase, 0.08f, 0.24f);
            var diagonalBeat = PhasePulse(phase, 0.4f, 0.24f);
            var leadForeBeat = PhasePulse(phase, 0.72f, 0.22f);
            var lift = rearBeat * 0.18f + diagonalBeat * 0.28f + leadForeBeat * 0.22f;

            RideCouplingStrength = speedIntent;
            RideVerticalSignal = (lift - 0.16f) * runtimeGait.rideVertical * 0.35f;
            RideForeAftSignal = (rearBeat * 0.18f + diagonalBeat * 0.12f - leadForeBeat * 0.16f) * runtimeGait.rideForeAft;
            RidePitchSignal = (rearBeat * 0.16f + diagonalBeat * 0.06f - leadForeBeat * 0.18f) * runtimeGait.ridePitch;
            RideRollSignal = (leadForeBeat * 0.18f - diagonalBeat * 0.12f) * runtimeGait.rideRoll;
        }

        private void ApplyManualAuditionDrive(RuntimeGait runtimeGait, float driveIntent)
        {
            if (!useManualGaitSelection || runtimeGait.manualAuditionSpeed <= 0f)
            {
                return;
            }

            var localVelocity = transform.InverseTransformDirection(body.linearVelocity);
            var targetSpeed = CalculateManualAuditionTargetSpeed(runtimeGait, driveIntent);
            var speedError = Mathf.Clamp(targetSpeed - localVelocity.z, -2.5f, 2.5f);

            body.AddRelativeForce(Vector3.forward * speedError * gaitProfile.manualAuditionDrive * 0.55f, ForceMode.Acceleration);
        }

        private float CalculateManualLocomotionDrive(RuntimeGait runtimeGait, float driveIntent)
        {
            var localVelocity = transform.InverseTransformDirection(body.linearVelocity);
            var targetSpeed = CalculateManualAuditionTargetSpeed(runtimeGait, driveIntent);
            if (targetSpeed <= 0.05f)
            {
                return Mathf.Clamp(driveIntent, -1f, 0f);
            }

            var gaitCruise = runtimeGait.gaitType == GaitType.DogGallop ? 0.62f : 0.42f;
            var speedCorrection = Mathf.Clamp((targetSpeed - localVelocity.z) * 0.18f, -0.45f, 0.45f);
            return Mathf.Clamp(gaitCruise + speedCorrection, -0.4f, 1f);
        }

        private static float CalculateManualAuditionTargetSpeed(RuntimeGait runtimeGait, float driveIntent)
        {
            var forwardBoost = Mathf.Max(0f, driveIntent) * runtimeGait.manualAuditionSpeed * 0.25f;
            var reinIn = Mathf.Max(0f, -driveIntent);
            return Mathf.Lerp(runtimeGait.manualAuditionSpeed + forwardBoost, 0f, reinIn);
        }

        private static float PhasePulse(float phase, float center, float halfWidth)
        {
            var distance = Mathf.Abs(Mathf.DeltaAngle(phase * 360f, center * 360f)) / 360f;
            if (distance >= halfWidth)
            {
                return 0f;
            }

            var normalized = distance / Mathf.Max(0.001f, halfWidth);
            return 0.5f + Mathf.Cos(normalized * Mathf.PI) * 0.5f;
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
            var longitudinalDampingScale = useManualGaitSelection
                ? 1.1f
                : Mathf.Lerp(1.4f, 0.55f, Mathf.Clamp01(Mathf.Abs(throttleInput)));
            var dampingForceLocal = new Vector3(
                -localVelocity.x * gaitProfile.lateralDamping,
                -Mathf.Min(0f, localVelocity.y) * gaitProfile.verticalDamping,
                -localVelocity.z * gaitProfile.longitudinalDamping * longitudinalDampingScale);

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
