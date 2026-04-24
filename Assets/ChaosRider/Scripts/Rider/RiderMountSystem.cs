using ChaosRider.Animals;
using ChaosRider.Cameras;
using ChaosRider.Game;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChaosRider.Rider
{
    public class RiderMountSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AnimalPhysicsController animalController;
        [SerializeField] private Rigidbody animalBody;
        [SerializeField] private GaitEngine gaitEngine;
        [SerializeField] private Transform seatAnchor;
        [SerializeField] private RiderRagdollSystem ragdollSystem;
        [SerializeField] private CameraModeController cameraController;

        [Header("Mounted Rider")]
        [SerializeField] private Transform mountedRiderRoot;
        [SerializeField] private float riderPositionSharpness = 16f;
        [SerializeField] private float riderRotationSharpness = 10f;
        [SerializeField] private float riderLean = 12f;
        [SerializeField] private float riderRoll = 16f;
        [SerializeField] private float gaitRideHeight = 0.045f;
        [SerializeField] private float gaitForeAftShift = 0.03f;
        [SerializeField] private float gaitPitchCoupling = 5f;
        [SerializeField] private float gaitRollCoupling = 4f;

        [Header("Stability")]
        [SerializeField] private float maxStability = 100f;
        [SerializeField] private float recoveryPerSecond = 7f;
        [SerializeField] private float holdOnRecoveryPerSecond = 20f;
        [SerializeField] private float balanceDrain = 1.4f;
        [SerializeField] private float surgeDrain = 0.22f;
        [SerializeField] private float surgeGraceAcceleration = 5f;
        [SerializeField] private float slipDrain = 0.8f;
        [SerializeField] private float impactDrainMultiplier = 0.0035f;
        [SerializeField] private float instantEjectImpactForce = 9000f;
        [SerializeField] private float lowStabilityEjectDelay = 0.45f;
        [SerializeField] private float ejectionImpulseMultiplier = 0.22f;
        [SerializeField] private float upwardEjectionImpulse = 7f;
        [SerializeField] private Key holdOnKey = Key.Space;
        [SerializeField] private Key debugEjectKey = Key.E;

        [Header("Debug")]
        [SerializeField] private bool debugMountedState = true;

        public float StabilityNormalized => Mathf.Clamp01(stability / Mathf.Max(1f, maxStability));
        public bool IsMounted { get; private set; } = true;

        private float stability;
        private float previousForwardSpeed;
        private float lowStabilityTimer;

        public void Configure(AnimalPhysicsController controller, Rigidbody body, Transform mountAnchor, RiderRagdollSystem riderRagdollSystem, Transform riderVisualRoot, CameraModeController riderCameraController)
        {
            if (animalController != null)
            {
                animalController.Impacted -= HandleImpact;
            }

            animalController = controller;
            animalBody = body;
            gaitEngine = controller != null ? controller.GetComponent<GaitEngine>() : null;
            seatAnchor = mountAnchor;
            ragdollSystem = riderRagdollSystem;
            mountedRiderRoot = riderVisualRoot;
            cameraController = riderCameraController;

            if (animalController != null)
            {
                animalController.Impacted += HandleImpact;
            }

            previousForwardSpeed = animalController != null ? animalController.ForwardSpeed : 0f;
            lowStabilityTimer = 0f;
        }

        private void Awake()
        {
            stability = maxStability;
            previousForwardSpeed = animalController != null ? animalController.ForwardSpeed : 0f;
        }

        private void OnDestroy()
        {
            if (animalController != null)
            {
                animalController.Impacted -= HandleImpact;
            }
        }

        private void Update()
        {
            if (!IsMounted || animalController == null || animalBody == null)
            {
                return;
            }

            UpdateStability();

            if (Keyboard.current != null && Keyboard.current[debugEjectKey].wasPressedThisFrame)
            {
                Eject("Debug eject");
            }
        }

        private void LateUpdate()
        {
            if (!IsMounted || seatAnchor == null || mountedRiderRoot == null || animalBody == null)
            {
                return;
            }

            var desiredPosition = seatAnchor.position;
            if (gaitEngine != null && gaitEngine.CurrentGait == GaitType.DogTrot)
            {
                var gaitCycle = gaitEngine.GaitPhase * Mathf.PI * 2f;
                var beatLift = Mathf.Abs(Mathf.Sin(gaitCycle));
                var foreAftShift = Mathf.Sin(gaitCycle - Mathf.PI * 0.25f);
                desiredPosition += seatAnchor.up * ((beatLift - 0.5f) * gaitRideHeight);
                desiredPosition += seatAnchor.forward * (foreAftShift * gaitForeAftShift);
            }

            mountedRiderRoot.position = Vector3.Lerp(mountedRiderRoot.position, desiredPosition, 1f - Mathf.Exp(-riderPositionSharpness * Time.deltaTime));

            var localAngularVelocity = transform.InverseTransformDirection(animalBody.angularVelocity);
            var leanPitch = -localAngularVelocity.x * riderLean;
            var leanRoll = -localAngularVelocity.z * riderRoll;
            if (gaitEngine != null && gaitEngine.CurrentGait == GaitType.DogTrot)
            {
                var gaitCycle = gaitEngine.GaitPhase * Mathf.PI * 2f;
                leanPitch += Mathf.Sin(gaitCycle - Mathf.PI * 0.25f) * gaitPitchCoupling;
                leanRoll += -Mathf.Sin(gaitCycle) * gaitRollCoupling;
            }

            var desiredRotation = seatAnchor.rotation * Quaternion.Euler(leanPitch, 0f, leanRoll);
            mountedRiderRoot.rotation = Quaternion.Slerp(mountedRiderRoot.rotation, desiredRotation, 1f - Mathf.Exp(-riderRotationSharpness * Time.deltaTime));
        }

        private void UpdateStability()
        {
            var holdOn = Keyboard.current != null && Keyboard.current[holdOnKey].isPressed;
            var localAngularVelocity = transform.InverseTransformDirection(animalBody.angularVelocity);
            var rollPitchStress = new Vector2(localAngularVelocity.x, localAngularVelocity.z).magnitude;
            var forwardSpeed = animalController.ForwardSpeed;
            var signedForwardAcceleration = (forwardSpeed - previousForwardSpeed) / Mathf.Max(Time.deltaTime, 0.0001f);
            var decelerationSpike = Mathf.Max(0f, -signedForwardAcceleration - surgeGraceAcceleration);
            var lateralSlip = Mathf.Abs(Vector3.Dot(animalBody.linearVelocity, transform.right));

            var balanceStress = rollPitchStress * balanceDrain * Time.deltaTime;
            var surgeStress = decelerationSpike * surgeDrain * 0.1f * Time.deltaTime;
            var slipStress = lateralSlip * slipDrain * Time.deltaTime;
            var recovery = (holdOn ? holdOnRecoveryPerSecond : recoveryPerSecond) * Time.deltaTime;
            var slowAndCenteredBonus = Mathf.Clamp01(1f - Mathf.Abs(forwardSpeed) / 6f) * Mathf.Clamp01(1f - lateralSlip / 2f) * 6f * Time.deltaTime;

            stability -= balanceStress + surgeStress + slipStress;
            stability += recovery + slowAndCenteredBonus;
            stability = Mathf.Clamp(stability, 0f, maxStability);
            previousForwardSpeed = forwardSpeed;

            if (stability <= maxStability * 0.08f)
            {
                lowStabilityTimer += Time.deltaTime;

                if (lowStabilityTimer >= lowStabilityEjectDelay)
                {
                    Eject("Lost stability");
                }
            }
            else
            {
                lowStabilityTimer = 0f;
            }
        }

        private void HandleImpact(float impactForce, Collision collision)
        {
            if (!IsMounted)
            {
                return;
            }

            stability = Mathf.Max(0f, stability - impactForce * impactDrainMultiplier);

            if (impactForce >= instantEjectImpactForce)
            {
                Eject("Hard impact");
            }
        }

        private void Eject(string reason)
        {
            if (!IsMounted)
            {
                return;
            }

            IsMounted = false;

            if (mountedRiderRoot != null)
            {
                mountedRiderRoot.gameObject.SetActive(false);
            }

            var throwVelocity = animalBody.linearVelocity + transform.forward * (Mathf.Abs(animalController.ForwardSpeed) * ejectionImpulseMultiplier) + Vector3.up * upwardEjectionImpulse;
            if (ragdollSystem != null)
            {
                ragdollSystem.Eject(seatAnchor != null ? seatAnchor.position : transform.position + Vector3.up, seatAnchor != null ? seatAnchor.rotation : transform.rotation, throwVelocity, reason);
            }

            if (cameraController != null && ragdollSystem != null && ragdollSystem.CurrentFocusTarget != null)
            {
                cameraController.ForceChaseView(ragdollSystem.CurrentFocusTarget, ragdollSystem.CurrentFocusTarget);
            }

            if (debugMountedState)
            {
                Debug.Log($"Rider ejected: {reason}");
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.EndRun(reason);
            }
        }
    }
}
