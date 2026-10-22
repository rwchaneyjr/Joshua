using UnityEngine;

namespace JerichoPrototype
{
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        public Transform target;
        public float distance = 8f, height = 1.5f, sensitivity = 3f, smoothing = 14f;
        public LayerMask collisionMask = 1;
        float yaw, pitch = 24f;
        bool looking;
        float lookBackUntil;
        bool wasLookingBack;
        Vector3 overviewOffset;
        float overviewDistance;
        bool hornView;
        Vector3 hornFocus, hornPosition;
        public bool ShowingHornView => hornView;
        public void ShowHornView(Bounds priests, Vector3 cityCenter)
        {
            hornView = true;
            lookBackUntil = 0;
            hornFocus = priests.center;
            var camera = GetComponent<Camera>();
            float vertical = camera.fieldOfView * Mathf.Deg2Rad * .5f;
            float angle = Mathf.Min(vertical, Mathf.Atan(Mathf.Tan(vertical)*camera.aspect));
            float range = Mathf.Max(10, priests.extents.magnitude / Mathf.Sin(angle)*1.25f);
            Vector3 outward = hornFocus-cityCenter; outward.y=0;
            if(outward.sqrMagnitude<.01f)outward=-target.forward;
            hornPosition = hornFocus+(outward.normalized+Vector3.up*.65f).normalized*range;
        }
        public void EndHornView() { hornView=false; wasLookingBack=true; }
        public Vector3 MovementForward => Quaternion.Euler(0, yaw, 0) * Vector3.forward;
        void LateUpdate()
        {
            if (!target) return;
            if (hornView)
            {
                Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
                transform.position=Vector3.Lerp(transform.position,hornPosition,1-Mathf.Exp(-6*Time.deltaTime));
                transform.LookAt(hornFocus);
                return;
            }
            if (Input.GetMouseButtonDown(0) && !Input.GetMouseButton(1))
            {
                FrameProcession();
                looking = false;
                lookBackUntil = Time.unscaledTime + 1f;
            }
            if (Input.GetMouseButtonDown(1)) looking = true;
            if (Input.GetKeyDown(KeyCode.Escape)) looking = false;
            if (looking || Input.GetKeyDown(KeyCode.Escape)) lookBackUntil = 0;
            Cursor.lockState = looking ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !looking;
            if (looking)
            {
                yaw += Input.GetAxis("Mouse X") * sensitivity;
                pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * sensitivity, 10, 65);
            }
            bool lookingBack = Time.unscaledTime < lookBackUntil;
            Quaternion rotation = Quaternion.Euler(lookingBack ? 55f : pitch, yaw + (lookingBack ? 180f : 0f), 0);
            Vector3 pivot = target.position + (lookingBack ? overviewOffset : Vector3.up * height);
            Vector3 back = rotation * Vector3.back;
            float length = lookingBack ? overviewDistance : distance;
            if (Physics.SphereCast(pivot, .25f, back, out RaycastHit hit, length, collisionMask, QueryTriggerInteraction.Ignore))
                length = Mathf.Max(.3f, hit.distance - .1f);
            Vector3 desired = pivot + back * length;
            // Snap between opposite views instead of interpolating through the player.
            transform.position = lookingBack != wasLookingBack ? desired :
                Vector3.Lerp(transform.position, desired, 1 - Mathf.Exp(-smoothing * Time.deltaTime));
            wasLookingBack = lookingBack;
            // Clamp after smoothing too, so following does not interpolate through a wall.
            Vector3 delta = transform.position - pivot;
            if (Physics.SphereCast(pivot, .25f, delta.normalized, out hit, delta.magnitude, collisionMask, QueryTriggerInteraction.Ignore))
                transform.position = pivot + delta.normalized * Mathf.Max(.3f, hit.distance - .1f);
            transform.LookAt(pivot);
        }
        void FrameProcession()
        {
            var bounds = new Bounds(target.position + Vector3.up, new Vector3(2, 2, 2));
            foreach (var follower in FindObjectsOfType<ProcessionFollower>())
                if (follower.gameObject.scene == gameObject.scene)
                    foreach (var renderer in follower.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(renderer.bounds);
            foreach (var rig in FindObjectsOfType<ArkProcession.ArkCarryRig>())
                if (rig.gameObject.scene == gameObject.scene)
                    foreach (var renderer in rig.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(renderer.bounds);
            overviewOffset = bounds.center - target.position;
            var camera = GetComponent<Camera>();
            float halfVertical = (camera ? camera.fieldOfView : 60f) * Mathf.Deg2Rad * .5f;
            float halfHorizontal = Mathf.Atan(Mathf.Tan(halfVertical) * (camera ? camera.aspect : 1f));
            float halfAngle = Mathf.Min(halfVertical, halfHorizontal);
            overviewDistance = Mathf.Max(distance, bounds.extents.magnitude / Mathf.Sin(halfAngle) * 1.15f);
        }
        void ReleaseLook() { looking = false; lookBackUntil = 0; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        void OnDisable() { hornView=false; ReleaseLook(); }
        void OnApplicationFocus(bool focused) { if (!focused) ReleaseLook(); }
    }
}
