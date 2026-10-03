using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JerichoPrototype.Editor
{
    public static class PrototypeBuilder
    {
        const string Root = "Assets/JerichoPrototype/";
        public const string ScenePath = "Assets/Scenes/JerichoPrototype.unity";
        const string JoshuaModel = "Assets/CHARACTERS/Meshy_AI_Ancient_Mediterranean_biped_Animation_Walking_withSkin.fbx";
        const string SoldierModel = "Assets/CHARACTERS/Meshy_AI_Ancient_Levantine_War_biped_Animation_Running_withSkin.fbx";

        [MenuItem("Tools/Jericho Prototype/Create Prototype (new assets only)")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)) throw new InvalidOperationException("Prototype already exists; open its scene. Builder will not overwrite it.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var original = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/SharedHumanoidAnimation/JoshuaAnimationTest.controller");
            var states = original.layers[0].stateMachine.states;
            AnimationClip idle = (AnimationClip)states.First(s => s.state.name == "Idle").state.motion;
            AnimationClip walk = (AnimationClip)states.First(s => s.state.name == "Walk").state.motion;
            AnimationClip run = (AnimationClip)states.First(s => s.state.name == "Run").state.motion;
            foreach (var clip in new[] {idle, walk, run})
                if (!clip || !clip.humanMotion || clip.name.EndsWith(".001")) throw new InvalidOperationException("Expected full Humanoid clip.");
            var controller = AnimatorController.CreateAnimatorControllerAtPath(Root + "SoldierMarch.controller");
            controller.AddParameter("Marching", AnimatorControllerParameterType.Bool);
            controller.AddParameter("MarchPhase", AnimatorControllerParameterType.Float);
            var machine = controller.layers[0].stateMachine;
            var rest = machine.AddState("Idle"); rest.motion = idle; machine.defaultState = rest;
            var march = machine.AddState("Walk"); march.motion = walk;
            march.timeParameter = "MarchPhase"; march.timeParameterActive = true;
            var enter = rest.AddTransition(march); enter.hasExitTime = false; enter.duration = .15f; enter.AddCondition(AnimatorConditionMode.If, 0, "Marching");
            var stop = march.AddTransition(rest); stop.hasExitTime = false; stop.duration = .15f; stop.AddCondition(AnimatorConditionMode.IfNot, 0, "Marching");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var groundMaterial = Material("Ground", new Color(.37f, .35f, .25f));
            var wallMaterial = Material("Walls", new Color(.67f, .53f, .35f));
            Cube("Ground", new Vector3(0, -.5f, 0), new Vector3(100, 1, 100), groundMaterial);
            for (int i = -3; i <= 3; i++)
            {
                Cube("North wall", new Vector3(i * 4, 2, 14), new Vector3(4, 4, 1), wallMaterial);
                Cube("South wall", new Vector3(i * 4, 2, -14), new Vector3(4, 4, 1), wallMaterial);
                Cube("East wall", new Vector3(14, 2, i * 4), new Vector3(1, 4, 4), wallMaterial);
                Cube("West wall", new Vector3(-14, 2, i * 4), new Vector3(1, 4, 4), wallMaterial);
            }
            var light = new GameObject("Sun").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.2f; light.transform.rotation = Quaternion.Euler(50, -30, 0);
            RenderSettings.ambientLight = new Color(.55f, .55f, .55f);
            var player = Character("Joshua", JoshuaModel, original);
            var movement = player.AddComponent<JoshuaController>(); movement.animator = player.GetComponentInChildren<Animator>();
            PrefabUtility.SaveAsPrefabAsset(player, Root + "Joshua.prefab");
            player.transform.position = new Vector3(0, .1f, -25);
            var camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera"; camera.farClipPlane = 180;
            camera.gameObject.AddComponent<AudioListener>(); camera.transform.position = player.transform.position + new Vector3(0, 5, -8);
            var follow = camera.gameObject.AddComponent<ThirdPersonCamera>(); follow.target = player.transform;
            movement.cameraTransform = camera.transform;
            var manager = new GameObject("Procession").AddComponent<ProcessionManager>(); manager.leader = player.transform;
            var coordinator = manager.gameObject.AddComponent<MarchCoordinator>();
            var soldier = Character("Soldier", SoldierModel, controller);
            soldier.AddComponent<FormationSlot>();
            var follower = soldier.AddComponent<ProcessionFollower>(); follower.animator = soldier.GetComponentInChildren<Animator>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(soldier, Root + "Soldier.prefab");
            UnityEngine.Object.DestroyImmediate(soldier);
            manager.followers = new ProcessionFollower[10];
            for (int i = 0; i < 10; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = "Soldier " + (i + 1);
                int row = i / 3;
                var slot = instance.GetComponent<FormationSlot>(); slot.localOffset = new Vector3(i == 9 ? 0 : (i % 3 - 1) * 2.2f, 0, -3 - row * 2.4f); slot.phaseGroup = row % 2;
                var npc = instance.GetComponent<ProcessionFollower>(); npc.procession = manager; npc.march = coordinator;
                instance.transform.position = player.transform.position + slot.localOffset;
                manager.followers[i] = npc;
                PrefabUtility.RecordPrefabInstancePropertyModifications(slot); PrefabUtility.RecordPrefabInstancePropertyModifications(npc);
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            }
            var mission = new GameObject("Mission - Joshua 6").AddComponent<JerichoMission>(); mission.player = player.transform;
            Vector3[] points = { new Vector3(25,0,-25), new Vector3(25,0,0), new Vector3(25,0,25), new Vector3(-25,0,25), new Vector3(-25,0,-25), new Vector3(0,0,-25) };
            mission.checkpoints = new Transform[6];
            for (int i = 0; i < points.Length; i++)
            {
                var checkpoint = new GameObject("Checkpoint " + (i + 1)); checkpoint.transform.position = points[i]; mission.checkpoints[i] = checkpoint.transform;
                var label = new GameObject("Route sign " + (i + 1)).AddComponent<TextMesh>(); label.text = (i + 1).ToString(); label.color = Color.black; label.fontSize = 64; label.characterSize = .3f; label.anchor = TextAnchor.MiddleCenter;
                label.transform.position = points[i] + Vector3.up * .03f; label.transform.rotation = Quaternion.Euler(90,0,0);
            }
            EditorSceneManager.SaveScene(scene, ScenePath); AssetDatabase.SaveAssets();
            Debug.Log("JERICHO BUILD PASS: scene, Joshua, ten soldiers, six ordered checkpoints; full Humanoid clips and valid Avatars.");
        }
        static GameObject Character(string name, string modelPath, RuntimeAnimatorController controller)
        {
            var root = new GameObject(name); root.layer = 2;
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));
            model.transform.SetParent(root.transform, false);
            foreach (var t in model.GetComponentsInChildren<Transform>()) t.gameObject.layer = 2;
            var animator = model.GetComponent<Animator>();
            if (!animator || !animator.avatar || !animator.avatar.isValid || !animator.avatar.isHuman) throw new InvalidOperationException("Invalid Avatar: " + modelPath);
            animator.runtimeAnimatorController = controller; animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var renderers = model.GetComponentsInChildren<Renderer>(); var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            float scale = 1.8f / bounds.size.y;
            model.transform.localScale *= scale; model.transform.localPosition = Vector3.up * (-bounds.min.y * scale);
            var motor = root.AddComponent<CharacterController>(); motor.height = 1.8f; motor.radius = .38f; motor.center = Vector3.up * .9f; motor.stepOffset = .25f;
            return root;
        }
        static Material Material(string name, Color color)
        {
            var material = new Material(Shader.Find("Standard")); material.color = color; AssetDatabase.CreateAsset(material, Root + name + ".mat"); return material;
        }
        static void Cube(string name, Vector3 position, Vector3 scale, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); cube.name = name; cube.transform.position = position; cube.transform.localScale = scale; cube.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}

