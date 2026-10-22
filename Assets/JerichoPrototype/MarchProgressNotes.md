# March progress and checkpoint zones

The march uses checkpoint proximity in numerical order (1 through 6). Each zone has a minimum horizontal radius of 18 metres; height does not affect arrival. There is no solid collider to block the troops. The HUD shows the next required checkpoint.

The sun advances between the checkpoints. Days 1–6 each end after checkpoint 6, followed by horns and the camp/night/morning sequence. Day 7 requires seven circuits within the same day.

Progress saves locally using Unity PlayerPrefs (`Jericho.MarchProgress.v1`) at each checkpoint, after camp transitions, and on normal application pause/quit. Continue restores day, circuits, checkpoint, actor poses, formation heading and camera heading. Start over removes this game's save. Saves are local to the device/browser and are not GitHub/cloud saves.

An interrupted horn/camp transition resumes from the last safe checkpoint, so an unfinished ceremony may replay. This avoids skipping a day if the game closes during the fade.

Verification logs: `Logs/checkpoint-save-test.txt` and `Logs/march-camp-test.txt`. The latter moves the actual player through the six scene checkpoint zones and checks sun advancement and the camp/day-two handoff. Test runs disable real autosaving to preserve existing player progress.

Testing: press F8 or click Testing / F8. Next checkpoint moves the group into the next zone. Finish this day advances to its final circuit. Final circuit jumps to day seven's last checkpoint and runs the horns and city collapse. Shortcuts disable autosaving until Play Mode/game restart. Completed saves retain fallen walls; older completed saves without the wallsFallen field play the missing collapse when continued.

