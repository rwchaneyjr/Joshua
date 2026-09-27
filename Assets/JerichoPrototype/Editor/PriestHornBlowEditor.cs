#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace JerichoPrototype.Editor
{
    [CustomEditor(typeof(PriestHornBlow))]
    public sealed class PriestHornBlowEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Temporary Play Mode Tests", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Blow This Priest")) ((PriestHornBlow)target).BlowHorn();
                if (GUILayout.Button("Repeat This Priest")) { var horn = (PriestHornBlow)target; horn.BlowHorn(); horn.BlowHorn(); }
                if (GUILayout.Button("Blow All Priests")) PriestHornBlowTestTools.BlowAll();
            }
            if (!EditorApplication.isPlaying)
                EditorGUILayout.HelpBox("Enter Play Mode to run gesture tests. Use the menu verification for deterministic prefab checks.", MessageType.Info);
        }
    }

    public static class PriestHornBlowTestTools
    {
        [MenuItem("Tools/Jericho Prototype/Horn Tests/Blow All Priests (Play Mode)")]
        public static void BlowAll()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play Mode before triggering priests."); return; }
            foreach (var horn in UnityEngine.Object.FindObjectsOfType<PriestHornBlow>()) horn.BlowHorn();
        }

        [MenuItem("Tools/Jericho Prototype/Horn Tests/Verify Prefab Sequence")]
        public static void VerifyPrefabSequence()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JerichoPrototype/Priest.prefab");
            if (!prefab) throw new Exception("Priest prefab is missing.");
            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            instance.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var horn = instance.GetComponent<PriestHornBlow>();
                var follower = instance.GetComponent<ProcessionFollower>();
                var animator = instance.GetComponentInChildren<Animator>();
                if (!horn || !follower || !animator || !animator.avatar || !animator.avatar.isHuman)
                    throw new Exception("Priest needs PriestHornBlow, ProcessionFollower, and a valid Humanoid Animator.");
                var upperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                var startArm = upperArm.localRotation;
                var startHorn = FindHorn(instance.transform);
                if (!startHorn) throw new Exception("Ceremonial Horn reference is missing.");
                var savedParent = startHorn.parent;
                var savedLocalPosition = startHorn.localPosition;
                var savedLocalRotation = startHorn.localRotation;
                var startWorldPosition = startHorn.position;

                horn.BlowHorn();
                horn.BlowHorn(); // repeated trigger must restart safely
                horn.DebugAdvance(.55f);
                if (!follower.MovementSuppressed) throw new Exception("Follower did not stop during horn sequence.");
                if (Quaternion.Angle(startArm, upperArm.localRotation) < .1f) throw new Exception("Horn sequence did not move Humanoid arm bone.");
                if (Vector3.Distance(startWorldPosition, startHorn.position) < .0001f) throw new Exception("Horn did not follow the raised hand.");
                if (startHorn.parent != savedParent) throw new Exception("Horn parenting changed.");

                horn.DebugAdvance(1.4f);
                horn.DebugAdvance(.5f);
                if (horn.IsBlowing || follower.MovementSuppressed) throw new Exception("Sequence did not complete and resume following.");
                if (startHorn.parent != savedParent || startHorn.localPosition != savedLocalPosition || startHorn.localRotation != savedLocalRotation)
                    throw new Exception("Horn saved local alignment was not restored.");
                Debug.Log("PRIEST HORN PASS: trigger/retrigger, bone motion, horn alignment, completion, and follower resume.");
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        static Transform FindHorn(Transform root)
        {
            foreach (var node in root.GetComponentsInChildren<Transform>(true))
                if (node.name.ToLowerInvariant().Contains("horn")) return node;
            return null;
        }
    }
}
#endif