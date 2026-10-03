using UnityEngine;

namespace JerichoPrototype
{
    /// <summary>
    /// Reusable, clip-independent horn gesture for a Humanoid Animator. It only drives bones while
    /// active; the existing controller and the horn's saved hierarchy are never replaced.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PriestHornBlow : MonoBehaviour
    {
        public enum HornHand { Auto, Left, Right }
        enum Phase { Idle, Raising, Holding, Lowering }

        [Header("References")]
        [SerializeField] Animator animator;
        [SerializeField] ProcessionFollower follower;
        [Tooltip("Optional: no horn audio asset currently exists in this project.")]
        [SerializeField] AudioClip hornSound;
        [SerializeField] AudioSource audioSource;
        [SerializeField] Transform horn;
        [SerializeField] HornHand hornHand = HornHand.Auto;

        [Header("BlowHorn Timing")]
        [Min(.01f)] [SerializeField] float raiseDuration = .55f;
        [Min(0)] [SerializeField] float holdDuration = 1.4f;
        [Min(.01f)] [SerializeField] float lowerDuration = .5f;

        [Header("Humanoid Pose Offsets")]
        [Tooltip("Local Euler rotation applied to the horn-holding upper arm at full raise.")]
        [SerializeField] Vector3 upperArmRaiseEuler = new Vector3(-80f, 18f, -42f);
        [Tooltip("Local Euler rotation applied to the horn-holding lower arm at full raise.")]
        [SerializeField] Vector3 lowerArmRaiseEuler = new Vector3(20f, 0f, 75f);
        [Tooltip("Local Euler rotation applied to the horn-holding hand at full raise.")]
        [SerializeField] Vector3 handRaiseEuler = new Vector3(16f, 0f, 0f);
        [Tooltip("World-space trim applied only while raised. The saved horn local position remains unchanged and is restored after lowering.")]
        [SerializeField] Vector3 hornMouthOffset = new Vector3(6.07f, -.82f, -6.73f);
        [Tooltip("Optional local Euler trim applied to the saved horn rotation only during the raised pose.")]
        [SerializeField] Vector3 hornMouthEulerOffset = new Vector3(0f, 0f, 90f);

        Phase phase;
        float phaseTime;
        Transform upperArm;
        Transform lowerArm;
        Transform hand;
        Quaternion upperArmRest;
        Quaternion lowerArmRest;
        Quaternion handRest;
        Vector3 hornRestLocalPosition;
        Quaternion hornRestLocalRotation;
        bool initialized;

        [System.Serializable]
        public struct AuthoredTransform
        {
            public string path;
            public Vector3 position, scale;
            public Quaternion rotation;
        }
        [SerializeField] AuthoredTransform[] authoredHoldPose;
        Transform[] authoredBones;
        AuthoredTransform[] authoredRest;

        public void SetAuthoredHoldPose(AuthoredTransform[] pose)
        {
            authoredHoldPose = pose;
            authoredBones = null;
            authoredRest = null;
        }

        void CaptureAuthoredRest()
        {
            if (authoredHoldPose == null || authoredHoldPose.Length == 0) return;
            authoredBones = new Transform[authoredHoldPose.Length];
            authoredRest = new AuthoredTransform[authoredHoldPose.Length];
            for (int i = 0; i < authoredHoldPose.Length; i++)
            {
                var bone = transform.Find(authoredHoldPose[i].path);
                authoredBones[i] = bone;
                if (bone) authoredRest[i] = new AuthoredTransform { position = bone.localPosition, rotation = bone.localRotation, scale = bone.localScale };
            }
        }

        public bool IsBlowing => phase != Phase.Idle;
        public float SequenceDuration => raiseDuration + holdDuration + lowerDuration;
        public AudioClip HornSound { get => hornSound; set => hornSound = value; }

        void Reset()
        {
            animator = GetComponentInChildren<Animator>();
            follower = GetComponent<ProcessionFollower>();
            horn = FindHorn(transform);
        }

        void Awake()
        {
            if (!animator) animator = GetComponentInChildren<Animator>();
            if (!follower) follower = GetComponent<ProcessionFollower>();
            if (!horn) horn = FindHorn(transform);
            CacheHumanoidReferences();
        }

        void OnDisable()
        {
            RestoreRestPose();
            if (follower) follower.SetMovementSuppressed(false);
            phase = Phase.Idle;
        }

        /// <summary>Inspector/API trigger. A second call cleanly restarts the complete sequence.</summary>
        public void BlowHorn()
        {
            if (!CacheHumanoidReferences()) return;
            if (phase == Phase.Idle) CaptureAuthoredRest();
            phase = Phase.Raising;
            phaseTime = 0f;
            if (follower) follower.SetMovementSuppressed(true);
            if (animator) animator.SetBool("Marching", false);
            if (hornSound && audioSource) audioSource.PlayOneShot(hornSound);
        }

        void LateUpdate()
        {
            Advance(Time.deltaTime);
        }

        // Public solely for deterministic temporary editor verification.
        public void DebugAdvance(float seconds)
        {
            Advance(seconds);
        }

        void Advance(float seconds)
        {
            if (phase == Phase.Idle || !CacheHumanoidReferences()) return;
            phaseTime += Mathf.Max(0f, seconds);
            float poseWeight;
            switch (phase)
            {
                case Phase.Raising:
                    poseWeight = Mathf.Clamp01(phaseTime / raiseDuration);
                    ApplyRaisedPose(poseWeight);
                    if (poseWeight >= 1f) { phase = Phase.Holding; phaseTime = 0f; }
                    break;
                case Phase.Holding:
                    ApplyRaisedPose(1f);
                    if (phaseTime >= holdDuration) { phase = Phase.Lowering; phaseTime = 0f; }
                    break;
                default:
                    poseWeight = 1f - Mathf.Clamp01(phaseTime / lowerDuration);
                    ApplyRaisedPose(poseWeight);
                    if (poseWeight <= 0f) Complete();
                    break;
            }
        }

        void Complete()
        {
            RestoreRestPose();
            phase = Phase.Idle;
            phaseTime = 0f;
            if (follower) follower.SetMovementSuppressed(false);
        }

        bool CacheHumanoidReferences()
        {
            if (initialized) return true;
            if (!animator || !animator.avatar || !animator.avatar.isHuman)
            {
                Debug.LogWarning($"{name}: PriestHornBlow requires a valid Humanoid Animator.", this);
                return false;
            }
            if (!horn) horn = FindHorn(transform);
            var selectedHand = ResolveHand();
            hand = animator.GetBoneTransform(selectedHand == HornHand.Left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            upperArm = animator.GetBoneTransform(selectedHand == HornHand.Left ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm);
            lowerArm = animator.GetBoneTransform(selectedHand == HornHand.Left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
            if (!upperArm || !lowerArm || !hand)
            {
                Debug.LogWarning($"{name}: PriestHornBlow could not find required {selectedHand} arm Humanoid bones.", this);
                return false;
            }
            upperArmRest = upperArm.localRotation;
            lowerArmRest = lowerArm.localRotation;
            handRest = hand.localRotation;
            if (horn) { hornRestLocalPosition = horn.localPosition; hornRestLocalRotation = horn.localRotation; }
            initialized = true;
            return true;
        }

        HornHand ResolveHand()
        {
            if (hornHand != HornHand.Auto) return hornHand;
            if (horn && animator)
            {
                var left = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                for (Transform node = horn.parent; node; node = node.parent) if (node == left) return HornHand.Left;
            }
            return HornHand.Right;
        }

        void ApplyRaisedPose(float weight)
        {
            if (authoredBones != null)
            {
                for (int i = 0; i < authoredBones.Length; i++)
                {
                    var bone = authoredBones[i];
                    if (!bone) continue;
                    bone.localPosition = Vector3.Lerp(authoredRest[i].position, authoredHoldPose[i].position, weight);
                    bone.localRotation = Quaternion.Slerp(authoredRest[i].rotation, authoredHoldPose[i].rotation, weight);
                    bone.localScale = Vector3.Lerp(authoredRest[i].scale, authoredHoldPose[i].scale, weight);
                }
                return;
            }
            upperArm.localRotation = upperArmRest * Quaternion.Euler(upperArmRaiseEuler * weight);
            lowerArm.localRotation = lowerArmRest * Quaternion.Euler(lowerArmRaiseEuler * weight);
            hand.localRotation = handRest * Quaternion.Euler(handRaiseEuler * weight);
            if (horn)
            {
                // This never changes parenting, saved scale, material, or the persisted prefab transform.
                horn.localPosition = hornRestLocalPosition;
                horn.position += hornMouthOffset * weight;
                horn.localRotation = hornRestLocalRotation * Quaternion.Euler(hornMouthEulerOffset * weight);
            }
        }

        void RestoreRestPose()
        {
            if (authoredBones != null)
            {
                for (int i = 0; i < authoredBones.Length; i++)
                {
                    var bone = authoredBones[i];
                    if (!bone) continue;
                    bone.localPosition = authoredRest[i].position;
                    bone.localRotation = authoredRest[i].rotation;
                    bone.localScale = authoredRest[i].scale;
                }
                return;
            }
            if (!initialized) return;
            if (upperArm) upperArm.localRotation = upperArmRest;
            if (lowerArm) lowerArm.localRotation = lowerArmRest;
            if (hand) hand.localRotation = handRest;
            if (horn) { horn.localPosition = hornRestLocalPosition; horn.localRotation = hornRestLocalRotation; }
        }

        [ContextMenu("Horn Tests/Blow Horn")]
        void TestBlowHorn() => BlowHorn();

        [ContextMenu("Horn Tests/Repeat Blow Horn")]
        void TestRepeatedBlowHorn() { BlowHorn(); BlowHorn(); }

        [ContextMenu("Horn Tests/Log Setup")]
        void LogSetup()
        {
            bool humanoid = animator && animator.avatar && animator.avatar.isHuman;
            Debug.Log($"{name}: Humanoid={humanoid}, horn={(horn ? horn.name : "missing")}, hand={ResolveHand()}, audio={(hornSound ? hornSound.name : "unassigned")}", this);
        }

        static Transform FindHorn(Transform root)
        {
            foreach (var node in root.GetComponentsInChildren<Transform>(true))
                if (node.name.ToLowerInvariant().Contains("horn")) return node;
            return null;
        }
    }
}
