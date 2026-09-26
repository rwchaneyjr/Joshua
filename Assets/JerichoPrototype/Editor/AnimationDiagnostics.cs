using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace JerichoPrototype.Editor
{
    public static class AnimationDiagnostics
    {
        public static void Audit()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/SharedHumanoidAnimation/JoshuaAnimationTest.controller");
            foreach (var child in controller.layers[0].stateMachine.states)
            {
                var clip = child.state.motion as AnimationClip;
                if (clip) Debug.Log($"CLIP {child.state.name}: {clip.name} length={clip.length} human={clip.humanMotion} loop={clip.isLooping} curves={AnimationUtility.GetCurveBindings(clip).Length}");
            }
            foreach(var path in new[] { "Assets/CHARACTERS/Meshy_AI_Ancient_Mediterranean_biped_Animation_Walking_withSkin.fbx", "Assets/CHARACTERS/Meshy_AI_Desert_Sentinel_biped_Animation_Idle_4_withSkin.fbx", "Assets/CHARACTERS/Meshy_AI_Ancient_Levantine_War_biped_Animation_Running_withSkin.fbx" })
                foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>())
                    Debug.Log($"AVAILABLE CLIP {path}: {clip.name} length={clip.length} human={clip.humanMotion} loop={clip.isLooping}");
            foreach (var path in new[] { "Assets/CHARACTERS/Meshy_AI_Ancient_Mediterranean_biped_Animation_Walking_withSkin.fbx", "Assets/CHARACTERS/Meshy_AI_Ancient_Levantine_War_biped_Animation_Running_withSkin.fbx", "Assets/JerichoPrototype/Joshua.prefab", "Assets/JerichoPrototype/Soldier.prefab" })
            {
                var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                try
                {
                    var animator = root.GetComponentInChildren<Animator>();
                    animator.runtimeAnimatorController = controller; animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    animator.Rebind(); animator.SetInteger("TestState", 2); animator.Play("Walk",0,0); animator.Update(.1f);
                    var leg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
                    var foot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                    var arm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                    var q = leg.localRotation; var a = arm.localRotation;
                    var skin = root.GetComponentInChildren<SkinnedMeshRenderer>();
                    var mesh = new Mesh(); skin.BakeMesh(mesh); var before = mesh.vertices;
                    animator.Update(.25f); skin.BakeMesh(mesh); var after = mesh.vertices;
                    float max = 0; for(int i=0;i<before.Length;i++) max = Mathf.Max(max, Vector3.Distance(before[i], after[i]));
                    Debug.Log($"ANIM AUDIT {path}: legAngle={Quaternion.Angle(q,leg.localRotation)} armAngle={Quaternion.Angle(a,arm.localRotation)} meshDelta={max} footInSkin={skin.bones.Contains(foot)} stateTime={animator.GetCurrentAnimatorStateInfo(0).normalizedTime} speed={animator.speed} scale={animator.humanScale} skinRoot={skin.rootBone.name}");
                    Object.DestroyImmediate(mesh);
                }
                finally { Object.DestroyImmediate(root); }
            }
        }
    }
}
