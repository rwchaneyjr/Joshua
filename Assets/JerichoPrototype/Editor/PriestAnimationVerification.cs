using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace JerichoPrototype.Editor
{
    public static class PriestAnimationVerification
    {
        const string Request = ".utmp/priest-animation.request";
        [InitializeOnLoadMethod]
        static void CheckRequest()
        {
            if (File.Exists(Request)) EditorApplication.delayCall += () =>
            {
                File.Delete(Request);
                try { Verify(); }
                catch (Exception ex) { Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/priest-animation-verification.txt", "FAIL: " + ex); Debug.LogException(ex); }
            };
        }
        [MenuItem("Tools/Jericho Prototype/Verify Priest Bone Animation")]
        public static void Verify()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before this isolated animation check.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JerichoPrototype/Priest.prefab");
            var copy = UnityEngine.Object.Instantiate(prefab);
            copy.hideFlags = HideFlags.HideAndDontSave;
            string result = "";
            try
            {
                var animator = copy.GetComponentInChildren<Animator>();
                if (!animator || !animator.avatar || !animator.avatar.isHuman || !animator.avatar.isValid) throw new Exception("Invalid Priest Avatar");
                var controller = animator.runtimeAnimatorController as AnimatorController;
                foreach (string name in new[] { "IDLE", "WALK" })
                {
                    var clip = controller.layers[0].stateMachine.states.First(s=>s.state.name == name).state.motion as AnimationClip;
                    if (!clip || !clip.humanMotion || !clip.isLooping || clip.name.StartsWith("__preview__") || clip.name.EndsWith(".001")) throw new Exception("Invalid runtime " + name + " clip");
                    result += name + ": full looping Humanoid clip, " + clip.length + " seconds\n";
                }
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind(); animator.Play("IDLE", 0, 0); animator.Update(.1f);
                var bones = new[] { HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftUpperLeg }.Select(animator.GetBoneTransform).ToArray();
                var idleStart = bones.Select(b=>b.localRotation).ToArray();
                float idleMotion = 0;
                for (int frame=0; frame<180; frame++)
                {
                    animator.Update(1f/30f);
                    for(int n=0;n<bones.Length;n++) idleMotion=Mathf.Max(idleMotion,Quaternion.Angle(idleStart[n],bones[n].localRotation));
                }
                if (idleMotion < .01f) throw new Exception("Idle bones did not animate");
                result += "Idle bone motion: " + idleMotion + " degrees\n";
                animator.SetBool("Marching", true); animator.Update(.3f);
                if (!animator.GetCurrentAnimatorStateInfo(0).IsName("WALK")) throw new Exception("Did not transition to WALK");
                var leg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
                animator.SetFloat("MarchPhase",0); animator.Update(.01f); var start = leg.localRotation;
                float walkMotion=0;
                for(int frame=0;frame<180;frame++)
                {
                    animator.SetFloat("MarchPhase",(frame%30)/30f); animator.Update(1f/30f);
                    walkMotion=Mathf.Max(walkMotion,Quaternion.Angle(start,leg.localRotation));
                }
                if(walkMotion<10) throw new Exception("Walk leg did not sustain motion");
                result += "Six marching cycles, leg motion: " + walkMotion + " degrees\n";
                animator.SetBool("Marching",false); animator.Update(.3f); animator.Update(.1f);
                if(!animator.GetCurrentAnimatorStateInfo(0).IsName("IDLE")) throw new Exception("Did not return to IDLE");
                result += "Walk-to-Idle transition: PASS\n";
            }
            finally { UnityEngine.Object.DestroyImmediate(copy); }
            Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/priest-animation-verification.txt","PASS\n"+result);
            Debug.Log("PRIEST ANIMATION PASS\n"+result);
        }
    }
}
