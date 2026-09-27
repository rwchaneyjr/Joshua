using System;
using System.Collections.Generic;
using UnityEngine;

namespace JerichoPrototype
{
    /// <summary>
    /// Temporary Play Mode-only keyboard harness for the saved priest horn gestures.
    /// It creates no serialized scene or prefab references, so horn attachments remain untouched.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class PriestHornKeyboardTestControls : MonoBehaviour
    {
        const int PriestCount = 7;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void CreateRuntimeHarness()
        {
            var harness = new GameObject("Priest Horn Keyboard Test Controls");
            DontDestroyOnLoad(harness);
            harness.AddComponent<PriestHornKeyboardTestControls>();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.H))
            {
                TriggerAll();
                return;
            }

            for (int index = 0; index < PriestCount; index++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + index)) ||
                    Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad1 + index)))
                {
                    TriggerPriest(index);
                    return;
                }
            }
        }

        /// <summary>Triggers every active saved priest in Play Mode.</summary>
        public static void TriggerAll()
        {
            foreach (var priest in FindPriests()) priest.BlowHorn();
        }

        /// <summary>Triggers a saved priest by its zero-based formation index.</summary>
        public static void TriggerPriest(int formationIndex)
        {
            var priests = FindPriests();
            if (formationIndex < 0 || formationIndex >= priests.Count)
            {
                Debug.LogWarning($"Horn test key requested Priest {formationIndex + 1}, but only {priests.Count} active priest(s) were found.");
                return;
            }

            priests[formationIndex].BlowHorn();
        }

        static List<PriestHornBlow> FindPriests()
        {
            var priests = new List<PriestHornBlow>(FindObjectsOfType<PriestHornBlow>());
            priests.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
            return priests;
        }
    }
}
