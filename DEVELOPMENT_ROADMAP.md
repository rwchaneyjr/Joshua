# Joshua: The Promised Land — Development Roadmap

**Project:** `C:\Users\rober\Joshua`  
**Unity:** `2022.3.62f2` — do not upgrade.  
**Current branch:** `codex/phase-2-character-foundation`

## Development Rules

- Inspect existing work before editing; reuse models, rigs, animations, controllers, and systems.
- Preserve original assets and working animation tests.
- Avoid unrelated changes and unnecessary ProjectSettings changes.
- Complete one phase at a time. Compile and test its behavior.
- Report files changed, tests performed, limitations, and exact testing instructions.
- Save and commit completed work on its phase branch. Do not merge without approval.
- Stop after each phase and wait for approval.
- Report **WORKING**, **BLOCKED**, or **DONE — READY TO TEST** accurately.

## Completed Checkpoint — Animation Testing

- Project audit completed.
- Joshua, Warrior, and Priest animation tests work; confirmed by the user.
- Shared character selector and keyboard/on-screen animation controls exist.
- Carry is restricted to the compatible Priest.
- Animation checkpoint committed and pushed to `main`.

## Phase 2 — Joshua Movement and Camera: CURRENT

1. Verify project folder and current branch.
2. Inspect animation/test scripts to avoid competing Animator control.
3. Create reusable Joshua prefab from the existing character.
4. Implement `JoshuaController.cs`: camera-relative WASD movement, Shift running, turning, gravity, and Idle/Walk/Run.
5. Separate input reading from movement for later mobile controls.
6. Add smooth third-person camera with adjustable distance/height and collision handling.
7. Create a separate movement test scene with ground; preserve animation test scene.
8. Test movement, stopping, running, turning, ground contact, and camera behavior.
9. Save, commit, report, and **STOP**.

## Future Phases — Not Authorized Yet

- **Phase 3 — NPC foundation**: reusable NPC prefabs, formation slots, and followers.
- **Phase 4 — Synchronized marching**: shared timing and phase A/B rows.
- **Phase 5 — Priests and Ark**: priests, Ark carriers, and stable Ark attachments.
- **Phase 6 — Jericho environment**: terrain, walls, route, and ordered checkpoints.
- **Phase 7 — Missions and prototype UI**: reusable objectives and a one-circuit Jericho mission.
- **Phase 8 — Complete Jericho prototype**: integrate Joshua, procession, route, and mission sequence.
- **Phase 9 — Mobile controls and optimization**: only after desktop prototype succeeds.
- **Phase 10 — Additional missions**: only after Jericho is reliable and with per-mission approval.

**Authorization boundary:** Only Phase 2 is authorized now. This roadmap does not authorize work on later phases.