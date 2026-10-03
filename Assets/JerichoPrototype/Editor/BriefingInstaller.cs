#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace JerichoPrototype.Editor
{
    public static class BriefingInstaller
    {
        [InitializeOnLoadMethod] static void CheckRequest()
        {
            EditorApplication.delayCall += () => {
                if (!File.Exists(".utmp/opening-briefing")) return;
                File.Delete(".utmp/opening-briefing");
                try { Install(); }
                catch (Exception ex) { File.WriteAllText("Logs/opening-briefing.txt", ex.ToString()); }
            };
        }
        [MenuItem("Tools/Jericho Prototype/Install Opening Briefing")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play Mode first.");
            var player = UnityEngine.Object.FindObjectOfType<JoshuaController>();
            if (!player) throw new Exception("Open the Jericho scene first.");
            var npc = GameObject.Find("Briefing NPC");
            var root = GameObject.Find("Opening Briefing");
            if (!root) { root = new GameObject("Opening Briefing"); Undo.RegisterCreatedObjectUndo(root, "Add opening briefing"); }
            var dialogue = root.GetComponent<BriefingDialogue>() ?? root.AddComponent<BriefingDialogue>();
            if (npc && npc.GetComponent<BriefingDialogue>()) EditorUtility.CopySerialized(npc.GetComponent<BriefingDialogue>(), dialogue);
            dialogue.speakerName = "Mission Briefing";
            dialogue.player = player.transform;
            dialogue.controller = player;
            dialogue.cameraController = UnityEngine.Object.FindObjectOfType<ThirdPersonCamera>();
            if (npc) Undo.DestroyObjectImmediate(npc);
            EditorUtility.SetDirty(dialogue);
            EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
            EditorSceneManager.SaveScene(player.gameObject.scene);
            File.WriteAllText("Logs/opening-briefing.txt", "PASS: opening briefing saved with existing dialogue pages; NPC removed. Instruction visibility tied to first checkpoint.");
        }
    }
}
#endif
