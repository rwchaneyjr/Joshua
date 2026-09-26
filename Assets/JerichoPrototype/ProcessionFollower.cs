using UnityEngine;
namespace JerichoPrototype
{
    [RequireComponent(typeof(CharacterController), typeof(FormationSlot))]
    public sealed class ProcessionFollower : MonoBehaviour
    {
        public Animator animator;
        public ProcessionManager procession;
        public MarchCoordinator march;
        public float maxSpeed = 6.5f;
        public bool Moving { get; private set; }
        public FormationSlot Slot { get; private set; }
        CharacterController motor;
        float gravity;
        void Awake() { motor = GetComponent<CharacterController>(); Slot = GetComponent<FormationSlot>(); animator.applyRootMotion = false; }
        void OnEnable() { if (march) march.Register(this); }
        void Start() { if (march) march.Register(this); }
        void OnDisable() { if (march) march.Remove(this); }
        void Update()
        {
            if (!procession || !procession.leader) return;
            Vector3 delta = procession.PositionFor(Slot) - transform.position; delta.y = 0;
            Vector3 velocity = delta.magnitude > .25f ? delta.normalized * Mathf.Min(maxSpeed, delta.magnitude * 2) : Vector3.zero;
            // Separation remains active even at a slot when another follower is too close.
            foreach (var other in procession.followers)
            {
                if (!other || other == this || !other.isActiveAndEnabled) continue;
                Vector3 away = transform.position - other.transform.position; away.y = 0;
                if (away.sqrMagnitude > .0001f && away.magnitude < 1.25f)
                    velocity += away.normalized * (1.25f - away.magnitude) * 4f;
            }
            velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
            gravity = motor.isGrounded && gravity < 0 ? -2 : gravity - 20 * Time.deltaTime;
            Vector3 before = transform.position;
            motor.Move((velocity + Vector3.up * gravity) * Time.deltaTime);
            Vector3 moved = transform.position - before; moved.y = 0;
            Moving = moved.magnitude > .03f * Time.deltaTime;
            if (Moving) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(moved), 240 * Time.deltaTime);
        }
    }
}
