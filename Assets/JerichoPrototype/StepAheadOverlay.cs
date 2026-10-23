using UnityEngine;

namespace JerichoPrototype
{
    public sealed class StepAheadOverlay : MonoBehaviour
    {
        bool open = true;
        void TriggerFinale() { SendMessage("TestFinishDay", true, SendMessageOptions.DontRequireReceiver); }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.E)) open = !open;
            if (Input.GetKeyDown(KeyCode.F7)) TriggerFinale();
        }
        void OnGUI()
        {
            GUI.enabled = true;
            GUI.color = Color.black;
            if (GUI.Button(new Rect(20, 20, 210, 34), open ? "Hide step menu (E)" : "Show step menu (E)")) open = !open;
            if (!open) return;
            GUI.Box(new Rect(20, 62, 300, 160), "STEP AHEAD");
            GUI.Label(new Rect(38, 98, 260, 25), "E: hide/show this menu");
            GUI.Label(new Rect(38, 124, 260, 25), "F7: skip to walls falling");
            if (GUI.Button(new Rect(38, 158, 260, 34), "Final circuit -> walls fall")) TriggerFinale();
        }
    }
}
