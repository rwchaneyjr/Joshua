using UnityEngine;

namespace SharedHumanoidAnimation
{
    /// <summary>
    /// Drives the shared humanoid controller from the actual movement of this GameObject.
    /// Assign the generated SharedHumanoid.controller and an Animator with a valid Humanoid Avatar.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class SharedHumanoidAnimationCoordinator : MonoBehaviour
    {
        [SerializeField] private RuntimeAnimatorController sharedController;
        [SerializeField, Min(0.01f)] private float speedDampTime = 0.12f;
        [SerializeField, Min(0f)] private float movementThreshold = 0.02f;
        [SerializeField] private bool useCharacterControllerVelocity = true;
        [SerializeField] private bool useRigidbodyVelocity = true;

        private static readonly int Speed = Animator.StringToHash("Speed");
        private Animator animator;
        private CharacterController characterController;
        private Rigidbody body;
        private Vector3 previousPosition;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            characterController = GetComponent<CharacterController>();
            body = GetComponent<Rigidbody>();

            if (sharedController != null && animator.runtimeAnimatorController != sharedController)
            {
                animator.runtimeAnimatorController = sharedController;
            }

            if (animator.avatar == null || !animator.avatar.isHuman)
            {
                Debug.LogWarning("SharedHumanoidAnimationCoordinator requires an Animator with a valid Humanoid Avatar.", this);
            }

            previousPosition = transform.position;
        }

        private void Update()
        {
            float speed = ReadPlanarSpeed();
            animator.SetFloat(Speed, speed < movementThreshold ? 0f : speed, speedDampTime, Time.deltaTime);
            previousPosition = transform.position;
        }

        private float ReadPlanarSpeed()
        {
            Vector3 velocity;
            if (useCharacterControllerVelocity && characterController != null)
            {
                velocity = characterController.velocity;
            }
            else if (useRigidbodyVelocity && body != null)
            {
                velocity = body.velocity;
            }
            else
            {
                velocity = (transform.position - previousPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
            }

            velocity.y = 0f;
            return velocity.magnitude;
        }
    }
}