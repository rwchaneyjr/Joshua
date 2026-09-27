using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JerichoPrototype.Editor
{
    /// <summary>Creates the saved priest prefab and inserts the ceremonial seven-priest formation into the existing scene.</summary>
    public static class PriestFormationInstaller
    {
        const string Root = "Assets/JerichoPrototype/";
        const string PriestPrefabPath = Root + "Priest.prefab";
        const string PriestModelPath = "Assets/CHARACTERS/Meshy_AI_Desert_Sentinel_biped_Animation_Idle_4_withSkin.fbx";
        const string HornModelPath = "Assets/CHARACTERS/Meshy_AI_Ancient_Ceremonial_Ho_0924161751_texture.fbx";
        const string PriestControllerPath = "Assets/CHARACTERS/PRIEST.controller";

        [MenuItem("Tools/Jericho Prototype/Install Seven Horn Priests")]
        public static void Install()
        {
            ConfigurePriestController();
            var scene = EditorSceneManager.OpenScene(PrototypeBuilder.ScenePath, OpenSceneMode.Single);
            var manager = UnityEngine.Object.FindObjectOfType<ProcessionManager>();
            var coordinator = UnityEngine.Object.FindObjectOfType<MarchCoordinator>();
            if (!manager || !manager.leader || !coordinator)
                throw new InvalidOperationException("Jericho scene is missing its procession manager, leader, or march coordinator.");

            var priestPrefab = CreatePriestPrefab();
            var soldiers = manager.followers.Where(f => f && f.name.StartsWith("Soldier ")).Take(10).ToArray();
            if (soldiers.Length != 10) throw new InvalidOperationException("Expected the ten saved soldier instances before adding priests.");

            foreach (var oldPriest in UnityEngine.Object.FindObjectsOfType<ProcessionFollower>()
                         .Where(f => f.name.StartsWith("Priest ")).ToArray())
                UnityEngine.Object.DestroyImmediate(oldPriest.gameObject);

            var followers = new ProcessionFollower[17];
            for (var index = 0; index < 7; index++)
            {
                var priest = (GameObject)PrefabUtility.InstantiatePrefab(priestPrefab);
                priest.name = "Priest " + (index + 1);
                ConfigureFollower(priest, manager, coordinator, index, 3f, 2.6f);
                followers[index] = priest.GetComponent<ProcessionFollower>();
            }

            for (var index = 0; index < soldiers.Length; index++)
            {
                // Soldiers remain saved prefab instances, now physically behind the priest rows.
                ConfigureFollower(soldiers[index].gameObject, manager, coordinator, index, 11f, 2.6f);
                followers[index + 7] = soldiers[index];
            }

            manager.followers = followers;
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            VerifySavedScene();
            Debug.Log("JERICHO PRIESTS PASS: saved seven horn priests before ten soldiers in a 17-member procession.");
        }

        static GameObject CreatePriestPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PriestPrefabPath);
            if (existing) AssetDatabase.DeleteAsset(PriestPrefabPath);

            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PriestModelPath);
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PriestControllerPath);
            var hornAsset = AssetDatabase.LoadAssetAtPath<GameObject>(HornModelPath);
            if (!modelAsset || !controller || !hornAsset) throw new InvalidOperationException("Required priest model, controller, or ceremonial horn asset is missing.");

            var priest = new GameObject("Priest");
            priest.layer = 2;
            var model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
            model.transform.SetParent(priest.transform, false);
            foreach (var transform in model.GetComponentsInChildren<Transform>()) transform.gameObject.layer = 2;
            var animator = model.GetComponent<Animator>();
            if (!animator || !animator.avatar || !animator.avatar.isValid || !animator.avatar.isHuman)
                throw new InvalidOperationException("Priest model must expose a valid Humanoid avatar.");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var renderers = model.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            var characterScale = 1.8f / bounds.size.y;
            model.transform.localScale *= characterScale;
            model.transform.localPosition = Vector3.up * (-bounds.min.y * characterScale);

            var motor = priest.AddComponent<CharacterController>();
            motor.height = 1.8f; motor.radius = .38f; motor.center = Vector3.up * .9f; motor.stepOffset = .25f;
            priest.AddComponent<FormationSlot>();
            var follower = priest.AddComponent<ProcessionFollower>();
            follower.animator = animator;

            // Parent to the hand so the horn follows both priest locomotion and the shared cadence.
            var hand = animator.GetBoneTransform(HumanBodyBones.RightHand) ?? model.transform;
            var horn = (GameObject)PrefabUtility.InstantiatePrefab(hornAsset);
            horn.name = "Ceremonial Horn";
            horn.transform.SetParent(hand, false);
            var hornRenderers = horn.GetComponentsInChildren<Renderer>();
            var hornBounds = hornRenderers[0].bounds;
            foreach (var renderer in hornRenderers.Skip(1)) hornBounds.Encapsulate(renderer.bounds);
            var hornScale = .65f / Mathf.Max(hornBounds.size.x, hornBounds.size.y, hornBounds.size.z);
            horn.transform.localScale *= hornScale;
            horn.transform.localPosition = new Vector3(.06f, .02f, .13f);
            horn.transform.localRotation = Quaternion.Euler(80f, 90f, 0f);

            var prefab = PrefabUtility.SaveAsPrefabAsset(priest, PriestPrefabPath);
            UnityEngine.Object.DestroyImmediate(priest);
            return prefab;
        }

        static void ConfigureFollower(GameObject character, ProcessionManager manager, MarchCoordinator coordinator, int index, float firstRowDistance, float rowSpacing)
        {
            var row = index / 3;
            var column = index % 3;
            var slot = character.GetComponent<FormationSlot>();
            var follower = character.GetComponent<ProcessionFollower>();
            slot.localOffset = new Vector3((column - 1) * 2.2f, 0f, -firstRowDistance - row * rowSpacing);
            // The single seventh/tenth character is centered, preserving practical row spacing.
            if ((index == 6) || (index == 9)) slot.localOffset = new Vector3(0f, 0f, -firstRowDistance - row * rowSpacing);
            slot.phaseGroup = row % 2;
            follower.procession = manager;
            follower.march = coordinator;
            character.transform.position = manager.leader.position + slot.localOffset;
            PrefabUtility.RecordPrefabInstancePropertyModifications(slot);
            PrefabUtility.RecordPrefabInstancePropertyModifications(follower);
            PrefabUtility.RecordPrefabInstancePropertyModifications(character.transform);
        }

        static void ConfigurePriestController()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PriestControllerPath);
            if (!controller) throw new InvalidOperationException("Priest controller is missing.");
            if (!controller.parameters.Any(p => p.name == "Marching")) controller.AddParameter("Marching", AnimatorControllerParameterType.Bool);
            if (!controller.parameters.Any(p => p.name == "MarchPhase")) controller.AddParameter("MarchPhase", AnimatorControllerParameterType.Float);
            var machine = controller.layers[0].stateMachine;
            var idle = machine.states.First(s => s.state.name == "IDLE").state;
            var walk = machine.states.First(s => s.state.name == "WALK").state;
            machine.defaultState = idle;
            walk.timeParameter = "MarchPhase";
            walk.timeParameterActive = true;
            foreach (var transition in idle.transitions.ToArray()) idle.RemoveTransition(transition);
            foreach (var transition in walk.transitions.ToArray()) walk.RemoveTransition(transition);
            var begin = idle.AddTransition(walk); begin.hasExitTime = false; begin.duration = .15f; begin.AddCondition(AnimatorConditionMode.If, 0, "Marching");
            var stop = walk.AddTransition(idle); stop.hasExitTime = false; stop.duration = .15f; stop.AddCondition(AnimatorConditionMode.IfNot, 0, "Marching");
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Jericho Prototype/Verify Seven Horn Priests")]
        public static void VerifySavedScene()
        {
            var scene = EditorSceneManager.OpenScene(PrototypeBuilder.ScenePath, OpenSceneMode.Single);
            var manager = UnityEngine.Object.FindObjectOfType<ProcessionManager>();
            if (!manager || manager.followers == null || manager.followers.Length != 17) throw new Exception("Expected 17 procession followers.");
            for (var index = 0; index < 7; index++)
            {
                var priest = manager.followers[index];
                if (!priest || !priest.name.StartsWith("Priest ") || !priest.march || priest.procession != manager || !priest.animator || !priest.animator.runtimeAnimatorController.name.Equals("PRIEST"))
                    throw new Exception("Priest follower wiring failed at index " + index + ".");
                if (!priest.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Ceremonial Horn")) throw new Exception("Priest horn is missing at index " + index + ".");
                if (index >= 3 && index % 3 == 0 && priest.GetComponent<FormationSlot>().localOffset.z >= manager.followers[index - 3].GetComponent<FormationSlot>().localOffset.z) throw new Exception("Priest rows are not ordered.");
            }
            for (var index = 7; index < 17; index++)
                if (!manager.followers[index] || !manager.followers[index].name.StartsWith("Soldier ") || manager.followers[index].GetComponent<FormationSlot>().localOffset.z >= manager.followers[6].GetComponent<FormationSlot>().localOffset.z)
                    throw new Exception("Soldier order or spacing failed at index " + index + ".");
            if (!scene.IsValid()) throw new Exception("Jericho scene did not open.");
            Debug.Log("JERICHO PRIEST SCENE VERIFY PASS: 7 saved horn priests, 10 soldiers, formation and march wiring preserved.");
        }
    }
}