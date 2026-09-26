# Windows desktop build

Use Unity **2022.3.62f2**. The sole enabled Build Settings scene is
`Assets/Scenes/JerichoPrototype.unity`.

Build with **Tools > Jericho Prototype > Build Windows Desktop**.
This builds the existing saved scene, not the currently open scene, and never
calls PrototypeBuilder or regenerates prototype assets. Save intended edits
to the prototype before building. Output is a Windows x64 Development Build
at `Builds/Windows/Joshua.exe`; retain its adjacent data folder and DLLs.
The build summary is `Builds/Windows/build-result.txt`.

Run the executable normally to play. WASD moves, Shift runs, click locks the
mouse for camera rotation, Escape releases it. Alt+F4 closes the player.
Follow ground signs 1–6 around the walls. Completing the ordered route shows
Circuit: 1 / 1 and OBJECTIVE COMPLETE. Relaunch to start a fresh circuit.

## Repeatable standalone smoke test

From the project root in PowerShell:

```powershell
$smoke = Start-Process -FilePath '.\Builds\Windows\Joshua.exe' -ArgumentList '--jericho-smoke-test -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -logFile smoke-test.log' -WorkingDirectory "$PWD\Builds\Windows" -PassThru
$smoke.WaitForExit()
$smoke.ExitCode
```

Exit code 0 plus `JERICHO WINDOWS SMOKE PASS` in
`Builds/Windows/smoke-test.log` means the assertions passed. Exit code 1 means
failure; inspect that log. `smoke-complete.png` captures the completed screen.
The test is opt-in and compiled only in Editor/development configurations.
Normal launches do not create the smoke test component or automate input.

Checks include scene wiring, initial Idle, rejection of an out-of-order
checkpoint, actual movement around the six-point outside route using the
player input properties, Walk/Run and soldier Walk states, ordered checkpoint
completion, stopped Idle, and valid above-ground soldiers with root motion off.
Only the negative out-of-order check teleports; the full circuit uses movement.
This is a functional smoke test, not performance certification or a substitute
for checking keyboard/mouse feel and formation quality manually.

## Verification performed — 2026-09-26

- Final Windows x64 Development Build succeeded in Unity 2022.3.62f2: zero
  errors and zero warnings. Exact size/timing are in `build-result.txt`.
- Standalone smoke run returned exit code 0 and the explicit PASS marker.
  All six checkpoints were reached through controller-driven movement.
- A normal interactive launch rendered the mission HUD, walls and route hints.
- Removed the user-identified Icosphere helper children from both prototype
  prefabs and the scene's Joshua/ten soldiers. Original FBXs were preserved.
  Model instances were unpacked to permit removing the helper child; skin,
  material, Avatar and animation asset references remain intact.
- The final visible smoke run returned exit code 0. Its `smoke-complete.png`
  shows Circuit: 1 / 1 and OBJECTIVE COMPLETE with no Icospheres.
- Raised only the visual models with a `Model Height Offset` parent: Joshua
  +0.5992m, soldiers +1.9122m. Ground geometry and movement colliders are intact.
- Fixed prototype animation bindings that referenced hidden `__preview__`
  clips. Joshua uses a new `JoshuaRuntime.controller`; SoldierMarch uses full
  looping runtime Idle/Walk. Existing animation-test controllers are untouched.
  Characters now stand upright with visible legs. Materials remain pale;
  material polish and keyboard/mouse feel have not been changed.
- The final smoke test also requires more than ten degrees of sustained leg
  rotation on Joshua and every soldier after the first route leg, beyond one
  animation cycle, and rejects any hidden preview clip references.
- An earlier repeated smoke run exposed a timing-dependent Walk assertion.
  The test now keeps walking until the Animator evaluates Walk before
  requesting Run. The rebuilt executable passed the revised full test.
- No prototype regeneration was performed. Original FBXs, animation clips
  and animation-test controllers were preserved.

Files added for this build task: `Editor/WindowsPrototypeBuild.cs`,
`Editor/PrototypeModelCleanup.cs`, `Editor/AnimationDiagnostics.cs`,
`Editor/RuntimeAnimationRepair.cs`, `JoshuaRuntime.controller`,
`WindowsSmokeTest.cs`, this document, and their Unity `.meta` files.
Files modified: `ProjectSettings/EditorBuildSettings.asset`,
`Assets/Scenes/JerichoPrototype.unity`, `Assets/JerichoPrototype/Joshua.prefab`,
`Assets/JerichoPrototype/Soldier.prefab`,
`Assets/JerichoPrototype/SoldierMarch.controller`, and the prototype README.
Build outputs and test logs are under the git-ignored `Builds/Windows` folder.
