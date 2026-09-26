using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JerichoPrototype.Editor
{
    public static class RuntimeAnimationRepair
    {
        const string RuntimePath = "Assets/JerichoPrototype/JoshuaRuntime.controller";
        [MenuItem("Tools/Jericho Prototype/Repair Runtime Clip Bindings")]
        public static void Repair()
        {
            if (!AssetDatabase.LoadAssetAtPath<AnimatorController>(RuntimePath))
                AssetDatabase.CopyAsset("Assets/SharedHumanoidAnimation/JoshuaAnimationTest.controller", RuntimePath);
            var player = AssetDatabase.LoadAssetAtPath<AnimatorController>(RuntimePath);
            Fix(player);
            Fix(AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/JerichoPrototype/SoldierMarch.controller"));
            var contents = PrefabUtility.LoadPrefabContents("Assets/JerichoPrototype/Joshua.prefab");
            try { contents.GetComponentInChildren<Animator>().runtimeAnimatorController = player; PrefabUtility.SaveAsPrefabAsset(contents, "Assets/JerichoPrototype/Joshua.prefab"); }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            var scene = EditorSceneManager.OpenScene(WindowsPrototypeBuild.Scene);
            foreach (var root in scene.GetRootGameObjects().Where(r=>r.GetComponent<JoshuaController>()))
                root.GetComponentInChildren<Animator>().runtimeAnimatorController = player;
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            // Re-measure visual height against the actual runtime Idle, not a preview take.
            PrototypeModelCleanup.AlignAndBuild();
        }
        static void Fix(AnimatorController controller)
        {
            foreach (var child in controller.layers[0].stateMachine.states)
            {
                var old = child.state.motion as AnimationClip;
                if (!old) continue;
                var clips = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(old)).OfType<AnimationClip>();
                var clip = clips.Where(c=>!c.name.StartsWith("__preview__", StringComparison.Ordinal) && !c.name.EndsWith(".001", StringComparison.Ordinal) && c.humanMotion)
                    .OrderByDescending(c=>c.length).FirstOrDefault();
                if (!clip || ((child.state.name == "Idle" || child.state.name == "Walk" || child.state.name == "Run") && !clip.isLooping))
                    throw new InvalidOperationException("Missing full looping runtime clip for " + child.state.name);
                child.state.motion = clip; EditorUtility.SetDirty(child.state);
                Debug.Log($"RUNTIME CLIP {controller.name}/{child.state.name}: {clip.name}, {clip.length:F3}s, loop={clip.isLooping}");
            }
            EditorUtility.SetDirty(controller);
        }
    }
}
