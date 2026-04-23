using UnityEngine;

namespace ChaosRider.Cameras
{
    public class SimpleFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 4.5f, -8f);
        [SerializeField] private float positionLerpSpeed = 5f;
        [SerializeField] private float lookLerpSpeed = 7f;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            var desiredPosition = target.TransformPoint(followOffset);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, 1f - Mathf.Exp(-positionLerpSpeed * Time.deltaTime));

            var lookDirection = (target.position + Vector3.up * 1.25f) - transform.position;
            if (lookDirection.sqrMagnitude < 0.001f)
            {
                return;
            }

            var desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, 1f - Mathf.Exp(-lookLerpSpeed * Time.deltaTime));
        }
    }
}
