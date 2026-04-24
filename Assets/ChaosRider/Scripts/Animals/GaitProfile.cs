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
        [Range(0.2f, 0.9f)] public float stanceFraction = 0.58f;
        public float idleThreshold = 0.08f;

        [Header("Contact Phase Offsets")]
        public float frontLeftPhaseOffset = 0f;
        public float frontRightPhaseOffset = 0.5f;
        public float rearLeftPhaseOffset = 0.5f;
        public float rearRightPhaseOffset = 0f;

        [Header("Forces")]
        public float supportForce = 34f;
        public float driveForce = 20f;
        public float brakingForce = 18f;
        public float frontSteerForce = 9f;
        public float rearCounterSteerForce = 5f;

        [Header("Body Reaction")]
        public float frontSupportBias = 1.08f;
        public float rearSupportBias = 0.92f;
        public float rollTorque = 3f;
        public float pitchTorque = 2.5f;
        public float settleForce = 15f;

        [Header("Body Tension")]
        public float uprightSpring = 18f;
        public float uprightDamping = 4.5f;
        public float verticalDamping = 3.5f;
        public float lateralDamping = 2.75f;
    }
}
