using UnityEngine;

namespace ChaosRider.Animals
{
    [System.Serializable]
    public class GaitProfile
    {
        [Header("Identity")]
        public GaitType gaitType = GaitType.DogTrot;

        [Header("Timing")]
        public float cycleDurationAtLowSpeed = 0.82f;
        public float cycleDurationAtHighSpeed = 0.46f;
        [Range(0.2f, 0.9f)] public float stanceFraction = 0.64f;
        public float idleThreshold = 0.08f;

        [Header("Contact Phase Offsets")]
        public float frontLeftPhaseOffset = 0f;
        public float frontRightPhaseOffset = 0.5f;
        public float rearLeftPhaseOffset = 0.5f;
        public float rearRightPhaseOffset = 0f;

        [Header("Forces")]
        public float supportForce = 31f;
        public float driveForce = 16f;
        public float brakingForce = 18f;
        public float frontSteerForce = 8f;
        public float rearCounterSteerForce = 4f;
        public float idleTurnTorque = 10f;
        public float lowSpeedSteerScale = 0.35f;

        [Header("Body Reaction")]
        public float frontSupportBias = 1.08f;
        public float rearSupportBias = 0.92f;
        public float rollTorque = 2.25f;
        public float pitchTorque = 1.1f;
        public float settleForce = 15f;

        [Header("Body Tension")]
        public float uprightSpring = 18f;
        public float uprightDamping = 4.5f;
        public float verticalDamping = 3.5f;
        public float lateralDamping = 2.75f;
        public float longitudinalDamping = 1.8f;

        [Header("Torso Rhythm")]
        public float cadenceRollTorque = 1.35f;
        public float cadencePitchTorque = 0.45f;
        public float cadenceSurgeForce = 1.6f;
    }
}
