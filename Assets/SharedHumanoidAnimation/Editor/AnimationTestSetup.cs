using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SharedHumanoidAnimation.Editor
{
    /// <summary>Builds isolated test bindings for all SampleScene characters. Carry is exclusive to Priest.</summary>
    public static class AnimationTestSetup
    {
        private const string JoshuaControllerPath = "Assets/SharedHumanoidAnimation/JoshuaAnimationTest.controller";
        private const string WarriorControllerPath = "Assets/SharedHumanoidAnimation/WarriorAnimationTest.controller";
        private const string PriestControllerPath = "Assets/SharedHumanoidAnimation/AnimationTest.controller";
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string JoshuaName = "Meshy_AI_Ancient_Mediterranean_biped_Animation_Walking_withSkin";
        private const string WarriorName = "Meshy_AI_Ancient_Levantine_War_biped_Animation_Running_withSkin";
        private const string PriestName = "Meshy_AI_Desert_Sentinel_biped_Animation_Idle_4_withSkin";
        private const string KickSourcePath = "Assets/CHARACTERS/Meshy_AI_Ancient_Levantine_War_biped_Animation_High_Kick_withSkin.fbx";
        private const string CarrySourcePath = "Assets/CHARACTERS/Meshy_AI_Desert_Sentinel_biped_Animation_Carry_Heavy_Cannon_Forward_withSkin.fbx";

        [MenuItem("Tools/Shared Humanoid Animation/Setup Animation Test Scene")]
        public static void SetupAnimationTestScene()
        {
            AnimationClip idle = LoadHumanoidClip("Assets/CHARACTERS/Meshy_AI_Desert_Sentinel_biped_Animation_Idle_4_withSkin.fbx", "Idle_4");
            AnimationClip walk = LoadHumanoidClip("Assets/CHARACTERS/Meshy_AI_Ancient_Mediterranean_biped_Animation_Walking_withSkin.fbx", "Walking");
            AnimationClip run = LoadHumanoidClip("Assets/CHARACTERS/Meshy_AI_Ancient_Levantine_War_biped_Animation_Running_withSkin.fbx", "Running");
            AnimationClip kick = LoadHumanoidClip(KickSourcePath, "High_Kick");
            AnimationClip carry = LoadHumanoidClip(CarrySourcePath, "Carry_Heavy_Cannon_Forward");

            AnimatorController joshuaController = BuildController(JoshuaControllerPath, idle, walk, run, kick, null);
            AnimatorController warriorController = BuildController(WarriorControllerPath, idle, walk, run, kick, null);
            AnimatorController priestController = BuildController(PriestControllerPath, idle, walk, run, kick, carry);
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            BindCharacter(scene, JoshuaName, "Joshua", joshuaController, false);
            BindCharacter(scene, WarriorName, "Warrior", warriorController, false);
            BindCharacter(scene, PriestName, "Priest", priestController, true);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Animation test setup complete: selector wires Joshua/Warrior 1-4 and Priest 1-5 with Carry.");
        }

        [MenuItem("Tools/Shared Humanoid Animation/Verify Animation Test Scene")]
        public static void VerifyAnimationTestScene()
        {
            AnimatorController joshuaController = AssetDatabase.LoadAssetAtPath<AnimatorController>(JoshuaControllerPath);
            AnimatorController warriorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(WarriorControllerPath);
            AnimatorController priestController = AssetDatabase.LoadAssetAtPath<AnimatorController>(PriestControllerPath);
            VerifyController(joshuaController, false, JoshuaControllerPath);
            VerifyController(warriorController, false, WarriorControllerPath);
            VerifyController(priestController, true, PriestControllerPath);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            VerifyCharacter(scene, JoshuaName, "Joshua", joshuaController, false);
            VerifyCharacter(scene, WarriorName, "Warrior", warriorController, false);
            VerifyCharacter(scene, PriestName, "Priest", priestController, true);
            Debug.Log("Animation test scene verification passed: all three characters have isolated Animator/controller references; only Priest supports Carry.");
        }

        private static void BindCharacter(Scene scene, string characterName, string characterLabel, AnimatorController controller, bool carryAvailable)
        {
            GameObject target = scene.GetRootGameObjects().FirstOrDefault(root => root.name == characterName);
            Animator animator = target != null ? target.GetComponent<Animator>() : null;
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
                throw new InvalidOperationException("The " + characterLabel + " test character must have a valid Humanoid Animator Avatar.");

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            AnimationTestControls controls = target.GetComponent<AnimationTestControls>() ?? target.AddComponent<AnimationTestControls>();
            SerializedObject serializedControls = new SerializedObject(controls);
            serializedControls.FindProperty("targetAnimator").objectReferenceValue = animator;
            serializedControls.FindProperty("testController").objectReferenceValue = controller;
            serializedControls.FindProperty("characterLabel").stringValue = characterLabel;
            serializedControls.FindProperty("testMode").boolValue = true;
            serializedControls.FindProperty("showOnScreenControls").boolValue = true;
            serializedControls.FindProperty("carryAvailable").boolValue = carryAvailable;
            serializedControls.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(controls);
        }

        private static void VerifyCharacter(Scene scene, string characterName, string characterLabel, AnimatorController controller, bool carryAvailable)
        {
            GameObject target = scene.GetRootGameObjects().FirstOrDefault(root => root.name == characterName);
            Animator animator = target != null ? target.GetComponent<Animator>() : null;
            AnimationTestControls[] controls = target != null ? target.GetComponents<AnimationTestControls>() : Array.Empty<AnimationTestControls>();
            if (animator == null || controls.Length != 1 || animator.runtimeAnimatorController != controller || animator.applyRootMotion || animator.avatar == null || !animator.avatar.isHuman)
                throw new InvalidOperationException(characterLabel + " bindings are incomplete, duplicated, or not in-place.");

            SerializedObject serializedControls = new SerializedObject(controls[0]);
            if (serializedControls.FindProperty("targetAnimator").objectReferenceValue != animator ||
                serializedControls.FindProperty("testController").objectReferenceValue != controller ||
                serializedControls.FindProperty("characterLabel").stringValue != characterLabel ||
                serializedControls.FindProperty("carryAvailable").boolValue != carryAvailable)
                throw new InvalidOperationException(characterLabel + " AnimationTestControls references or Carry availability are incorrect.");
        }

        private static AnimatorController BuildController(string path, AnimationClip idle, AnimationClip walk, AnimationClip run, AnimationClip kick, AnimationClip carry)
        {
            AssetDatabase.DeleteAsset(path);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("TestState", AnimatorControllerParameterType.Int);
            controller.parameters.First(parameter => parameter.name == "TestState").defaultInt = 1;
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AddState(machine, "Idle", idle, 1, new Vector3(300f, 40f));
            AddState(machine, "Walk", walk, 2, new Vector3(300f, 110f));
            AddState(machine, "Run", run, 3, new Vector3(300f, 180f));
            AddState(machine, "Kick", kick, 4, new Vector3(300f, 250f));
            if (carry != null) AddState(machine, "Carry", carry, 5, new Vector3(300f, 320f));
            machine.defaultState = machine.states.Select(child => child.state).First(state => state.name == "Idle");
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void AddState(AnimatorStateMachine machine, string name, AnimationClip clip, int testState, Vector3 position)
        {
            AnimatorState state = machine.AddState(name, position);
            state.motion = clip;
            AnimatorStateTransition transition = machine.AddAnyStateTransition(state);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.08f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.Equals, testState, "TestState");
        }

        private static void VerifyController(AnimatorController controller, bool carryExpected, string path)
        {
            if (controller == null || controller.parameters.Count(parameter => parameter.name == "TestState") != 1 || controller.parameters.Count(parameter => parameter.name == "Speed") != 1)
                throw new InvalidOperationException(path + " is missing its required parameters.");
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            foreach (string name in new[] { "Idle", "Walk", "Run", "Kick" }) VerifyState(machine, name, true);
            VerifyState(machine, "Carry", carryExpected);
            VerifyActionState(machine, "Kick", KickSourcePath);
            if (carryExpected) VerifyActionState(machine, "Carry", CarrySourcePath);
        }

        private static void VerifyState(AnimatorStateMachine machine, string name, bool expected)
        {
            bool found = machine.states.Any(child => child.state.name == name && child.state.motion != null);
            if (found != expected) throw new InvalidOperationException("Controller state expectation failed for " + name + ".");
        }

        private static void VerifyActionState(AnimatorStateMachine machine, string stateName, string expectedAssetPath)
        {
            ModelImporter importer = AssetImporter.GetAtPath(expectedAssetPath) as ModelImporter;
            AnimatorState state = machine.states.Select(child => child.state).First(candidate => candidate.name == stateName);
            if (importer == null || importer.animationType != ModelImporterAnimationType.Human || AssetDatabase.GetAssetPath(state.motion) != expectedAssetPath)
                throw new InvalidOperationException(stateName + " is not assigned to its full compatible Humanoid source clip.");
        }

        private static AnimationClip LoadHumanoidClip(string assetPath, string requiredNamePart)
        {
            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null || importer.animationType != ModelImporterAnimationType.Human)
                throw new InvalidOperationException(assetPath + " is not a validated Humanoid animation source.");
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<AnimationClip>()
                .Where(candidate => !candidate.name.EndsWith(".001", StringComparison.Ordinal))
                .FirstOrDefault(candidate => candidate.name.IndexOf(requiredNamePart, StringComparison.OrdinalIgnoreCase) >= 0);
            if (clip == null) throw new InvalidOperationException("No full embedded take matching '" + requiredNamePart + "' was found in " + assetPath);
            return clip;
        }
    }
}