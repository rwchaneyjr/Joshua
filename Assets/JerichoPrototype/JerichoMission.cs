using UnityEngine;
namespace JerichoPrototype
{
    public sealed class JerichoMission : MonoBehaviour
    {
        public Transform player;
        public Transform[] checkpoints;
        public float checkpointRadius = 4;
        public int CompletedCheckpoints { get; private set; }
        public bool Complete => CompletedCheckpoints == checkpoints.Length;
        void Update()
        {
            if (!player || Complete) return;
            Vector3 delta = player.position - checkpoints[CompletedCheckpoints].position; delta.y = 0;
            if (delta.magnitude <= checkpointRadius) CompletedCheckpoints++;
        }
        void OnGUI()
        {
            GUI.Label(new Rect(30, 15, 400, 25), "MISSION: JERICHO");
            GUI.Label(new Rect(30, 43, 400, 135), "Lead the procession around Jericho.\nCircuit: " + (Complete ? "1" : "0") + " / 1\nJoshua 6\n" +
                (Complete ? "OBJECTIVE COMPLETE" : "Next checkpoint: " + (CompletedCheckpoints + 1) + " / 6") +
                "\nWASD move | Shift run | Click then mouse: camera\nEsc: release mouse | Follow the numbered route signs");
        }
    }
}
