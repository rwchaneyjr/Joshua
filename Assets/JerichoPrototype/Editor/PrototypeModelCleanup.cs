using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JerichoPrototype.Editor
{
    public static class PrototypeModelCleanup
    {
        [MenuItem("Tools/Jericho Prototype/Align Model Feet and Build Windows")]
        public static void AlignAndBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (string path in new[] { "Assets/JerichoPrototype/Joshua.prefab", "Assets/JerichoPrototype/Soldier.prefab" })
            {
                var contents = PrefabUtility.LoadPrefabContents(path);
                try { Align(contents); PrefabUtility.SaveAsPrefabAsset(contents, path); }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }
            var scene = EditorSceneManager.OpenScene(WindowsPrototypeBuild.Scene);
            // Soldiers inherit their corrected prefab. Joshua is a standalone scene root.
            foreach (var root in scene.GetRootGameObjects().Where(r => r.GetComponent<JoshuaController>())) Align(root);
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            WindowsPrototypeBuild.Build();
        }
        static void Align(GameObject character)
        {
            var animator = character.GetComponentInChildren<Animator>();
            var offset = character.transform.Find("Model Height Offset");
            if (!offset)
            {
                offset = new GameObject("Model Height Offset").transform;
                offset.SetParent(character.transform, false);
                animator.transform.SetParent(offset, true);
            }
            // Pose a disposable copy so measuring does not save sampled bone transforms.
            var copy = UnityEngine.Object.Instantiate(character);
            float bottom;
            try
            {
                var sampled = copy.GetComponentInChildren<Animator>();
                sampled.Rebind(); sampled.Play("Idle", 0, 0); sampled.Update(0);
                bottom = Bottom(copy);
                if (float.IsInfinity(bottom) || float.IsNaN(bottom)) throw new InvalidOperationException("No measurable character skin.");
            }
            finally { UnityEngine.Object.DestroyImmediate(copy); }
            offset.localPosition += Vector3.up * (.01f - bottom);
            Debug.Log("MODEL HEIGHT " + character.name + " raised by " + (.01f - bottom).ToString("F4") + "; visual offset Y=" + offset.localPosition.y.ToString("F4"));
        }
        [MenuItem("Tools/Jericho Prototype/Remove Prototype Icospheres")]
        public static void CleanAndBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (string path in new[] { "Assets/JerichoPrototype/Joshua.prefab", "Assets/JerichoPrototype/Soldier.prefab" })
            {
                var contents = PrefabUtility.LoadPrefabContents(path);
                try { Remove(contents); PrefabUtility.SaveAsPrefabAsset(contents, path); }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }
            var scene = EditorSceneManager.OpenScene(WindowsPrototypeBuild.Scene);
            foreach (var root in scene.GetRootGameObjects().Where(r => r.GetComponent<JoshuaController>() || r.GetComponent<ProcessionFollower>()))
                Remove(root);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Audit();
            WindowsPrototypeBuild.Build();
        }
        static void Remove(GameObject character)
        {
            var spheres = character.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Icosphere").ToArray();
            foreach (var sphere in spheres)
            {
                if (sphere.GetComponentInChildren<SkinnedMeshRenderer>(true)) throw new InvalidOperationException("Refusing to remove a character skin.");
                var model = PrefabUtility.GetOutermostPrefabInstanceRoot(sphere.gameObject);
                // Unpack only this model instance to remove the helper mesh, leaving its
                // imported mesh, material and Avatar references intact. Never edit FBXs.
                if (model) PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                UnityEngine.Object.DestroyImmediate(sphere.gameObject);
                Debug.Log("Removed Icosphere from " + character.name);
            }
        }
        public static void Audit()
        {
            var scene = EditorSceneManager.OpenScene(WindowsPrototypeBuild.Scene);
            foreach (var root in scene.GetRootGameObjects().Where(r => r.GetComponent<JoshuaController>() || r.GetComponent<ProcessionFollower>()))
            {
                var animator = root.GetComponentInChildren<Animator>();
                animator.Rebind(); animator.Play("Idle", 0, 0); animator.Update(0);
                Debug.Log("HEIGHT AUDIT " + root.name + " model local=" + animator.transform.localPosition.ToString("F4") + " scale=" + animator.transform.localScale.ToString("F4") + " bottom=" + Bottom(root).ToString("F4") + " leftFoot=" + root.transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.LeftFoot).position).y.ToString("F4"));
            }
        }
        static float Bottom(GameObject character)
        {
            float min = float.PositiveInfinity;
            var mesh = new Mesh();
            try
            {
                foreach (var skin in character.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    skin.BakeMesh(mesh);
                    foreach (var vertex in mesh.vertices)
                        min = Mathf.Min(min, character.transform.InverseTransformPoint(skin.transform.TransformPoint(vertex)).y);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(mesh); }
            return min;
        }
    }
}
