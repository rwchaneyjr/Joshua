using System.Collections.Generic;
using UnityEngine;

namespace SharedHumanoidAnimation
{
    /// <summary>Character-local animation test driver. A single shared selector owns all keyboard and button input.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class AnimationTestControls : MonoBehaviour
    {
        private const string TestStateParameter = "TestState";
        private const string SpeedParameter = "Speed";
        private const int IdleState = 1;
        private const int WalkState = 2;
        private const int RunState = 3;
        private const int KickState = 4;
        private const int CarryState = 5;

        private static readonly List<AnimationTestControls> RegisteredControls = new List<AnimationTestControls>();
        private static AnimationTestControls selectedControl;

        [SerializeField] private Animator targetAnimator;
        [SerializeField] private RuntimeAnimatorController testController;
        [SerializeField] private string characterLabel;
        [SerializeField] private bool testMode = true;
        [SerializeField] private bool showOnScreenControls = true;
        [SerializeField] private bool carryAvailable;

        private SharedHumanoidAnimationCoordinator movementCoordinator;
        private RuntimeAnimatorController previousController;
        private bool previousRootMotion;
        private bool coordinatorWasEnabled;
        private int selectedState = IdleState;
        private bool carryUnavailableRequested;

        private void Reset() => targetAnimator = GetComponent<Animator>();

        private void Awake()
        {
            if (targetAnimator == null) targetAnimator = GetComponent<Animator>();
            movementCoordinator = GetComponent<SharedHumanoidAnimationCoordinator>();
            EnableTestMode();
        }

        private void OnEnable()
        {
            if (!RegisteredControls.Contains(this)) RegisteredControls.Add(this);
            if (selectedControl == null || characterLabel == "Joshua") selectedControl = this;
        }

        private void OnDisable()
        {
            RegisteredControls.Remove(this);
            if (selectedControl == this) selectedControl = RegisteredControls.Count > 0 ? RegisteredControls[0] : null;
        }

        private void OnDestroy() => RestoreAnimatorOwnership();

        private void Update()
        {
            // This gate prevents the other scene characters from consuming the same commands.
            if (!testMode || selectedControl != this || targetAnimator == null) return;
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) SelectState(IdleState);
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) SelectState(WalkState);
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) SelectState(RunState);
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) SelectState(KickState);
            if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) SelectCarryState();
        }

        private void EnableTestMode()
        {
            if (!testMode || targetAnimator == null) return;
            previousController = targetAnimator.runtimeAnimatorController;
            previousRootMotion = targetAnimator.applyRootMotion;
            coordinatorWasEnabled = movementCoordinator != null && movementCoordinator.enabled;
            if (movementCoordinator != null) movementCoordinator.enabled = false;
            targetAnimator.applyRootMotion = false;
            if (testController != null) targetAnimator.runtimeAnimatorController = testController;
            SelectState(IdleState);
        }

        private void RestoreAnimatorOwnership()
        {
            if (targetAnimator == null) return;
            if (testMode && previousController != null) targetAnimator.runtimeAnimatorController = previousController;
            targetAnimator.applyRootMotion = previousRootMotion;
            if (movementCoordinator != null) movementCoordinator.enabled = coordinatorWasEnabled;
        }

        private void SelectState(int state)
        {
            if (targetAnimator == null) return;
            carryUnavailableRequested = false;
            selectedState = state;
            targetAnimator.SetInteger(TestStateParameter, state);
            targetAnimator.SetFloat(SpeedParameter, state == WalkState ? 0.1f : state == RunState ? 3f : 0f);
        }

        private void SelectCarryState()
        {
            if (carryAvailable) { SelectState(CarryState); return; }
            carryUnavailableRequested = true;
        }

        private void OnGUI()
        {
            // Exactly one panel is rendered: the selected character owns the selector and actions.
            if (!testMode || !showOnScreenControls || targetAnimator == null || selectedControl != this) return;
            using (JerichoPrototype.BlackGameText.Scope()) { GUILayout.BeginArea(new Rect(16f, 16f, 310f, 300f), GUI.skin.box);
            GUILayout.Label("Selected: " + characterLabel);
            GUILayout.Label("Choose character (only selected receives 1-5):");
            GUILayout.BeginHorizontal();
            DrawSelector("Joshua");
            DrawSelector("Warrior");
            DrawSelector("Priest");
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);
            GUILayout.Label("Current: " + CurrentStateLabel());
            GUILayout.Label("Root motion: OFF (in-place)");
            DrawButton("1  Idle", IdleState);
            DrawButton("2  Walk", WalkState);
            DrawButton("3  Run", RunState);
            DrawButton("4  Kick", KickState);
            DrawCarryButton();
            GUILayout.EndArea(); }
        }

        private void DrawSelector(string label)
        {
            AnimationTestControls control = RegisteredControls.Find(candidate => candidate != null && candidate.characterLabel == label);
            bool isSelected = control == selectedControl;
            if (GUILayout.Button(isSelected ? "[" + label + "]" : label) && control != null)
                selectedControl = control;
        }

        private void DrawCarryButton()
        {
            string label = carryAvailable ? "5  Carry" : "5  Carry (unavailable on " + characterLabel + ")";
            if (GUILayout.Button(label)) SelectCarryState();
        }

        private void DrawButton(string label, int state)
        {
            if (GUILayout.Button(label)) SelectState(state);
        }

        private string CurrentStateLabel()
        {
            if (carryUnavailableRequested) return "Carry unavailable on " + characterLabel;
            AnimatorStateInfo stateInfo = targetAnimator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Idle")) return "Idle";
            if (stateInfo.IsName("Walk")) return "Walk";
            if (stateInfo.IsName("Run")) return "Run";
            if (stateInfo.IsName("Kick")) return "Kick";
            if (stateInfo.IsName("Carry")) return "Carry";
            return "Transitioning";
        }
    }
}
