using UnityEngine;

namespace ChaosRider.Animals
{
    [System.Serializable]
    public class AnimalProfile
    {
        [Header("Identity")]
        public string displayName = "Canine Reference Torso";
        public AnimalMood defaultMood = AnimalMood.Obedient;

        [Header("Torso Dimensions")]
        public float torsoLength = 2.2f;
        public float torsoWidth = 0.9f;
        public float torsoHeight = 1.1f;
        public float contactDepth = 0.15f;

        [Header("Locomotion Bias")]
        [Range(0f, 1f)] public float shoulderBias = 0.62f;
        [Range(0f, 1f)] public float hindDriveBias = 0.7f;
        public float frontTrackWidth = 0.48f;
        public float rearTrackWidth = 0.42f;
    }
}
