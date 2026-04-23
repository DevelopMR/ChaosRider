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
        [SerializeField] private Transform seatAnchor;
        [SerializeField] private RiderRagdollSystem ragdollSystem;
        [SerializeField] private CameraModeController cameraController;

        [Header("Mounted Rider")]
        [SerializeField] private Transform mountedRiderRoot;
        [SerializeField] private float riderPositionSharpness = 16f;
        [SerializeField] private float riderRotationSharpness = 10f;
        [SerializeField] private float riderLean = 12f;
        [SerializeField] private float riderRoll = 16f;

        [Header("Stability")]
        [SerializeField] private float maxStability = 100f;
        [SerializeField] private float recoveryPerSecond = 4f;
        [SerializeField] private float holdOnRecoveryPerSecond = 18f;
        [SerializeField] private float angularDrain = 2.4f;
        [SerializeField] private float speedDrain = 3.5f;
        [SerializeField] private float impactDrainMultiplier = 0.0035f;
        [SerializeField] private float instantEjectImpactForce = 9000f;
        [SerializeField] private float ejectionImpulseMultiplier = 0.55f;
        [SerializeField] private float upwardEjectionImpulse = 7f;
        [SerializeField] private Key holdOnKey = Key.Space;
        [SerializeField] private Key debugEjectKey = Key.E;

        [Header("Debug")]
        [SerializeField] private bool debugMountedState = true;

        public float StabilityNormalized => Mathf.Clamp01(stability / Mathf.Max(1f, maxStability));
        public bool IsMounted { get; private set; } = true;

        private float stability;

        public void Configure(AnimalPhysicsController controller, Rigidbody body, Transform mountAnchor, RiderRagdollSystem riderRagdollSystem, Transform riderVisualRoot, CameraModeController riderCameraController)
        {
            if (animalController != null)
            {
                animalController.Impacted -= HandleImpact;
            }

            animalController = controller;
            animalBody = body;
            seatAnchor = mountAnchor;
            ragdollSystem = riderRagdollSystem;
            mountedRiderRoot = riderVisualRoot;
            cameraController = riderCameraController;

            if (animalController != null)
            {
                animalController.Impacted += HandleImpact;
            }
        }

        private void Awake()
        {
            stability = maxStability;
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
            mountedRiderRoot.position = Vector3.Lerp(mountedRiderRoot.position, desiredPosition, 1f - Mathf.Exp(-riderPositionSharpness * Time.deltaTime));

            var localAngularVelocity = transform.InverseTransformDirection(animalBody.angularVelocity);
            var leanPitch = -localAngularVelocity.x * riderLean;
            var leanRoll = -localAngularVelocity.z * riderRoll;
            var desiredRotation = seatAnchor.rotation * Quaternion.Euler(leanPitch, 0f, leanRoll);
            mountedRiderRoot.rotation = Quaternion.Slerp(mountedRiderRoot.rotation, desiredRotation, 1f - Mathf.Exp(-riderRotationSharpness * Time.deltaTime));
        }

        private void UpdateStability()
        {
            var holdOn = Keyboard.current != null && Keyboard.current[holdOnKey].isPressed;
            var angularStress = animalBody.angularVelocity.magnitude * angularDrain * Time.deltaTime;
            var speedStress = Mathf.Abs(animalController.ForwardSpeed) * speedDrain * 0.05f * Time.deltaTime;
            var recovery = (holdOn ? holdOnRecoveryPerSecond : recoveryPerSecond) * Time.deltaTime;

            stability -= angularStress + speedStress;
            stability += recovery;
            stability = Mathf.Clamp(stability, 0f, maxStability);

            if (stability <= 0.01f)
            {
                Eject("Lost stability");
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
