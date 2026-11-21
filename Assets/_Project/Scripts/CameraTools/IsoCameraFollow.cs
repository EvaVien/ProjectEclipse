using UnityEngine;

namespace Eclipse.CameraTools
{
    /// <summary>Изометрическая камера: фиксированный угол, плавное следование за целью.</summary>
    public class IsoCameraFollow : MonoBehaviour
    {
        public Transform target;
        public float distance = 25f;
        public float pitch = 50f;
        public float yaw = 45f;
        public float smoothTime = 0.12f;

        Vector3 velocity;

        Vector3 DesiredPosition()
        {
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            return target.position - rot * Vector3.forward * distance;
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = DesiredPosition();
        }

        void LateUpdate()
        {
            if (target == null) return;
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = Vector3.SmoothDamp(transform.position, DesiredPosition(), ref velocity, smoothTime);
        }
    }
}
