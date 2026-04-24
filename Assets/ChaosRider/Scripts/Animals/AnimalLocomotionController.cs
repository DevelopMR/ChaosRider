using UnityEngine;
using UnityEngine.InputSystem;

namespace ChaosRider.Animals
{
    public class AnimalLocomotionController : MonoBehaviour
    {
        [SerializeField] private AnimalMood currentMood = AnimalMood.Obedient;
        [SerializeField] private float throttleSharpness = 7f;
        [SerializeField] private float steeringSharpness = 8f;

        public float DesiredThrottle { get; private set; }
        public float DesiredSteering { get; private set; }
        public AnimalMood CurrentMood => currentMood;

        private void Update()
        {
            var rawThrottle = ReadThrottle();
            var rawSteering = ReadSteering();

            DesiredThrottle = Mathf.MoveTowards(DesiredThrottle, ApplyMoodToThrottle(rawThrottle), throttleSharpness * Time.deltaTime);
            DesiredSteering = Mathf.MoveTowards(DesiredSteering, ApplyMoodToSteering(rawSteering), steeringSharpness * Time.deltaTime);
        }

        public void SetMood(AnimalMood newMood)
        {
            currentMood = newMood;
        }

        private float ApplyMoodToThrottle(float rawThrottle)
        {
            return currentMood switch
            {
                AnimalMood.Obedient => rawThrottle,
                AnimalMood.Agreeable => rawThrottle * 0.95f,
                AnimalMood.Angry => rawThrottle * 1.05f,
                AnimalMood.Petulant => rawThrottle * 0.85f,
                AnimalMood.Protective => rawThrottle * 0.9f,
                _ => rawThrottle,
            };
        }

        private float ApplyMoodToSteering(float rawSteering)
        {
            return currentMood switch
            {
                AnimalMood.Obedient => rawSteering,
                AnimalMood.Agreeable => rawSteering * 0.95f,
                AnimalMood.Angry => rawSteering * 0.9f,
                AnimalMood.Petulant => rawSteering * 0.75f,
                AnimalMood.Protective => rawSteering * 0.85f,
                _ => rawSteering,
            };
        }

        private static float ReadThrottle()
        {
            var throttle = 0f;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                {
                    throttle += 1f;
                }

                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                {
                    throttle -= 1f;
                }
            }

            return Mathf.Clamp(throttle, -1f, 1f);
        }

        private static float ReadSteering()
        {
            var steering = 0f;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                {
                    steering -= 1f;
                }

                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                {
                    steering += 1f;
                }
            }

            return Mathf.Clamp(steering, -1f, 1f);
        }
    }
}
