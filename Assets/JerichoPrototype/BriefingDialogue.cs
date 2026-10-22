using UnityEngine;

namespace JerichoPrototype
{
    /// <summary>One-time opening briefing, independent of any NPC.</summary>
    public sealed class BriefingDialogue : MonoBehaviour
    {
        public string speakerName = "Mission Briefing";
        [TextArea] public string[] pages = {
            "Gameplay paraphrase — Joshua 6.\n\nLead the procession around Jericho through six checkpoints in order.",
            "March once each day for six days, then seven times on day seven. Return to camp each evening."
        };
        public Transform player;
        public JoshuaController controller;
        public ThirdPersonCamera cameraController;
        int page;
        bool open;
        bool controllerWasEnabled, cameraWasEnabled;
        public bool IsOpen => open;
        public bool PuzzleOpen { get; private set; }
        public MarchingInstructionsPuzzle Puzzle { get; } = new MarchingInstructionsPuzzle();
        GUIStyle panel, card, heading, body;
        Texture2D paper;
        readonly int[] cardDisplayOrder = { 2, 0, 3, 1 };

        void Start()
        {
            // Older saved scenes still contain the former one-lap briefing.
            if (pages != null)
                for (int i = 0; i < pages.Length; i++)
                    if (pages[i] != null && pages[i].Contains("one-circuit objective"))
                        pages[i] = "March once each day for six days, then seven times on day seven. Return to camp each evening.";
            open = true;
            PuzzleOpen = pages == null || pages.Length == 0;
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
            if (!open || PuzzleOpen) return;
            if (Input.GetKeyDown(KeyCode.Escape)) Close();
            else if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) Next();
        }

        public void Next()
        {
            if (!open || PuzzleOpen) return;
            if (page + 1 < pages.Length) page++;
            else Close();
        }

        public void Close()
        {
            if (!open) return;
            PuzzleOpen = true;
        }

        public void BeginMarch()
        {
            if (!open || !PuzzleOpen || !Puzzle.Solved) return;
            RestoreControls();
        }

        void RestoreControls()
        {
            if (!open) return;
            open = false;
            PuzzleOpen = false;
            if (controller) controller.enabled = controllerWasEnabled;
            if (cameraController) cameraController.enabled = cameraWasEnabled;
        }

        void OnDisable() { RestoreControls(); }
        void OnDestroy() { if (paper) Destroy(paper); }

        void OnGUI()
        {
            if (!open) return;
            if (PuzzleOpen) { DrawPuzzle(); return; }
            using (BlackGameText.Scope())
            {
                GUI.Box(new Rect(30, 220, 600, 180), speakerName);
                GUI.Label(new Rect(50, 255, 560, 90), pages[page]);
                if (GUI.Button(new Rect(390, 360, 105, 28), page + 1 < pages.Length ? "Next" : "Instructions")) Next();
                if (GUI.Button(new Rect(500, 360, 110, 28), "Skip briefing")) Close();
            }
        }

        void DrawPuzzle()
        {
            var previous = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 960f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width-960*scale)/2, (Screen.height-720*scale)/2,0), Quaternion.identity,Vector3.one*scale);
            using (BlackGameText.Scope())
            {
                if (panel == null)
                {
                    paper = new Texture2D(1,1); paper.SetPixel(0,0,new Color(.96f,.89f,.72f)); paper.Apply();
                    panel = new GUIStyle(GUI.skin.box); panel.normal.background = paper;
                    body = new GUIStyle(GUI.skin.label) { fontSize=19, wordWrap=true };
                    heading = new GUIStyle(body) { fontSize=27, fontStyle=FontStyle.Bold };
                    card = new GUIStyle(GUI.skin.button) { fontSize=18, wordWrap=true, alignment=TextAnchor.MiddleLeft, padding=new RectOffset(14,12,5,5) };
                }
                GUI.Box(new Rect(30,20,900,680),"",panel);
                GUI.Label(new Rect(55,38,850,40),"Prepare to march • Joshua 6",heading);
                GUI.Label(new Rect(55,82,850,48),"Put the four instructions in order. Click a placed card to return it and change your answer.",body);
                for(int i=0;i<4;i++)
                {
                    string text=(i+1)+".  "+(i<Puzzle.Order.Count ? MarchingInstructionsPuzzle.Cards[Puzzle.Order[i]] : "Choose a card below…");
                    GUI.enabled=!Puzzle.Solved && i<Puzzle.Order.Count;
                    if(GUI.Button(new Rect(55,140+i*57,850,50),text,card)) Puzzle.Remove(i);
                }
                GUI.enabled=true;
                GUI.Label(new Rect(55,375,850,28),"Available cards",body);
                for(int i=0;i<4;i++)
                {
                    int id=cardDisplayOrder[i]; GUI.enabled=!Puzzle.Solved && !Puzzle.Order.Contains(id);
                    if(GUI.Button(new Rect(55+(i%2)*435,410+(i/2)*68,415,60),MarchingInstructionsPuzzle.Cards[id],card))Puzzle.Select(id);
                }
                GUI.enabled=true;
                GUI.Label(new Rect(55,550,850,58),Puzzle.Feedback,body);
                if(Puzzle.Solved)
                {
                    if(GUI.Button(new Rect(625,630,280,44),"Begin march",card))BeginMarch();
                }
                else
                {
                    if(GUI.Button(new Rect(55,630,180,44),"Start over",card))Puzzle.Reset();
                    GUI.enabled=Puzzle.Order.Count==4;
                    if(GUI.Button(new Rect(625,630,280,44),"Check Order",card))Puzzle.Check();
                    GUI.enabled=true;
                }
            }
            GUI.matrix=previous;
        }
    }
}

