using UnityEngine;

namespace ChaosRider.Animals
{
    [System.Serializable]
    public class GaitProfile
    {
        [Header("Identity")]
        public GaitType gaitType = GaitType.DogTrot;

        [Header("Timing")]
        public float cycleDurationAtLowSpeed = 0.72f;
        public float cycleDurationAtHighSpeed = 0.4f;
        [Range(0.2f, 0.9f)] public float stanceFraction = 0.64f;
        public float idleThreshold = 0.08f;

        [Header("Contact Phase Offsets")]
        public float frontLeftPhaseOffset = 0f;
        public float frontRightPhaseOffset = 0.5f;
        public float rearLeftPhaseOffset = 0.5f;
        public float rearRightPhaseOffset = 0f;

        [Header("Forces")]
        public float supportForce = 31f;
        public float driveForce = 18f;
        public float brakingForce = 18f;
        public float frontSteerForce = 8f;
        public float rearCounterSteerForce = 4f;

        [Header("Body Reaction")]
        public float frontSupportBias = 1.08f;
        public float rearSupportBias = 0.92f;
        public float rollTorque = 2.25f;
        public float pitchTorque = 1.75f;
        public float settleForce = 15f;

        [Header("Body Tension")]
        public float uprightSpring = 18f;
        public float uprightDamping = 4.5f;
        public float verticalDamping = 3.5f;
        public float lateralDamping = 2.75f;

        [Header("Torso Rhythm")]
        public float cadenceRollTorque = 1.8f;
        public float cadencePitchTorque = 1.1f;
        public float cadenceSurgeForce = 4.5f;
    }
}
