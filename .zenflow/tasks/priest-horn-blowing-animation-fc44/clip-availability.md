# Priest horn asset inspection

- **Humanoid priest avatar**: Available. `Assets/JerichoPrototype/Priest.prefab` contains a valid humanoid Animator, and the source model defines both LeftHand and RightHand.
- **Existing runtime clips**: Available. The priest controller has `IDLE` and `WALK`; the supplied uncommitted controller change points both states at full Humanoid variants (not `.001` generic variants). These assets are left unchanged.
- **Dedicated horn/blow/raise clip**: Not available. Available source motion assets are idle, walking, running, high-kick, and carry-heavy-cannon; none is a horn-blowing action.
- **Horn setup**: Available. The prefab contains `Ceremonial Horn`, instanced from `Assets/CHARACTERS/HORN.fbx`, parented under the priest rig with manually serialized local position, rotation, scale, and material setup. The implementation will not alter those serialized transforms or materials.
- **Horn audio**: Not available. No supported audio assets (`.wav`, `.mp3`, `.ogg`, `.aiff`, `.aif`) were found under `Assets`; the behavior will expose an unassigned AudioClip field.
- **Approach**: Add a new reusable runtime component that uses Animator Humanoid IK, keeping the horn's saved parenting intact, and suppress movement/marching for the requested sequence.