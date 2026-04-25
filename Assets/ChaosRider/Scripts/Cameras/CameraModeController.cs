using ChaosRider.Animals;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChaosRider.Cameras
{
    public class CameraModeController : MonoBehaviour
    {
        private enum CameraMode
        {
            MountedFirstPerson = 0,
            Chase = 1,
        }

        [Header("Target")]
        [SerializeField] private Transform animalRoot;
        [SerializeField] private Rigidbody animalBody;
        [SerializeField] private AnimalPhysicsController animalController;
        [SerializeField] private GaitEngine gaitEngine;
        [SerializeField] private Transform mountedAnchor;
        [SerializeField] private Transform chaseLookTarget;
        [SerializeField] private Renderer[] hiddenInMountedView;

        [Header("Mounted FPV")]
        [SerializeField] private Vector3 mountedOffset = new Vector3(0f, 0.48f, -0.18f);
        [SerializeField] private float mountedPositionSharpness = 18f;
        [SerializeField] private float mountedRotationSharpness = 14f;
        [SerializeField] private float mountedPitchInfluence = 0.55f;
        [SerializeField] private float mountedRollInfluence = 0.8f;
        [SerializeField] private float mountedYawLookAhead = 0.6f;
        [SerializeField] private float mountedFieldOfView = 72f;
        [SerializeField] private float mountedSpeedFieldOfViewBoost = 5f;
        [SerializeField] private float mountedBobAmount = 0.08f;
        [SerializeField] private float mountedBobSpeed = 7f;
        [SerializeField] private float mountedGaitPitch = 1.4f;
        [SerializeField] private float mountedGaitRoll = 1.2f;

        [Header("Chase Cam")]
        [SerializeField] private Vector3 chaseOffset = new Vector3(0f, 3.25f, -6.5f);
        [SerializeField] private float chasePositionSharpness = 5.5f;
        [SerializeField] private float chaseRotationSharpness = 7f;
        [SerializeField] private float chaseFieldOfView = 64f;

        [Header("Mode Switch")]
        [SerializeField] private Key toggleKey = Key.C;
        [SerializeField] private bool startInMountedView = true;

        private Camera attachedCamera;
        private CameraMode currentMode;
        private CameraMode lastAppliedMode = (CameraMode)(-1);
        private float bobTime;

        public void Configure(Transform targetRoot, Rigidbody targetBody, AnimalPhysicsController targetController, Transform mountedViewAnchor, Transform chaseViewTarget, Renderer[] hideInMountedView)
        {
            animalRoot = targetRoot;
            animalBody = targetBody;
            animalController = targetController;
            gaitEngine = targetRoot != null ? targetRoot.GetComponent<GaitEngine>() : null;
            mountedAnchor = mountedViewAnchor;
            chaseLookTarget = chaseViewTarget;
            hiddenInMountedView = hideInMountedView;
        }

        public void ForceChaseView(Transform targetRoot, Transform lookTarget)
        {
            animalRoot = targetRoot;
            chaseLookTarget = lookTarget;
            animalBody = targetRoot != null ? targetRoot.GetComponent<Rigidbody>() : null;
            animalController = null;
            gaitEngine = null;
            currentMode = CameraMode.Chase;
            lastAppliedMode = (CameraMode)(-1);
        }

        private void Awake()
        {
            attachedCamera = GetComponent<Camera>();
            currentMode = startInMountedView ? CameraMode.MountedFirstPerson : CameraMode.Chase;
        }

        private void LateUpdate()
        {
            if (animalRoot == null || animalBody == null)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                currentMode = currentMode == CameraMode.MountedFirstPerson ? CameraMode.Chase : CameraMode.MountedFirstPerson;
            }

            ApplyVisibilityForMode();

            if (currentMode == CameraMode.MountedFirstPerson)
            {
                UpdateMountedCamera();
            }
            else
            {
                UpdateChaseCamera();
            }
        }

        private void UpdateMountedCamera()
        {
            var anchor = mountedAnchor != null ? mountedAnchor : animalRoot;
            var normalizedSpeed = animalController != null ? Mathf.Clamp01(Mathf.Abs(animalController.ForwardSpeed) / 8f) : 0f;
            var bobSignal = Mathf.Sin(bobTime);
            var gaitPitch = 0f;
            var gaitRoll = 0f;

            if (gaitEngine != null && gaitEngine.RideCouplingStrength > 0f)
            {
                bobSignal = gaitEngine.RideVerticalSignal;
                gaitPitch = gaitEngine.RidePitchSignal * mountedGaitPitch * normalizedSpeed * gaitEngine.RideCouplingStrength;
                gaitRoll = gaitEngine.RideRollSignal * mountedGaitRoll * normalizedSpeed * gaitEngine.RideCouplingStrength;
            }

            var bob = Vector3.up * (bobSignal * mountedBobAmount * normalizedSpeed);
            var desiredPosition = anchor.TransformPoint(mountedOffset) + bob;
            transform.position = DampPosition(transform.position, desiredPosition, mountedPositionSharpness);

            var localAngularVelocity = animalRoot.InverseTransformDirection(animalBody.angularVelocity);
            var pitch = -localAngularVelocity.x * mountedPitchInfluence + gaitPitch;
            var roll = -localAngularVelocity.z * mountedRollInfluence + gaitRoll;
            var yaw = localAngularVelocity.y * mountedYawLookAhead;
            bobTime += Time.deltaTime * mountedBobSpeed * Mathf.Clamp01(animalController != null ? Mathf.Abs(animalController.ForwardSpeed) / 6f : 0f);

            var desiredRotation = anchor.rotation * Quaternion.Euler(pitch, yaw, roll);
            transform.rotation = DampRotation(transform.rotation, desiredRotation, mountedRotationSharpness);

            if (attachedCamera != null)
            {
                var speedBoost = animalController != null ? animalController.NormalizedSpeed * mountedSpeedFieldOfViewBoost : 0f;
                attachedCamera.fieldOfView = Mathf.Lerp(attachedCamera.fieldOfView, mountedFieldOfView + speedBoost, 1f - Mathf.Exp(-10f * Time.deltaTime));
            }
        }

        private void UpdateChaseCamera()
        {
            var flattenedForward = Vector3.ProjectOnPlane(animalRoot.forward, Vector3.up);
            var basisRotation = flattenedForward.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(flattenedForward.normalized, Vector3.up)
                : Quaternion.identity;
            var desiredPosition = animalRoot.position + basisRotation * chaseOffset;
            transform.position = DampPosition(transform.position, desiredPosition, chasePositionSharpness);

            var lookTarget = chaseLookTarget != null ? chaseLookTarget.position : animalRoot.position + Vector3.up * 1.4f;
            var toTarget = lookTarget - transform.position;
            if (toTarget.sqrMagnitude > 0.001f)
            {
                var desiredRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
                transform.rotation = DampRotation(transform.rotation, desiredRotation, chaseRotationSharpness);
            }

            if (attachedCamera != null)
            {
                attachedCamera.fieldOfView = Mathf.Lerp(attachedCamera.fieldOfView, chaseFieldOfView, 1f - Mathf.Exp(-10f * Time.deltaTime));
            }
        }

        private void ApplyVisibilityForMode()
        {
            if (lastAppliedMode == currentMode || hiddenInMountedView == null)
            {
                return;
            }

            var shouldHide = currentMode == CameraMode.MountedFirstPerson;
            foreach (var renderer in hiddenInMountedView)
            {
                if (renderer == null)
                {
                    continue;
                }

                renderer.enabled = !shouldHide;
            }

            lastAppliedMode = currentMode;
        }

        private static Vector3 DampPosition(Vector3 current, Vector3 target, float sharpness)
        {
            return Vector3.Lerp(current, target, 1f - Mathf.Exp(-sharpness * Time.deltaTime));
        }

        private static Quaternion DampRotation(Quaternion current, Quaternion target, float sharpness)
        {
            return Quaternion.Slerp(current, target, 1f - Mathf.Exp(-sharpness * Time.deltaTime));
        }
    }
}
