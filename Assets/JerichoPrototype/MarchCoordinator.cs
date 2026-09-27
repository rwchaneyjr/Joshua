using System.Collections.Generic;
using UnityEngine;
namespace JerichoPrototype
{
    public sealed class MarchCoordinator : MonoBehaviour
    {
        [Min(.1f)] public float cyclesPerSecond = 1;
        public bool marching = true;
        readonly HashSet<ProcessionFollower> members = new HashSet<ProcessionFollower>();
        double clock;
        public void Register(ProcessionFollower follower) { if (follower) members.Add(follower); }
        public void Remove(ProcessionFollower follower) { members.Remove(follower); }
        void Update()
        {
            if (marching) clock += Time.deltaTime * cyclesPerSecond;
            foreach (var member in members)
            {
                if (!member || !member.isActiveAndEnabled || !member.animator || !member.animator.isActiveAndEnabled) continue;
                // Ceremonial actions own the pose until they explicitly release this follower.
                member.animator.SetBool("Marching", marching && member.Moving && !member.MovementSuppressed);
                member.animator.SetFloat("MarchPhase", Mathf.Repeat((float)(clock % 1.0) + member.Slot.phaseGroup * .5f, 1f));
            }
        }
    }
}
