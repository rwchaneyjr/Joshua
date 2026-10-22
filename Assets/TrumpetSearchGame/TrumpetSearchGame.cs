using UnityEngine;

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
        void Start() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        void Update()
        {
            if (InstructionsOpen || Complete || !searchCamera) return;
            bool clicked = Input.GetMouseButtonDown(0);
            Vector2 point = Input.mousePosition;
            if (Input.touchCount > 0) { var touch = Input.GetTouch(0); clicked = touch.phase == TouchPhase.Began; point = touch.position; }
            if (!clicked || point.y > Screen.height - 100) return;
            var ray = searchCamera.ScreenPointToRay(point);
            if (Physics.Raycast(ray, out var hit, 100f))
            {
                var trumpet = hit.collider.GetComponentInParent<TrumpetCollectible>();
                if (trumpet) Collect(trumpet);
            }
        }
        public void BeginSearch() { InstructionsOpen = false; }
        public bool Collect(TrumpetCollectible trumpet)
        {
            if (InstructionsOpen || Complete || !trumpet || trumpet.owner != this || trumpet.Collected ||
                trumpets == null || System.Array.IndexOf(trumpets, trumpet) < 0) return false;
            trumpet.MarkCollected(); Found++; return true;
        }
        public void RestartSearch()
        {
            Found = 0; InstructionsOpen = true;
            foreach (var trumpet in trumpets) if (trumpet) trumpet.ResetCollectible();
        }
        void OnGUI()
        {
            if (panel == null)
            {
                paper = new Texture2D(1,1); paper.SetPixel(0,0,new Color(.96f,.89f,.70f)); paper.Apply();
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
                GUI.Label(new Rect(35,59,650,28),"Click or tap a horn. Look around the crates, pillars, and scroll table.",label);
                GUI.Label(new Rect(740,36,230,32),"Trumpets Found: " + Found + "/7",label);
                if (InstructionsOpen || Complete)
                {
                    GUI.Box(new Rect(230,225,540,240),"",panel);
                    GUI.Label(new Rect(255,245,490,40),Complete ? "All seven trumpets found!" : "Prepare the procession",title);
                    GUI.Label(new Rect(255,300,490,90),Complete ? "The trumpet search is complete. The procession-order puzzle is the next development phase." : "Find seven ram’s-horn trumpets in this courtyard.\nThis search is an educational game activity inspired by Joshua 6.",label);
                    if (GUI.Button(new Rect(385,410,230,35),Complete ? "Search again" : "Begin search")) { if (Complete) RestartSearch(); else BeginSearch(); }
                }
            }
            GUI.matrix = original;
        }
        void OnDestroy() { if (paper) Destroy(paper); }
    }
}
