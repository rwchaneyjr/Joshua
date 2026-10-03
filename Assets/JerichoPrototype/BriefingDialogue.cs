using UnityEngine;

namespace JerichoPrototype
{
    /// <summary>One-time opening briefing, independent of any NPC.</summary>
    public sealed class BriefingDialogue : MonoBehaviour
    {
        public string speakerName = "Mission Briefing";
        [TextArea] public string[] pages = {
            "Gameplay paraphrase — Joshua 6.\n\nLead the procession around Jericho through six checkpoints in order.",
            "The one-circuit objective is a gameplay simplification. Follow the numbered route signs."
        };
        public Transform player;
        public JoshuaController controller;
        public ThirdPersonCamera cameraController;
        int page;
        bool open;
        bool controllerWasEnabled, cameraWasEnabled;
        public bool IsOpen => open;

        void Start()
        {
            if (pages == null || pages.Length == 0) return;
            open = true;
            page = 0;
            controllerWasEnabled = controller && controller.enabled;
            cameraWasEnabled = cameraController && cameraController.enabled;
            if (controller) controller.enabled = false;
            if (cameraController) cameraController.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Update()
        {
            if (!open) return;
            if (Input.GetKeyDown(KeyCode.Escape)) Close();
            else if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) Next();
        }

        public void Next()
        {
            if (!open) return;
            if (page + 1 < pages.Length) page++;
            else Close();
        }

        public void Close()
        {
            if (!open) return;
            open = false;
            if (controller) controller.enabled = controllerWasEnabled;
            if (cameraController) cameraController.enabled = cameraWasEnabled;
        }

        void OnDisable() { Close(); }

        void OnGUI()
        {
            if (!open) return;
            using (BlackGameText.Scope())
            {
                GUI.Box(new Rect(30, 220, 600, 180), speakerName);
                GUI.Label(new Rect(50, 255, 560, 90), pages[page]);
                if (GUI.Button(new Rect(420, 360, 80, 28), page + 1 < pages.Length ? "Next" : "Begin")) Next();
                if (GUI.Button(new Rect(510, 360, 80, 28), "Close")) Close();
            }
        }
    }
}
