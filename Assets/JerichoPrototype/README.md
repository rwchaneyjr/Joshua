# Small Jericho prototype

Unity 2022.3.62f2. Open `Assets/Scenes/JerichoPrototype.unity` and press Play.
Select `Joshua` to inspect movement speeds and Animator references.
WASD moves relative to the camera, either Shift runs. Click the Game window
to lock the mouse and rotate the camera; Escape releases it.

Follow route signs 1–6 around the outside of the walls. The checkpoint objects
are invisible; ground numbers are navigation aids. Each checkpoint has a four
metre acceptance radius. Only the next checkpoint counts. After all six,
Circuit becomes 1 / 1 and OBJECTIVE COMPLETE appears. Restart Play to reset.

Existing assets reused:
- Joshua: Ancient Mediterranean Walking withSkin FBX.
- Soldier: Ancient Levantine War Running withSkin FBX.
- JoshuaRuntime.controller: prototype copy of the existing animation contract,
  bound to the full imported runtime clips instead of hidden editor previews.
- Full embedded Idle_4 (Desert Sentinel), Walking (Mediterranean), Running
  (Levantine Warrior) clips. Original clips, importers and controllers are intact.

JoshuaController owns the player's TestState/Speed parameters. The prototype
has no AnimationTestControls or SharedHumanoidAnimationCoordinator components.
Input properties are separate from keyboard reading for automated checks.
All character root motion is disabled. CharacterControllers own movement.

Icosphere helper children have been removed from the prototype models. The
source FBXs remain unchanged. A `Model Height Offset` parent raises visual
models independently of movement colliders: Joshua +0.5992m and soldiers
+1.9122m. These offsets were measured from the posed mesh; original animation
and skin assets were not edited.

The Soldier prefab uses a separate SoldierMarch controller. MarchCoordinator
owns Marching and MarchPhase, sampling the existing Walk clip with a state
motion-time parameter. Cycles Per Second sets cadence. FormationSlot phase
group 0/1 selects offsets 0/.5. Rows alternate; disabled/re-enabled soldiers
unregister/rejoin the shared clock. Setting marching false stops animation,
not movement; ordinarily individual movement selects Idle/Walk automatically.

ProcessionManager exposes the leader and ten followers. FormationSlot contains
each offset. Followers steer toward slots with local separation and collision;
there is no navigation mesh or path planning. Use the wide marked outside
route. Tight turns, intentional collisions and shortcuts against walls may
distort or temporarily trap the formation. Walk pace gives the best march;
catch-up/run speed can cause foot sliding because cadence is shared.

ThirdPersonCamera exposes distance, height, sensitivity and collision mask.
Characters use built-in Ignore Raycast layer so camera collision sees the
ground/walls rather than the player. No project physics settings were changed.

PrototypeBuilder is an Editor-only one-time builder that refuses to overwrite
an existing prototype scene. PrototypeVerification.Run executes automated
Play Mode checks in Unity batch mode. These do not replace visual review of
retargeting, camera feel, formation turns, or physically walking the full route.

No Ark, priests, combat, wall collapse, mobile controls or other missions.
