using UnityEngine;
using UnityEngine.UI;

namespace JerichoPrototype
{
    [ExecuteAlways]
    public sealed class StepAheadOverlay : MonoBehaviour
    {
        bool open = true;
        JerichoMission mission;
        Canvas canvas;
        Text progressLabel;
        void Awake() { mission = GetComponent<JerichoMission>(); EnsureCanvas(); }
        void OnEnable() { mission = GetComponent<JerichoMission>(); EnsureCanvas(); }
        void Start() { EnsureCanvas(); }
        void EnsureCanvas()
        {
            if (canvas) return;
            var existing = FindObjectOfType<Canvas>();
            if (existing && existing.name == "Step Ahead Canvas") { canvas = existing; return; }
            BuildCanvas();
        }
        void TriggerFinale() { SendMessage("TestFinishDay", true, SendMessageOptions.DontRequireReceiver); }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.E)) open = !open;
            // Keep the runtime Canvas alive; the mission HUD remains visible even
            // while the step menu is toggled with E.
            if (canvas) canvas.enabled = true;
            if (progressLabel) progressLabel.gameObject.SetActive(true);
            if (progressLabel && mission)
                progressLabel.text = "MISSION: JERICHO - DAY " + mission.CurrentDay + " / 7\n" +
                    "LAPS / CIRCUITS: " + mission.CurrentCircuit + " / " + mission.RequiredCircuits + "\n" +
                    "CHECKPOINTS: " + mission.CompletedCheckpoints + " / 6\n\n" +
                    "STEP AHEAD\nE: hide/show menu\nF7: walls fall";
            if (Input.GetKeyDown(KeyCode.F7)) TriggerFinale();
        }

        void BuildCanvas()
        {
            var root = new GameObject("Step Ahead Canvas");
            root.transform.SetParent(null, false);
            root.name = Application.isPlaying ? "Step Ahead Canvas (Runtime)" : "Step Ahead Canvas";
            root.SetActive(true);
            canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            root.AddComponent<CanvasScaler>();
            root.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Step Ahead Panel");
            panel.transform.SetParent(root.transform, false);
            var image = panel.AddComponent<RawImage>();
            image.texture = Texture2D.whiteTexture;
            image.color = new Color(1f, 1f, 1f, 0f);
            image.enabled = false;
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(20, -20);
            rect.sizeDelta = new Vector2(360, 190);

            var text = new GameObject("Step Ahead Text");
            text.transform.SetParent(panel.transform, false);
            var label = text.AddComponent<Text>();
            progressLabel = label;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 22; label.color = Color.black; label.alignment = TextAnchor.UpperLeft;
            var labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0, 1); labelRect.anchorMax = new Vector2(1, 1);
            labelRect.pivot = new Vector2(.5f, 1); labelRect.anchoredPosition = new Vector2(0, -12);
            labelRect.sizeDelta = new Vector2(-24, 170);
            panel.SetActive(true);
            text.SetActive(true);
            canvas.enabled = true;
            Canvas.ForceUpdateCanvases();
        }
        void OnGUI()
        {
            // Draw the always-visible progress text above the save/continue window.
            GUI.depth = -10000;
            GUI.enabled = true;
            GUI.color = Color.black;
            int day = mission ? mission.CurrentDay : 1;
            int circuit = mission ? mission.CurrentCircuit : 0;
            int required = mission ? mission.RequiredCircuits : 7;
            int checkpoint = mission ? mission.CompletedCheckpoints : 0;
            GUI.Label(new Rect(20, 8, 520, 24), "MISSION: JERICHO - DAY " + day + " / 7");
            GUI.Label(new Rect(20, 34, 520, 24), "CIRCUITS: " + circuit + " / " + required + "    CHECKPOINTS: " + checkpoint + " / 6");
            GUI.Label(new Rect(20, 34, 500, 24), "LAPS / CIRCUITS: " + circuit + " / " + required + "    CHECKPOINTS: " + checkpoint + " / 6");
            if (GUI.Button(new Rect(20, 64, 210, 34), open ? "Hide step menu (E)" : "Show step menu (E)")) open = !open;
            if (!open) return;
            GUI.Box(new Rect(20, 106, 300, 160), "STEP AHEAD");
            GUI.Label(new Rect(38, 142, 260, 25), "E: hide/show this menu");
            GUI.Label(new Rect(38, 168, 260, 25), "F7: skip to walls falling");
            if (GUI.Button(new Rect(38, 202, 260, 34), "Final circuit -> walls fall")) TriggerFinale();
        }
    }
}
