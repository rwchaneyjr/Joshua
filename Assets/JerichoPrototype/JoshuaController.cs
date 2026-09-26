using UnityEngine;

namespace JerichoPrototype
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class JoshuaController : MonoBehaviour
    {
        public Animator animator;
        public Transform cameraTransform;
        public float walkSpeed = 2.5f, runSpeed = 5f, turnSpeed = 540f;
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
            Vector3 forward = cameraTransform ? cameraTransform.forward : Vector3.forward;
            forward.y = 0; forward.Normalize();
            Vector3 direction = Vector3.ClampMagnitude(forward * MoveInput.y + Vector3.Cross(Vector3.up, forward) * MoveInput.x, 1);
            if (direction.sqrMagnitude > .001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), turnSpeed * Time.deltaTime);
            verticalSpeed = motor.isGrounded && verticalSpeed < 0 ? -2f : verticalSpeed - 20f * Time.deltaTime;
            Vector3 before = transform.position;
            motor.Move((direction * (RunInput ? runSpeed : walkSpeed) + Vector3.up * verticalSpeed) * Time.deltaTime);
            Vector3 displacement = transform.position - before; displacement.y = 0;
            PlanarSpeed = displacement.magnitude / Mathf.Max(Time.deltaTime, .0001f);
            animator.SetInteger("TestState", PlanarSpeed < .05f ? 1 : RunInput ? 3 : 2);
            animator.SetFloat("Speed", PlanarSpeed);
        }
    }
}
