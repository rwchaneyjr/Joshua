#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SharedHumanoidAnimation.Editor
{
    /// <summary>Runs the two action bindings in actual Play Mode and exits batch mode with a useful status.</summary>
    public static class AnimationTestPlayModeVerification
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string TargetCharacterName = "Meshy_AI_Desert_Sentinel_biped_Animation_Idle_4_withSkin";
        private const string TestStateParameter = "TestState";
        private static readonly int TestState = Animator.StringToHash(TestStateParameter);
        private static float stateStartedAt;
        private static int phase;

        [MenuItem("Tools/Shared Humanoid Animation/Verify Priest Kick and Carry In Play Mode")]
        public static void VerifyKickAndCarryInPlayMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Exit Play Mode before starting the Kick and Carry verification.");
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode)
            {
                return;
            }

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            phase = 0;
            EditorApplication.update += VerifyActions;
        }

        private static void VerifyActions()
        {
            GameObject target = GameObject.Find(TargetCharacterName);
            Animator animator = target != null ? target.GetComponent<Animator>() : null;
            if (animator == null)
            {
                Fail("The Priest test character or its Animator was not available in Play Mode.");
                return;
            }

            if (phase == 0)
            {
                animator.SetInteger(TestState, 4);
                stateStartedAt = Time.realtimeSinceStartup;
                phase = 1;
                return;
            }

            if (phase == 1 && Time.realtimeSinceStartup - stateStartedAt >= 0.25f)
            {
                if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Kick"))
                {
                    Fail("Play Mode did not enter the Kick state for TestState=4.");
                    return;
                }

                animator.SetInteger(TestState, 5);
                stateStartedAt = Time.realtimeSinceStartup;
                phase = 2;
                return;
            }

            if (phase == 2 && Time.realtimeSinceStartup - stateStartedAt >= 0.25f)
            {
                if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Carry"))
                {
                    Fail("Play Mode did not enter the Carry state for TestState=5.");
                    return;
                }

                Succeed();
            }
        }

        private static void Succeed()
        {
            EditorApplication.update -= VerifyActions;
            Debug.Log("Priest Play Mode Kick and Carry verification passed: 4=Kick and 5=Carry.");
            Finish(0);
        }

        private static void Finish(int exitCode)
        {
            EditorApplication.isPlaying = false;
            if (Application.isBatchMode)
            {
                EditorApplication.delayCall += () => EditorApplication.Exit(exitCode);
            }
        }

        private static void Fail(string message)
        {
            EditorApplication.update -= VerifyActions;
            Debug.LogError(message);
            Finish(1);
        }
    }
}
#endif