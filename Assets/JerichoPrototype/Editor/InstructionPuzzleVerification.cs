using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JerichoPrototype.Editor
{
    [InitializeOnLoad]
    public static class InstructionPuzzleVerification
    {
        static InstructionPuzzleVerification()
        {
            EditorApplication.delayCall += () => {
                if (!File.Exists(".utmp/instruction-test") || EditorApplication.isPlayingOrWillChangePlaymode) return;
                File.Delete(".utmp/instruction-test");
                EditorSceneManager.OpenScene(PrototypeBuilder.ScenePath);
                SessionState.SetBool("InstructionPuzzleTest",true);
                EditorApplication.isPlaying=true;
            };
            EditorApplication.playModeStateChanged += state => {
                if (!SessionState.GetBool("InstructionPuzzleTest",false)) return;
                if (state == PlayModeStateChange.EnteredPlayMode) EditorApplication.delayCall += Verify;
                if (state == PlayModeStateChange.EnteredEditMode) SessionState.SetBool("InstructionPuzzleTest",false);
            };
        }
        static void Require(bool value,string message) { if(!value)throw new Exception(message); }
        static void Verify()
        {
            try
            {
                var puzzle=new MarchingInstructionsPuzzle();
                Require(!puzzle.Check(),"Incomplete answer accepted");
                puzzle.Select(2);puzzle.Select(2);Require(puzzle.Order.Count==1,"Duplicate card");
                puzzle.Select(0);puzzle.Select(3);puzzle.Select(1);
                Require(!puzzle.Check() && puzzle.Feedback.StartsWith("Hint:"),"Wrong order did not give hint");
                puzzle.Remove(0);Require(puzzle.Order.Count==3 && !puzzle.Order.Contains(2),"Remove failed");
                puzzle.Reset();Require(puzzle.Order.Count==0 && !puzzle.Solved,"Reset failed");
                var briefing=UnityEngine.Object.FindObjectOfType<BriefingDialogue>();
                Require(briefing.IsOpen && !briefing.controller.enabled && !briefing.cameraController.enabled,"Briefing did not pause controls");
                briefing.Close();briefing.BeginMarch();
                Require(briefing.IsOpen && briefing.PuzzleOpen && !briefing.controller.enabled,"Puzzle bypassed");
                for(int i=0;i<4;i++)briefing.Puzzle.Select(i);
                Require(briefing.Puzzle.Check(),"Correct order rejected");
                Require(!briefing.controller.enabled,"Movement released before Begin march");
                briefing.BeginMarch();
                Require(!briefing.IsOpen && briefing.controller.enabled && briefing.cameraController.enabled,"Controls not restored");
                File.WriteAllText("Logs/instruction-puzzle-test.txt","PASS: incomplete answers, duplicate selection, wrong-order hint, removal, reset, correct order, briefing gate, and control restoration verified in Unity Play Mode.");
            }
            catch(Exception e){File.WriteAllText("Logs/instruction-puzzle-test.txt",e.ToString());}
            finally{EditorApplication.isPlaying=false;}
        }
    }
}
