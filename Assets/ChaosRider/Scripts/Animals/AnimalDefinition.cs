using UnityEngine;

namespace ChaosRider.Animals
{
    [CreateAssetMenu(menuName = "Chaos Rider/Animals/Animal Definition", fileName = "AnimalDefinition")]
    public class AnimalDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Bull";

        [Header("Physics")]
        public float mass = 650f;
        public float drag = 0.4f;
        public float angularDrag = 1.5f;
        public float maxForwardSpeed = 18f;

        [Header("Drive")]
        public float cruiseForce = 2600f;
        public float chargeForce = 4200f;
        public float brakeForce = 1800f;
        public float reverseForce = 900f;
        public float turnTorque = 320f;
        public float steeringAssist = 0.2f;

        [Header("Chaos")]
        public float buckImpulse = 8f;
        public float buckTorque = 18f;
        public float lateralKick = 4f;
        public float chaosJitterTorque = 6f;
        public Vector2 buckIntervalRange = new Vector2(0.3f, 0.75f);
    }
}
