using UnityEngine;
using UnityEngine.SceneManagement;

namespace JerichoPrototype
{
    public sealed class TrumpetSearchGame : MonoBehaviour
    {
        public Camera searchCamera;
        public TrumpetCollectible[] trumpets;
        public int Found { get; private set; }
        public bool Complete => trumpets != null && trumpets.Length == 7 && Found == 7;
        public bool InstructionsOpen { get; private set; } = true;
        GUIStyle panel, label, title;
        Texture2D paper;
        int activeClue = -1;
        string feedback = "";
        static readonly string[] Questions = {
            "How many priests carried trumpets before the Ark? (Joshua 6:4)",
            "The trumpets were made from which animal's horns? (Joshua 6:4)",
            "How many times each day did they march during the first six days? (Joshua 6:3)",
            "How many times did they circle the city on the seventh day? (Joshua 6:4)",
            "What were the people to do after the long trumpet blast? (Joshua 6:5)",
            "What sacred object did the priests carry? (Joshua 6:6)",
            "Whose household was rescued from Jericho? (Joshua 6:22–23)"
        };
        static readonly string[][] Answers = {
            new[] { "Three", "Seven", "Twelve" }, new[] { "Rams", "Deer", "Oxen" },
            new[] { "Seven times", "Twice", "Once" }, new[] { "Seven times", "Six times", "Once" },
            new[] { "Sit down", "Shout", "Return to camp" }, new[] { "A throne", "A stone tablet", "The Ark of the Covenant" },
            new[] { "Rahab's", "Pharaoh's", "Goliath's" }
        };
        static readonly int[] Correct = { 1, 0, 2, 0, 1, 2, 0 };
        public bool AnswerClue(int clue, int answer)
        {
            if (InstructionsOpen || Complete || trumpets == null || clue < 0 || clue >= Mathf.Min(7, trumpets.Length) ||
                !trumpets[clue] || trumpets[clue].Collected || trumpets[clue].Revealed) return false;
            if (answer != Correct[clue]) { feedback = "Not quite. Try again! Read the verse reference for a hint."; return false; }
            trumpets[clue].Reveal(); feedback = "Correct! Find the glowing horn in the courtyard and click it.";
            return true;
        }
        void Start() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        void Update()
        {
            if (InstructionsOpen || Complete || activeClue >= 0 || !searchCamera) return;
            bool clicked = Input.GetMouseButtonDown(0);
            Vector2 point = Input.mousePosition;
            if (Input.touchCount > 0) { var touch = Input.GetTouch(0); clicked = touch.phase == TouchPhase.Began; point = touch.position; }
            float scale = Mathf.Min(Screen.width / 1000f, Screen.height / 700f);
            float guiY = (Screen.height - point.y - (Screen.height - 700 * scale) * .5f) / scale;
            if (!clicked || guiY < 100 || guiY > 580) return;
            var ray = searchCamera.ScreenPointToRay(point);
            if (Physics.Raycast(ray, out var hit, 100f))
            {
                var trumpet = hit.collider.GetComponentInParent<TrumpetCollectible>();
                if (trumpet) Collect(trumpet);
            }
        }
        public void BeginSearch() { InstructionsOpen = false; }
        public void ContinueToMarch()
        {
            if (!Complete) return;
            SceneManager.LoadScene("Assets/Scenes/JerichoPrototype.unity", LoadSceneMode.Single);
        }
        public bool Collect(TrumpetCollectible trumpet)
        {
            if (InstructionsOpen || Complete || !trumpet || !trumpet.Revealed || trumpet.owner != this || trumpet.Collected ||
                trumpets == null || System.Array.IndexOf(trumpets, trumpet) < 0) return false;
            trumpet.MarkCollected(); Found++; return true;
        }
        public void RestartSearch()
        {
            Found = 0; InstructionsOpen = true; activeClue = -1; feedback = "";
            foreach (var trumpet in trumpets) if (trumpet) trumpet.ResetCollectible();
        }
        void OnGUI()
        {
            if (panel == null)
            {
                paper = new Texture2D(1,1); paper.SetPixel(0,0,Color.white); paper.Apply();
                panel = new GUIStyle(GUI.skin.box); panel.normal.background = paper;
                label = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 18 };
                title = new GUIStyle(label) { fontSize = 24, fontStyle = FontStyle.Bold };
                label.normal.textColor = title.normal.textColor = Color.black;
            }
            var original = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1000f, Screen.height / 700f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width-1000*scale)*.5f,(Screen.height-700*scale)*.5f,0),Quaternion.identity,Vector3.one*scale);
            using (BlackGameText.Scope())
            {
                GUI.Box(new Rect(15,15,970,80),"",panel);
                GUI.Label(new Rect(35,25,550,32),"Find the seven trumpets",title);
                GUI.Label(new Rect(35,59,690,28),"Solve a clue below, then click its glowing horn in the courtyard.",label);
                GUI.Label(new Rect(740,36,230,32),"Trumpets Found: " + Found + "/7",label);
                if (!InstructionsOpen && !Complete && activeClue < 0)
                {
                    GUI.Box(new Rect(15,590,970,95), "", panel);
                    GUI.Label(new Rect(35,596,920,28), "Bible clues — each correct answer reveals one hidden horn", label);
                    for (int i = 0; i < Mathf.Min(7, trumpets.Length); i++)
                    {
                        GUI.enabled = trumpets[i] && !trumpets[i].Collected;
                        string status = trumpets[i] && trumpets[i].Collected ? "Collected" : trumpets[i] && trumpets[i].Revealed ? "Revealed" : "Clue " + (i + 1);
                        if (GUI.Button(new Rect(30 + i * 135, 633, 125, 38), status)) { activeClue = i; feedback = trumpets[i].Revealed ? "Find the glowing horn and click it to collect it." : ""; }
                    }
                    GUI.enabled = true;
                }
                if (activeClue >= 0)
                {
                    GUI.Box(new Rect(190,160,620,390), "", panel);
                    GUI.Label(new Rect(215,180,570,35), "Bible clue " + (activeClue + 1), title);
                    GUI.Label(new Rect(215,225,570,70), Questions[activeClue], label);
                    GUI.enabled = !trumpets[activeClue].Revealed;
                    for (int a = 0; a < 3; a++)
                        if (GUI.Button(new Rect(215,300+a*42,570,36), Answers[activeClue][a])) AnswerClue(activeClue,a);
                    GUI.enabled = true;
                    GUI.Label(new Rect(215,432,570,55), feedback, label);
                    if (GUI.Button(new Rect(360,500,280,35), "Back to courtyard")) activeClue = -1;
                }
                if (InstructionsOpen || Complete)
                {
                    GUI.Box(new Rect(230,225,540,240),"",panel);
                    GUI.Label(new Rect(255,245,490,40),Complete ? "All seven trumpets found!" : "Prepare the procession",title);
                    GUI.Label(new Rect(255,300,490,90),Complete ? "The seven trumpets are ready. Join Joshua and lead the procession around Jericho." : "Seven horns are concealed in the courtyard. Solve each Bible clue to reveal a glowing horn, then click it to collect it. Wrong answer? Try again!",label);
                    if (Complete)
                    {
                        if (GUI.Button(new Rect(255,410,260,35),"Continue to the march")) ContinueToMarch();
                        if (GUI.Button(new Rect(535,410,210,35),"Search again")) RestartSearch();
                    }
                    else if (GUI.Button(new Rect(385,410,230,35),"Begin search")) BeginSearch();
                }
            }
            GUI.matrix = original;
        }
        void OnDestroy() { if (paper) Destroy(paper); }
    }
}
