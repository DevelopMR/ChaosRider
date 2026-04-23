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
        [SerializeField] private Transform mountedAnchor;
        [SerializeField] private Transform chaseLookTarget;

        [Header("Mounted FPV")]
        [SerializeField] private Vector3 mountedOffset = new Vector3(0f, 0.62f, 0f);
        [SerializeField] private float mountedPositionSharpness = 18f;
        [SerializeField] private float mountedRotationSharpness = 14f;
        [SerializeField] private float mountedPitchInfluence = 0.55f;
        [SerializeField] private float mountedRollInfluence = 0.8f;
        [SerializeField] private float mountedYawLookAhead = 0.6f;
        [SerializeField] private float mountedFieldOfView = 72f;

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

        public void Configure(Transform targetRoot, Rigidbody targetBody, Transform mountedViewAnchor, Transform chaseViewTarget)
        {
            animalRoot = targetRoot;
            animalBody = targetBody;
            mountedAnchor = mountedViewAnchor;
            chaseLookTarget = chaseViewTarget;
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
            var desiredPosition = anchor.TransformPoint(mountedOffset);
            transform.position = DampPosition(transform.position, desiredPosition, mountedPositionSharpness);

            var localAngularVelocity = animalRoot.InverseTransformDirection(animalBody.angularVelocity);
            var pitch = -localAngularVelocity.x * mountedPitchInfluence;
            var roll = -localAngularVelocity.z * mountedRollInfluence;
            var yaw = localAngularVelocity.y * mountedYawLookAhead;

            var desiredRotation = anchor.rotation * Quaternion.Euler(pitch, yaw, roll);
            transform.rotation = DampRotation(transform.rotation, desiredRotation, mountedRotationSharpness);

            if (attachedCamera != null)
            {
                attachedCamera.fieldOfView = Mathf.Lerp(attachedCamera.fieldOfView, mountedFieldOfView, 1f - Mathf.Exp(-10f * Time.deltaTime));
            }
        }

        private void UpdateChaseCamera()
        {
            var desiredPosition = animalRoot.TransformPoint(chaseOffset);
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
