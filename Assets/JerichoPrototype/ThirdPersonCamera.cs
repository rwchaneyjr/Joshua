using UnityEngine;

namespace JerichoPrototype
{
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        public Transform target;
        public float distance = 8f, height = 1.5f, sensitivity = 3f, smoothing = 14f;
        public LayerMask collisionMask = 1;
        float yaw, pitch = 24f;
        void LateUpdate()
        {
            if (!target) return;
            if (Input.GetKeyDown(KeyCode.Escape)) Cursor.lockState = CursorLockMode.None;
            if (Input.GetMouseButtonDown(0)) Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                yaw += Input.GetAxis("Mouse X") * sensitivity;
                pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * sensitivity, 10, 65);
            }
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 pivot = target.position + Vector3.up * height;
            Vector3 back = rotation * Vector3.back;
            float length = distance;
            if (Physics.SphereCast(pivot, .25f, back, out RaycastHit hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
                length = Mathf.Max(.3f, hit.distance - .1f);
            Vector3 desired = pivot + back * length;
            transform.position = Vector3.Lerp(transform.position, desired, 1 - Mathf.Exp(-smoothing * Time.deltaTime));
            // Clamp after smoothing too, so following does not interpolate through a wall.
            Vector3 delta = transform.position - pivot;
            if (Physics.SphereCast(pivot, .25f, delta.normalized, out hit, delta.magnitude, collisionMask, QueryTriggerInteraction.Ignore))
                transform.position = pivot + delta.normalized * Mathf.Max(.3f, hit.distance - .1f);
            transform.LookAt(pivot);
        }
        void OnDisable() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }
}
