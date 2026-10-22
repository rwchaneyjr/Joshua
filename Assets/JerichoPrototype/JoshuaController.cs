using UnityEngine;

namespace JerichoPrototype
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class JoshuaController : MonoBehaviour
    {
        public Animator animator;
        public Transform cameraTransform;
        public float walkSpeed = 2.5f, runSpeed = 5f, turnSpeed = 540f;
        [Tooltip("Travel speed only; does not change animation playback speed.")]
        [Range(.5f, 2f)] public float travelSpeedMultiplier = 1.25f;
        public bool readKeyboard = true;
        public Vector2 MoveInput { get; set; }
        public bool RunInput { get; set; }
        public float PlanarSpeed { get; private set; }
        CharacterController motor;
        float verticalSpeed;

        void Awake() { motor = GetComponent<CharacterController>(); animator.applyRootMotion = false; }
        void Update()
        {
            if (readKeyboard)
            {
                MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                RunInput = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            }
            var orbitCamera = cameraTransform ? cameraTransform.GetComponent<ThirdPersonCamera>() : null;
            Vector3 forward = orbitCamera ? orbitCamera.MovementForward :
                cameraTransform ? cameraTransform.forward : Vector3.forward;
            forward.y = 0; forward.Normalize();
            // Follow mouse-look heading continuously; MovementForward excludes the temporary look-back.
            Vector3 direction = Vector3.ClampMagnitude(forward * MoveInput.y + Vector3.Cross(Vector3.up, forward) * MoveInput.x, 1);
            if (direction.sqrMagnitude > .001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), turnSpeed * Time.deltaTime);
            verticalSpeed = motor.isGrounded && verticalSpeed < 0 ? -2f : verticalSpeed - 20f * Time.deltaTime;
            Vector3 before = transform.position;
            motor.Move((direction * (RunInput ? runSpeed : walkSpeed) * travelSpeedMultiplier + Vector3.up * verticalSpeed) * Time.deltaTime);
            Vector3 displacement = transform.position - before; displacement.y = 0;
            PlanarSpeed = displacement.magnitude / Mathf.Max(Time.deltaTime, .0001f);
            animator.SetInteger("TestState", PlanarSpeed < .05f ? 1 : RunInput ? 3 : 2);
            animator.SetFloat("Speed", PlanarSpeed);
        }
    }
}
