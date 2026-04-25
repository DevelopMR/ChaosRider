using ChaosRider.Animals;
using ChaosRider.Cameras;
using ChaosRider.Rider;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChaosRider.Game
{
    public class PrototypeArenaBootstrap : MonoBehaviour
    {
        private const string BootstrapName = "__ChaosRiderBootstrap";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrap()
        {
            if (FindFirstObjectByType<PrototypeArenaBootstrap>() != null)
            {
                return;
            }

            var bootstrapObject = new GameObject(BootstrapName);
            bootstrapObject.AddComponent<PrototypeArenaBootstrap>();
        }

        private void Awake()
        {
            EnsureGameManager();
            EnsureArena();
        }

        private void EnsureGameManager()
        {
            if (FindFirstObjectByType<GameManager>() != null)
            {
                return;
            }

            var managerObject = new GameObject("GameManager");
            managerObject.AddComponent<GameManager>();
        }

        private void EnsureArena()
        {
            if (GameObject.Find("PrototypeArena_Root") != null)
            {
                return;
            }

            var arenaRoot = new GameObject("PrototypeArena_Root");

            CreateGround(arenaRoot.transform);
            CreateBounds(arenaRoot.transform);
            var bull = CreateBull(arenaRoot.transform);
            EnsureLight();
            EnsureCamera(bull.transform);
            SceneManager.MoveGameObjectToScene(arenaRoot, gameObject.scene);
        }

        private static void CreateGround(Transform parent)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Arena_Ground";
            ground.transform.SetParent(parent);
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(12f, 1f, 12f);

            CreateSafetyFloor(parent);
        }

        private static void CreateBounds(Transform parent)
        {
            CreateWall(parent, "North_Wall", new Vector3(0f, 2f, 48f), new Vector3(96f, 4f, 2f));
            CreateWall(parent, "South_Wall", new Vector3(0f, 2f, -48f), new Vector3(96f, 4f, 2f));
            CreateWall(parent, "East_Wall", new Vector3(48f, 2f, 0f), new Vector3(2f, 4f, 96f));
            CreateWall(parent, "West_Wall", new Vector3(-48f, 2f, 0f), new Vector3(2f, 4f, 96f));
        }

        private static GameObject CreateBull(Transform parent)
        {
            var bull = new GameObject("Bull_Controller");
            bull.transform.SetParent(parent);
            bull.transform.position = new Vector3(0f, 1.2f, 0f);

            var collider = bull.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0.35f, 0f);
            collider.height = 1.6f;
            collider.radius = 0.65f;
            collider.direction = 2;

            var body = bull.AddComponent<Rigidbody>();
            body.mass = 650f;
            body.centerOfMass = new Vector3(0f, -0.2f, 0.05f);
            body.maxAngularVelocity = 18f;

            bull.AddComponent<AnimalLocomotionController>();
            bull.AddComponent<GaitEngine>();
            bull.AddComponent<GaitDebugOverlay>();
            var animalController = bull.AddComponent<AnimalPhysicsController>();
            CreateCameraAnchors(bull.transform);

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Bull_Visual";
            visual.transform.SetParent(bull.transform);
            visual.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = new Vector3(1.35f, 1.1f, 2.2f);

            if (visual.TryGetComponent<Collider>(out var visualCollider))
            {
                Destroy(visualCollider);
            }

            CreateHorn(bull.transform, new Vector3(-0.35f, 0.75f, 1.15f), new Vector3(0.15f, 0.15f, 0.75f));
            CreateHorn(bull.transform, new Vector3(0.35f, 0.75f, 1.15f), new Vector3(0.15f, 0.15f, 0.75f));

            var mountedRider = CreateMountedRider(bull.transform);
            var ragdollSystem = bull.AddComponent<RiderRagdollSystem>();
            ragdollSystem.Configure(parent);

            var mountSystem = bull.AddComponent<RiderMountSystem>();
            mountSystem.Configure(
                animalController,
                body,
                bull.transform.Find("RiderSeatAnchor"),
                ragdollSystem,
                mountedRider,
                null);

            return bull;
        }

        private static void CreateCameraAnchors(Transform parent)
        {
            var riderSeatAnchor = new GameObject("RiderSeatAnchor");
            riderSeatAnchor.transform.SetParent(parent);
            riderSeatAnchor.transform.localPosition = new Vector3(0f, 0.72f, -0.7f);
            riderSeatAnchor.transform.localRotation = Quaternion.identity;

            var chaseTarget = new GameObject("ChaseLookTarget");
            chaseTarget.transform.SetParent(parent);
            chaseTarget.transform.localPosition = new Vector3(0f, 1.25f, 0.85f);
            chaseTarget.transform.localRotation = Quaternion.identity;
        }

        private static Transform CreateMountedRider(Transform parent)
        {
            var riderRoot = new GameObject("MountedRider");
            riderRoot.transform.SetParent(parent);
            riderRoot.transform.localPosition = new Vector3(0f, 0.72f, -0.85f);
            riderRoot.transform.localRotation = Quaternion.identity;

            var torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            torso.name = "RiderTorso";
            torso.transform.SetParent(riderRoot.transform);
            torso.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            torso.transform.localRotation = Quaternion.identity;
            torso.transform.localScale = new Vector3(0.45f, 0.55f, 0.35f);

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "RiderHead";
            head.transform.SetParent(riderRoot.transform);
            head.transform.localPosition = new Vector3(0f, 1.25f, -0.02f);
            head.transform.localScale = Vector3.one * 0.32f;

            var headCameraAnchor = new GameObject("HeadCameraAnchor");
            headCameraAnchor.transform.SetParent(head.transform);
            headCameraAnchor.transform.localPosition = new Vector3(0f, 0.03f, 0.12f);
            headCameraAnchor.transform.localRotation = Quaternion.identity;

            var leftLeg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            leftLeg.name = "LeftLeg";
            leftLeg.transform.SetParent(riderRoot.transform);
            leftLeg.transform.localPosition = new Vector3(-0.18f, 0.05f, 0.15f);
            leftLeg.transform.localScale = new Vector3(0.18f, 0.5f, 0.18f);

            var rightLeg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rightLeg.name = "RightLeg";
            rightLeg.transform.SetParent(riderRoot.transform);
            rightLeg.transform.localPosition = new Vector3(0.18f, 0.05f, 0.15f);
            rightLeg.transform.localScale = new Vector3(0.18f, 0.5f, 0.18f);

            foreach (var collider in riderRoot.GetComponentsInChildren<Collider>())
            {
                Destroy(collider);
            }

            return riderRoot.transform;
        }

        private static void CreateSafetyFloor(Transform parent)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Arena_SafetyFloor";
            floor.transform.SetParent(parent);
            floor.transform.position = new Vector3(0f, -8f, 0f);
            floor.transform.localScale = new Vector3(140f, 1f, 140f);

            if (floor.TryGetComponent<MeshRenderer>(out var renderer))
            {
                renderer.enabled = false;
            }
        }

        private static void CreateHorn(Transform parent, Vector3 localPosition, Vector3 localScale)
        {
            var horn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            horn.name = "Bull_Horn";
            horn.transform.SetParent(parent);
            horn.transform.localPosition = localPosition;
            horn.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            horn.transform.localScale = localScale;

            if (horn.TryGetComponent<Collider>(out var hornCollider))
            {
                Destroy(hornCollider);
            }
        }

        private static void CreateWall(Transform parent, string wallName, Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = wallName;
            wall.transform.SetParent(parent);
            wall.transform.position = position;
            wall.transform.localScale = scale;
        }

        private static void EnsureLight()
        {
            if (FindFirstObjectByType<Light>() != null)
            {
                return;
            }

            var lightObject = new GameObject("Directional Light");
            var lightComponent = lightObject.AddComponent<Light>();
            lightComponent.type = LightType.Directional;
            lightComponent.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void EnsureCamera(Transform target)
        {
            var mainCamera = Camera.main;

            if (mainCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            if (mainCamera.TryGetComponent<SimpleFollowCamera>(out var oldFollowCamera))
            {
                Destroy(oldFollowCamera);
            }

            var body = target.GetComponent<Rigidbody>();
            var animalController = target.GetComponent<AnimalPhysicsController>();
            var mountedCameraAnchor = target.Find("MountedRider/RiderHead/HeadCameraAnchor");
            if (mountedCameraAnchor == null)
            {
                mountedCameraAnchor = target.Find("RiderSeatAnchor");
            }
            var riderSeatAnchor = target.Find("RiderSeatAnchor");
            var chaseTarget = target.Find("ChaseLookTarget");
            var riderVisual = target.Find("MountedRider");
            var hiddenRenderers = new List<Renderer>();

            hiddenRenderers.AddRange(target.GetComponentsInChildren<Renderer>(true));

            if (riderVisual != null)
            {
                hiddenRenderers.AddRange(riderVisual.GetComponentsInChildren<Renderer>(true));
            }

            var cameraController = mainCamera.GetComponent<CameraModeController>();
            if (cameraController == null)
            {
                cameraController = mainCamera.gameObject.AddComponent<CameraModeController>();
            }

            cameraController.Configure(
                target,
                body,
                animalController,
                mountedCameraAnchor,
                chaseTarget,
                hiddenRenderers.ToArray());

            var mountSystem = target.GetComponent<RiderMountSystem>();
            var ragdollSystem = target.GetComponent<RiderRagdollSystem>();
            if (mountSystem != null)
            {
                mountSystem.Configure(
                    animalController,
                    body,
                    riderSeatAnchor,
                    ragdollSystem,
                    riderVisual,
                    cameraController);
            }
        }
    }
}
