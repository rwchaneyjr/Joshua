#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace JerichoPrototype
{
    // Opt-in standalone smoke test. Normal launches never instantiate this component.
    public sealed class WindowsSmokeTest : MonoBehaviour
    {
        bool failed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--jericho-smoke-test") >= 0)
                new GameObject("Windows smoke test").AddComponent<WindowsSmokeTest>();
        }
        IEnumerator Start()
        {
            Application.runInBackground = true;
            Application.logMessageReceived += OnLog;
            var test = Test();
            while (true)
            {
                object current;
                try { if (!test.MoveNext()) break; current = test.Current; }
                catch (Exception ex) { Debug.LogError("JERICHO WINDOWS SMOKE FAIL: " + ex); Application.Quit(1); yield break; }
                yield return current;
            }
            if (failed) { Application.Quit(1); yield break; }
            Debug.Log("JERICHO WINDOWS SMOKE PASS: actual player movement around six ordered checkpoints; rejected out-of-order checkpoint; Idle/Walk/Run; ten soldiers; Circuit 1/1 and objective complete.");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath, "..", "smoke-complete.png"));
            yield return new WaitForSeconds(2);
            Application.Quit(0);
        }
        void OnLog(string message, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) failed = true;
        }
        void OnDestroy() { Application.logMessageReceived -= OnLog; }
        static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); Debug.Log("SMOKE CHECK: " + message); }
        IEnumerator Test()
        {
            yield return new WaitForSeconds(1);
            var player = FindObjectOfType<JoshuaController>();
            var mission = FindObjectOfType<JerichoMission>();
            var procession = FindObjectOfType<ProcessionManager>();
            Check(player && mission && procession && procession.followers.Length == 10, "Scene references and ten soldiers");
            Check(mission.checkpoints.Length == 6 && mission.CompletedCheckpoints == 0, "Six checkpoints, fresh objective");
            Check(player.animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), "Idle animation");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath, "..", "smoke-start.png"));
            player.readKeyboard = false;
            Vector3 start = player.transform.position;
            var motor = player.GetComponent<CharacterController>();
            motor.enabled = false; player.transform.position = mission.checkpoints[2].position + Vector3.up * .05f; motor.enabled = true;
            yield return null; yield return null;
            Check(mission.CompletedCheckpoints == 0, "Out-of-order checkpoint rejected");
            motor.enabled = false; player.transform.position = start; motor.enabled = true;
            bool checkedWalk = false, checkedRun = false;
            var animated = new Animator[11]; animated[0] = player.animator;
            for (int n = 0; n < 10; n++) animated[n + 1] = procession.followers[n].animator;
            var legs = new Transform[11]; var firstLegRotation = new Quaternion[11]; var motion = new float[11];
            bool capturedMotion = false;
            for (int n = 0; n < 11; n++)
            {
                legs[n] = animated[n].GetBoneTransform(HumanBodyBones.LeftUpperLeg);
                foreach (var clip in animated[n].runtimeAnimatorController.animationClips)
                    Check(!clip.name.StartsWith("__preview__", StringComparison.Ordinal), "Runtime clip, not editor preview: " + clip.name);
            }
            for (int i = 0; i < 6; i++)
            {
                float began = Time.realtimeSinceStartup;
                while (true)
                {
                    Vector3 delta = mission.checkpoints[i].position - player.transform.position; delta.y = 0;
                    if (delta.magnitude < .5f) break;
                    if (Time.realtimeSinceStartup - began > 35) throw new Exception("Movement stalled before checkpoint " + (i + 1));
                    Vector3 forward = player.cameraTransform.forward; forward.y = 0; forward.Normalize();
                    Vector3 right = Vector3.Cross(Vector3.up, forward);
                    Vector3 direction = delta.normalized;
                    player.MoveInput = new Vector2(Vector3.Dot(direction, right), Vector3.Dot(direction, forward));
                    // Keep walking until the Animator has evaluated the requested input.
                    // Wall-clock delays (window activation or machine load) must not skip it.
                    player.RunInput = checkedWalk && (i > 0 || Time.realtimeSinceStartup - began > 2);
                    // Check sustained bone motion after the first leg of the route, well
                    // beyond the end of a single Walk/Run cycle. State names alone cannot pass.
                    if (i >= 1)
                    {
                        for (int n = 0; n < legs.Length; n++)
                        {
                            if (!capturedMotion) firstLegRotation[n] = legs[n].localRotation;
                            motion[n] = Mathf.Max(motion[n], Quaternion.Angle(firstLegRotation[n], legs[n].localRotation));
                        }
                        capturedMotion = true;
                    }
                    if (!checkedWalk && player.animator.GetCurrentAnimatorStateInfo(0).IsName("Walk"))
                    {
                        Check(player.animator.GetCurrentAnimatorStateInfo(0).IsName("Walk"), "Walk animation during movement"); checkedWalk = true;
                    }
                    if (!checkedRun && player.RunInput && player.animator.GetCurrentAnimatorStateInfo(0).IsName("Run"))
                    {
                        Check(player.animator.GetCurrentAnimatorStateInfo(0).IsName("Run"), "Run animation during movement"); checkedRun = true;
                        Check(procession.followers[0].animator.GetCurrentAnimatorStateInfo(0).IsName("Walk"), "Soldier marching during movement");
                    }
                    yield return null;
                }
                Check(mission.CompletedCheckpoints == i + 1, "Ordered checkpoint " + (i + 1));
            }
            player.MoveInput = Vector2.zero;
            yield return new WaitForSeconds(1);
            Check(checkedWalk && checkedRun, "Walk and Run both exercised");
            for (int n = 0; n < motion.Length; n++)
                Check(motion[n] > 10f, "Sustained leg animation " + n + ": " + motion[n].ToString("F1") + " degrees");
            Check(mission.Complete && mission.CompletedCheckpoints == 6, "Circuit 1/1 objective complete");
            Check(player.animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), "Idle after stopping");
            foreach (var npc in procession.followers)
                Check(npc && npc.animator.avatar.isValid && !npc.animator.applyRootMotion && npc.transform.position.y > -.2f, "Soldier valid and above ground");
        }
    }
}
#endif
