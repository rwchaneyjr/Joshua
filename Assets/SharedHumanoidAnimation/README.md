# Shared Humanoid Animation

## Setup

1. In Unity, run **Tools > Shared Humanoid Animation > Create Shared Assets** to generate the shared locomotion controller and experimental March asset without altering original character assets.
2. Select scene objects with an `Animator` and a valid **Humanoid** Avatar, then run **Assign Controller To Selected Humanoids**.
3. The assignment adds `SharedHumanoidAnimationCoordinator`, which derives the controller's `Speed` float from character movement.

## Controller and sources

The shared locomotion controller uses the inspection-validated Humanoid Idle, Walking, and Running clips. Kick and Carry are action-test clips, and visual quality must be reviewed on each target Avatar.

## Character-specific animation test scene

`SampleScene` contains separate in-place test bindings for the two characters. Run **Tools > Shared Humanoid Animation > Resolve Kick and Carry Humanoid Compatibility**, then **Setup Animation Test Scene**, then **Verify Animation Test Scene** after importing or resetting assets.

### Warrior — Levantine Running character

Select **Meshy_AI_Ancient_Levantine_War_biped_Animation_Running_withSkin** and press Play. It uses `WarriorAnimationTest.controller` and supports only these actions:

- **1**: Idle
- **2**: Walk
- **3**: Run
- **4**: Kick
- **5**: Carry is unavailable. Keyboard `5` and its on-screen button leave the Animator on its current supported action and show **Carry unavailable for Warrior**.

### Priest — Desert Sentinel Idle character

Select **Meshy_AI_Desert_Sentinel_biped_Animation_Idle_4_withSkin** and press Play. It uses `AnimationTest.controller` and supports:

- **1**: Idle
- **2**: Walk
- **3**: Run
- **4**: Kick
- **5**: Carry, using the full embedded `Carry_Heavy_Cannon_Forward` take from the validated Humanoid Carry source FBX.

Both characters display their controls in the upper-left and force root motion off for an in-place preview. Their test controllers are assigned only to the scene instances; original character controllers remain untouched.

## Exact verification steps

1. Open `SampleScene`.
2. Run **Tools > Shared Humanoid Animation > Resolve Kick and Carry Humanoid Compatibility**.
3. Run **Tools > Shared Humanoid Animation > Setup Animation Test Scene**.
4. Run **Tools > Shared Humanoid Animation > Verify Animation Test Scene**. This confirms the Warrior controller has no Carry state, Warrior key/button `5` is configured unavailable, and Priest has the full Carry clip at state `5`.
5. For runtime validation, run **Tools > Shared Humanoid Animation > Verify Priest Kick and Carry In Play Mode**. It enters the Priest's Kick and Carry states for `4` and `5` and exits.
6. For visual confirmation, press Play and use the character-specific controls above. Inspect Priest Carry retargeting before production use.