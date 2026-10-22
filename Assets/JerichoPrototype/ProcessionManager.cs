using UnityEngine;
namespace JerichoPrototype
{
    public sealed class ProcessionManager : MonoBehaviour
    {
        public Transform leader;
        public ProcessionFollower[] followers;
        public float turnSpeed = 65f;
        public Quaternion Heading { get; private set; } = Quaternion.identity;
        void Awake() { Heading = leader.rotation; }
        public void RestoreHeading(Quaternion heading) { Heading=heading; }
        void Update() { if (leader) Heading = Quaternion.RotateTowards(Heading, leader.rotation, turnSpeed * Time.deltaTime); }
        public Vector3 PositionFor(FormationSlot slot) => leader.position + Heading * slot.localOffset;
    }
}
