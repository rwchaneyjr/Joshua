using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JerichoPrototype.Editor
{
    [InitializeOnLoad]
    public static class PrototypeVerification
    {
        static int stage;
        static float started;
        static Vector3 origin;
        static PrototypeVerification() { EditorApplication.update += Tick; }
        public static void Run()
        {
            EditorSceneManager.OpenScene(PrototypeBuilder.ScenePath);
            SessionState.SetBool("JerichoVerification", true);
            SessionState.SetFloat("JerichoDeadline", (float)EditorApplication.timeSinceStartup + 120);
            EditorApplication.isPlaying = true;
        }
        static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
        static void Tick()
        {
            if (!SessionState.GetBool("JerichoVerification", false)) return;
            try
            {
                Require(EditorApplication.timeSinceStartup < SessionState.GetFloat("JerichoDeadline", 0), "Play Mode test timed out");
                if (!EditorApplication.isPlaying || Time.time < .5f) return;
                var player = UnityEngine.Object.FindObjectOfType<JoshuaController>();
                var manager = UnityEngine.Object.FindObjectOfType<ProcessionManager>();
                var mission = UnityEngine.Object.FindObjectOfType<JerichoMission>();
                if (stage == 0)
                {
                    Require(player && manager && mission && manager.followers.Length == 17, "Scene wiring");
                    Require(player.animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), "Idle state");
                    foreach (var npc in manager.followers) Require(npc.animator.avatar.isValid && npc.animator.avatar.isHuman && !npc.animator.applyRootMotion, "NPC Avatar/root motion");
                    player.readKeyboard = false; player.MoveInput = Vector2.up; origin = player.transform.position;
                    Advance();
                }
                else if (stage == 1 && Elapsed > 1)
                {
                    Require(Vector3.Distance(origin, player.transform.position) > 1.5f, "Walk movement");
                    Require(player.animator.GetCurrentAnimatorStateInfo(0).IsName("Walk"), "Walk animation");
                    player.RunInput = true; origin = player.transform.position; Advance();
                }
                else if (stage == 2 && Elapsed > 1)
                {
                    Require(Vector3.Distance(origin, player.transform.position) > 3.5f, "Run movement");
                    Require(player.animator.GetCurrentAnimatorStateInfo(0).IsName("Run"), "Run animation");
                    var a = manager.followers[0].animator; var b = manager.followers[10].animator;
                    Require(a.GetCurrentAnimatorStateInfo(0).IsName("WALK") && b.GetCurrentAnimatorStateInfo(0).IsName("Walk"), "March state");
                    Require(Mathf.Abs(Mathf.Repeat(b.GetFloat("MarchPhase") - a.GetFloat("MarchPhase"), 1) - .5f) < .01f, "Alternating phase");
                    manager.followers[10].gameObject.SetActive(false);
                    player.MoveInput = Vector2.zero; Advance();
                }
                else if (stage == 3 && Elapsed > .5f)
                {
                    Require(player.animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), "Stop animation");
                    Require(Mathf.Abs(player.transform.position.y) < .15f, "Ground contact");
                    manager.followers[10].gameObject.SetActive(true); Advance();
                }
                else if (stage == 4 && Elapsed > .5f)
                {
                    float difference = manager.followers[10].animator.GetFloat("MarchPhase") - manager.followers[0].animator.GetFloat("MarchPhase");
                    Require(Mathf.Abs(Mathf.Repeat(difference, 1) - .5f) < .01f, "Late join phase");
                    Teleport(player, mission.checkpoints[2].position); Advance();
                }
                else if (stage == 5 && Elapsed > .15f)
                {
                    Require(mission.CompletedCheckpoints == 0, "Out-of-order checkpoint accepted");
                    Teleport(player, mission.checkpoints[0].position); Advance();
                }
                else if (stage >= 6 && stage <= 11 && Elapsed > .15f)
                {
                    int expected = stage - 5;
                    Require(mission.CompletedCheckpoints == expected, "Ordered checkpoint " + expected);
                    if (expected == 6)
                    {
                        Require(mission.Complete, "Mission completion");
                        Debug.Log("JERICHO PLAYMODE PASS: Idle/Walk/Run, movement/stop/ground, 7 horn priests, 10 valid soldiers, synchronized marching, A/B phase, late rejoin, rejected out-of-order checkpoint, all six checkpoints and completion.");
                        Finish(0); return;
                    }
                    Teleport(player, mission.checkpoints[expected].position); Advance();
                }
            }
            catch (Exception exception) { Debug.LogException(exception); Finish(1); }
        }
        static float Elapsed => Time.time - started;
        static void Advance() { stage++; started = Time.time; }
        static void Teleport(JoshuaController player, Vector3 position)
        {
            var motor = player.GetComponent<CharacterController>(); motor.enabled = false; player.transform.position = position + Vector3.up * .05f; motor.enabled = true;
        }
        static void Finish(int code)
        {
            SessionState.SetBool("JerichoVerification", false);
            EditorApplication.isPlaying = false;
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }
    }
}
