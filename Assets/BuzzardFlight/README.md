# Rigged buzzard

`BuzzardFlying.prefab` is the new flock asset. The original BUZZARD model and colored prefab remain separate.

- 15-bone skin: body, neck/head, two wing chains, two leg/foot chains, and tail.
- Animation clips: Fly, Glide, Land, TakeOff, Perch, Hop.
- Source texture copied unchanged; the game mesh has 52,759 vertices / 106,106 triangles, down from 530,236 source vertices.
- Editable Blender master: `ArtSource/Buzzard/BuzzardRigged.blend` in the project root.

The scene's BuzzardFlock references this prefab. All 18 birds orbit the moving procession in world space, including Joshua, soldiers, priests and the Ark carriers. The radius grows with the group's spread plus a nine-metre clearance. Two birds alternate between circling and ground visits. Landing spots must be visible, on flat ground 12–35 metres from Joshua, and at least nine metres from every troop member, with room for a hop. The first visits are attempted after about 6 and 15 seconds; birds retry if there is no suitable visible spot. Each visit lands, pauses, hops, takes off and rejoins the current orbit.

The clips animate the body in place. BuzzardFlock provides movement through the scene and hop height. The rig uses Unity's Legacy Animation component for these non-humanoid clips.

Validation logs: `Logs/bird-rig-test.txt` and `Logs/flock-test.txt`. The Unity verification menus are under Tools / Jericho Prototype. Camera captures are in `Logs/BuzzardWork`.
