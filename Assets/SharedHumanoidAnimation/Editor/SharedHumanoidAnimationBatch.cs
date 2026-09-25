#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SharedHumanoidAnimation.Editor
{
    /// <summary>Creates only new shared assets; existing character clips and controllers are never modified.</summary>
    public static class SharedHumanoidAnimationBatch
    {
        private const string Root = "Assets/SharedHumanoidAnimation";
        private const string ControllerPath = Root + "/SharedHumanoid.controller";
        private const string MarchPath = Root + "/ExperimentalHumanoidMarch.anim";
        private const string IdlePath = "Assets/CHARACTERS/Meshy_AI_Desert_Sentinel_biped_Animation_Idle_4_withSkin.fbx";
        private const string WalkPath = "Assets/CHARACTERS/Meshy_AI_Ancient_Mediterranean_biped_Animation_Walking_withSkin.fbx";
        private const string RunPath = "Assets/CHARACTERS/Meshy_AI_Ancient_Levantine_War_biped_Animation_Running_withSkin.fbx";

        [MenuItem("Tools/Shared Humanoid Animation/Create Shared Assets")]
        public static void CreateSharedAssets()
        {
            EnsureFolder();
            CreateExperimentalMarch();
            CreateController();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Shared Humanoid Animation/Assign Controller To Selected Humanoids")]
        private static void AssignToSelectedHumanoids()
        {
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            if (controller == null)
            {
                Debug.LogError("Create the shared assets first.");
                return;
            }

            foreach (var gameObject in Selection.gameObjects)
            {
                var animator = gameObject.GetComponent<Animator>();
                if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
                {
                    Debug.LogWarning($"Skipped {gameObject.name}: it has no Animator with a valid Humanoid Avatar.", gameObject);
                    continue;
                }

                Undo.RecordObject(animator, "Assign shared humanoid controller");
                animator.runtimeAnimatorController = controller;
                if (gameObject.GetComponent<SharedHumanoidAnimationCoordinator>() == null)
                {
                    Undo.AddComponent<SharedHumanoidAnimationCoordinator>(gameObject);
                }
                EditorUtility.SetDirty(animator);
            }
        }

        private static void CreateController()
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            {
                Debug.Log($"Shared controller already exists at {ControllerPath}; it was not changed.");
                return;
            }

            var idle = LoadValidatedHumanoidClip(IdlePath, "Idle_4");
            var walk = LoadValidatedHumanoidClip(WalkPath, "Walking");
            var run = LoadValidatedHumanoidClip(RunPath, "Running");
            if (idle == null || walk == null || run == null)
            {
                Debug.LogError("Shared controller was not created because one or more validated Humanoid locomotion clips were unavailable.");
                return;
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            var state = controller.layers[0].stateMachine.AddState("Locomotion", new Vector3(300f, 100f));
            controller.layers[0].stateMachine.defaultState = state;
            var blendTree = new BlendTree { name = "Humanoid Locomotion", blendType = BlendTreeType.Simple1D, blendParameter = "Speed", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(blendTree, controller);
            blendTree.AddChild(idle, 0f);
            blendTree.AddChild(walk, 0.1f);
            blendTree.AddChild(run, 3f);
            state.motion = blendTree;
            EditorUtility.SetDirty(controller);
        }

        private static AnimationClip LoadValidatedHumanoidClip(string modelPath, string clipName)
        {
            var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null || importer.animationType != ModelImporterAnimationType.Human)
            {
                Debug.LogError($"{modelPath} is not imported as Humanoid and cannot be used by the shared controller.");
                return null;
            }

            var clip = AssetDatabase.LoadAllAssetsAtPath(modelPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(candidate => !candidate.name.EndsWith(".001", StringComparison.Ordinal) && candidate.name.Contains(clipName));
            if (clip == null) Debug.LogError($"Could not locate {clipName} in {modelPath}.");
            return clip;
        }

        private static void CreateExperimentalMarch()
        {
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(MarchPath) != null) return;

            var march = new AnimationClip { name = "ExperimentalHumanoidMarch", frameRate = 30f, legacy = false, wrapMode = WrapMode.Loop };
            // Humanoid muscle curves are intentionally used instead of Transform curves, so the clip can retarget through a Humanoid Avatar.
            AddMuscleCycle(march, "Left Upper Leg Front-Back", 26f, 0f);
            AddMuscleCycle(march, "Right Upper Leg Front-Back", 26f, 0.5f);
            AddMuscleCycle(march, "Left Leg Close-Open", 5f, 0.5f);
            AddMuscleCycle(march, "Right Leg Close-Open", 5f, 0f);
            AddMuscleCycle(march, "Left Lower Leg Stretch", 16f, 0.5f);
            AddMuscleCycle(march, "Right Lower Leg Stretch", 16f, 0f);
            AddMuscleCycle(march, "Left Arm Down-Up", 18f, 0.5f);
            AddMuscleCycle(march, "Right Arm Down-Up", 18f, 0f);
            AnimationUtility.SetAnimationClipSettings(march, new AnimationClipSettings { loopTime = true });
            AssetDatabase.CreateAsset(march, MarchPath);
        }

        private static void AddMuscleCycle(AnimationClip clip, string muscleName, float amplitude, float phase)
        {
            int muscleIndex = Array.IndexOf(HumanTrait.MuscleName, muscleName);
            if (muscleIndex < 0)
            {
                Debug.LogWarning($"The Humanoid muscle '{muscleName}' was not found; it was omitted from the experimental March clip.");
                return;
            }

            var curve = new AnimationCurve(
                new Keyframe(0f, phase == 0f ? amplitude : -amplitude),
                new Keyframe(0.25f, phase == 0f ? 0f : 0f),
                new Keyframe(0.5f, phase == 0f ? -amplitude : amplitude),
                new Keyframe(0.75f, 0f),
                new Keyframe(1f, phase == 0f ? amplitude : -amplitude));
            clip.SetCurve(string.Empty, typeof(Animator), HumanTrait.MuscleName[muscleIndex], curve);
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder("Assets", "SharedHumanoidAnimation");
        }
    }
}
#endif