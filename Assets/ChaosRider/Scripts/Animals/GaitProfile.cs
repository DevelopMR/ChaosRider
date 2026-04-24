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
        public float supportForce = 42f;
        public float driveForce = 28f;
        public float brakingForce = 18f;
        public float frontSteerForce = 14f;
        public float rearCounterSteerForce = 8f;

        [Header("Body Reaction")]
        public float frontSupportBias = 1.08f;
        public float rearSupportBias = 0.92f;
        public float rollTorque = 5f;
        public float pitchTorque = 4f;
        public float settleForce = 15f;
    }
}
