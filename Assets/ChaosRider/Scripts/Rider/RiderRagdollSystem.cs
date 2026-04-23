using System.Collections.Generic;
using UnityEngine;

namespace ChaosRider.Rider
{
    public class RiderRagdollSystem : MonoBehaviour
    {
        [Header("Ragdoll")]
        [SerializeField] private Transform ragdollParent;
        [SerializeField] private float torsoMass = 18f;
        [SerializeField] private float limbMass = 5f;
        [SerializeField] private float headMass = 4f;
        [SerializeField] private float additionalScatterForce = 2.5f;
        [SerializeField] private float ragdollLifetime = 12f;

        private GameObject currentRagdoll;
        public Transform CurrentFocusTarget { get; private set; }

        public void Configure(Transform parent)
        {
            ragdollParent = parent;
        }

        public void Eject(Vector3 position, Quaternion rotation, Vector3 initialVelocity, string reason)
        {
            if (currentRagdoll != null)
            {
                Destroy(currentRagdoll);
            }

            currentRagdoll = BuildRagdoll(position, rotation, initialVelocity, reason);
        }

        private GameObject BuildRagdoll(Vector3 position, Quaternion rotation, Vector3 initialVelocity, string reason)
        {
            var root = new GameObject("Rider_Ragdoll");
            root.transform.SetParent(ragdollParent);
            root.transform.position = position;
            root.transform.rotation = rotation;

            var torso = CreatePart(root.transform, "Torso", PrimitiveType.Capsule, new Vector3(0f, 0.8f, 0f), new Vector3(0.5f, 0.65f, 0.35f), torsoMass, Color.gray);
            var head = CreatePart(root.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.6f, 0.03f), Vector3.one * 0.28f, headMass, new Color(0.82f, 0.72f, 0.62f));
            var leftArm = CreatePart(root.transform, "LeftArm", PrimitiveType.Capsule, new Vector3(-0.45f, 1.1f, 0f), new Vector3(0.18f, 0.45f, 0.18f), limbMass, Color.gray);
            var rightArm = CreatePart(root.transform, "RightArm", PrimitiveType.Capsule, new Vector3(0.45f, 1.1f, 0f), new Vector3(0.18f, 0.45f, 0.18f), limbMass, Color.gray);
            var leftLeg = CreatePart(root.transform, "LeftLeg", PrimitiveType.Capsule, new Vector3(-0.18f, 0.15f, 0f), new Vector3(0.2f, 0.55f, 0.2f), limbMass, Color.gray);
            var rightLeg = CreatePart(root.transform, "RightLeg", PrimitiveType.Capsule, new Vector3(0.18f, 0.15f, 0f), new Vector3(0.2f, 0.55f, 0.2f), limbMass, Color.gray);

            Connect(head, torso, new Vector3(0f, -0.2f, 0f), Vector3.up);
            Connect(leftArm, torso, new Vector3(0.25f, 0.4f, 0f), Vector3.left);
            Connect(rightArm, torso, new Vector3(-0.25f, 0.4f, 0f), Vector3.right);
            Connect(leftLeg, torso, new Vector3(0.12f, 0.52f, 0f), Vector3.down);
            Connect(rightLeg, torso, new Vector3(-0.12f, 0.52f, 0f), Vector3.down);

            var parts = root.GetComponentsInChildren<Rigidbody>();
            foreach (var part in parts)
            {
                part.linearVelocity = initialVelocity;
                part.angularVelocity = Random.insideUnitSphere * additionalScatterForce;
            }

            root.name = $"Rider_Ragdoll_{reason.Replace(" ", string.Empty)}";
            CurrentFocusTarget = torso.transform;
            Destroy(root, ragdollLifetime);
            return root;
        }

        private static GameObject CreatePart(Transform parent, string name, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale, float mass, Color color)
        {
            var part = GameObject.CreatePrimitive(primitiveType);
            part.name = name;
            part.transform.SetParent(parent);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = localScale;

            if (part.TryGetComponent<MeshRenderer>(out var renderer))
            {
                renderer.material.color = color;
            }

            var rigidbody = part.AddComponent<Rigidbody>();
            rigidbody.mass = mass;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            return part;
        }

        private static void Connect(GameObject child, GameObject connectedBody, Vector3 anchor, Vector3 axis)
        {
            var joint = child.AddComponent<CharacterJoint>();
            joint.connectedBody = connectedBody.GetComponent<Rigidbody>();
            joint.anchor = anchor;
            joint.axis = axis;
            joint.swingAxis = Vector3.forward;
            joint.lowTwistLimit = new SoftJointLimit { limit = -25f };
            joint.highTwistLimit = new SoftJointLimit { limit = 25f };
            joint.swing1Limit = new SoftJointLimit { limit = 25f };
            joint.swing2Limit = new SoftJointLimit { limit = 25f };
            joint.enableProjection = true;
            joint.enablePreprocessing = false;
        }
    }
}
