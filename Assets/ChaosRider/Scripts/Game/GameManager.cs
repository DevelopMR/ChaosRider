using UnityEngine;

namespace ChaosRider.Game
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private RunState initialState = RunState.Active;

        public RunState CurrentState { get; private set; }
        public float RunStartTime { get; private set; }
        public float RunDuration => CurrentState == RunState.Active ? Time.time - RunStartTime : Mathf.Max(0f, endTime - RunStartTime);
        public string EndReason { get; private set; } = string.Empty;

        private float endTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            CurrentState = initialState;
            RunStartTime = Time.time;
            endTime = RunStartTime;
        }

        public void StartRun()
        {
            CurrentState = RunState.Active;
            RunStartTime = Time.time;
            endTime = RunStartTime;
            EndReason = string.Empty;
        }

        public void EndRun(string reason)
        {
            if (CurrentState == RunState.Ended)
            {
                return;
            }

            CurrentState = RunState.Ended;
            endTime = Time.time;
            EndReason = reason;
        }
    }
}
