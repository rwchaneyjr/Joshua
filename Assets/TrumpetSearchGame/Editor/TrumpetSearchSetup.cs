using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JerichoPrototype.Editor
{
    public static class TrumpetSearchSetup
    {
        const string Folder = "Assets/TrumpetSearchGame/";
        [InitializeOnLoadMethod] static void CheckRequest()
        {
            EditorApplication.delayCall += () => {
                if (!File.Exists(".utmp/trumpet-search-setup")) return;
                File.Delete(".utmp/trumpet-search-setup");
                try { Build(); } catch (Exception ex) { Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/trumpet-search-setup.txt",ex.ToString()); Debug.LogException(ex); }
            };
        }
        [MenuItem("Tools/Jericho Prototype/Set Up TrumpetSearch")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play Mode first.");
            var scene = SceneManager.GetSceneByPath("Assets/TrumpetSearch.unity");
            if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/TrumpetSearch.unity",OpenSceneMode.Additive);
            if(scene.GetRootGameObjects().Any(g=>g.GetComponent<TrumpetSearchGame>()))throw new Exception("TrumpetSearch is already set up; keeping the existing scene intact.");
            var root=new GameObject("Trumpet Search Courtyard");SceneManager.MoveGameObjectToScene(root,scene);Undo.RegisterCreatedObjectUndo(root,"Set up trumpet courtyard");
            var game=root.AddComponent<TrumpetSearchGame>();
            var sand=Material("CourtyardSand",new Color(.70f,.57f,.37f));var stone=Material("CourtyardStone",new Color(.57f,.46f,.32f));var wood=Material("CrateWood",new Color(.27f,.13f,.055f));var gold=Material("TrumpetGold",new Color(.87f,.62f,.17f));var parchment=Material("ScrollPaper",new Color(.93f,.84f,.62f));
            Box(root.transform,"Courtyard floor",new Vector3(0,-.2f,0),new Vector3(24,.4f,22),sand);
            Box(root.transform,"Rear stone wall",new Vector3(0,1.2f,10),new Vector3(24,2.4f,.6f),stone);
            Box(root.transform,"Left stone wall",new Vector3(-11.7f,.65f,0),new Vector3(.6f,1.3f,20),stone);
            Box(root.transform,"Right stone wall",new Vector3(11.7f,.65f,0),new Vector3(.6f,1.3f,20),stone);
            Vector3[] spots={new Vector3(-8,.8f,-6),new Vector3(-3,.8f,-5),new Vector3(4,.8f,-6),new Vector3(8,.8f,-1),new Vector3(2,.8f,2),new Vector3(-6,.8f,3),new Vector3(6,.8f,6)};
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CHARACTERS/HORN.fbx");if(!asset)throw new Exception("HORN.fbx is missing.");
            game.trumpets=new TrumpetCollectible[7];
            for(int i=0;i<7;i++)
            {
                Box(root.transform,"Crate "+(i+1),spots[i]+new Vector3(-.8f,-.4f,.65f),new Vector3(1.3f,.8f,1.2f),wood);
                var holder=new GameObject("Hidden Trumpet "+(i+1));holder.transform.SetParent(root.transform,false);
                var visual=UnityEngine.Object.Instantiate(asset,holder.transform);visual.name="Horn visual";
                foreach(var collider in visual.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(collider);
                foreach(var helper in visual.GetComponentsInChildren<Transform>().Where(t=>t.name=="Icosphere").ToArray())UnityEngine.Object.DestroyImmediate(helper.gameObject);
                var renderers=visual.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers){bounds.Encapsulate(r.bounds);r.sharedMaterial=gold;}
                float scale=1.55f/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);visual.transform.localScale*=scale;visual.transform.localPosition-=bounds.center*scale;
                holder.transform.position=spots[i];holder.transform.rotation=Quaternion.Euler(0,i*43,0);
                var hit=holder.AddComponent<BoxCollider>();hit.size=new Vector3(1.8f,1.25f,1.8f);
                var collectible=holder.AddComponent<TrumpetCollectible>();collectible.owner=game;game.trumpets[i]=collectible;
            }
            for(int i=0;i<4;i++)Box(root.transform,"Weathered pillar "+(i+1),new Vector3(-9+i*6,1.1f,8.3f),new Vector3(.8f,2.2f,.8f),stone);
            Box(root.transform,"Scroll table",new Vector3(-1,.55f,6),new Vector3(2,1.1f,1.2f),wood);
            Box(root.transform,"Instruction scroll",new Vector3(-1,1.13f,6),new Vector3(1.5f,.04f,.8f),parchment);
            var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).FirstOrDefault();
            if(!camera){var go=new GameObject("Search Camera");SceneManager.MoveGameObjectToScene(go,scene);camera=go.AddComponent<Camera>();}
            Undo.RecordObject(camera.transform,"Frame search courtyard");Undo.RecordObject(camera,"Frame search courtyard");camera.transform.position=new Vector3(0,22,-17);camera.transform.LookAt(new Vector3(0,0,1));camera.orthographic=true;camera.orthographicSize=13.8f;camera.nearClipPlane=.1f;camera.farClipPlane=150;camera.backgroundColor=new Color(.36f,.48f,.57f);camera.clearFlags=CameraClearFlags.SolidColor;game.searchCamera=camera;
            var light=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>()).FirstOrDefault();if(light){light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(55,-30,0);EditorUtility.SetDirty(light);}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            // Test collection logic on a disposable clone, leaving the saved scene untouched.
            var copy=UnityEngine.Object.Instantiate(root);try{var test=copy.GetComponent<TrumpetSearchGame>();if(test.Collect(test.trumpets[0]))throw new Exception("Collected while instructions open");test.BeginSearch();foreach(var horn in test.trumpets){if(!test.Collect(horn))throw new Exception("Collection failed");if(test.Collect(horn))throw new Exception("Duplicate counted");}if(!test.Complete||test.Found!=7)throw new Exception("Completion failed");test.RestartSearch();if(test.Found!=0||test.Complete||test.trumpets.Any(h=>h.Collected||!h.gameObject.activeSelf))throw new Exception("Restart failed");}finally{UnityEngine.Object.DestroyImmediate(copy);}
            Directory.CreateDirectory("Logs");File.WriteAllText("Logs/trumpet-search-setup.txt","PASS: seven collectible horns; instructions gate, duplicate prevention, completion and restart verified. Scene saved at Assets/TrumpetSearch.unity.");
            Selection.activeGameObject=root;
        }
        static Material Material(string name,Color color){string path=Folder+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Standard")){color=color};AssetDatabase.CreateAsset(m,path);}return m;}
        static GameObject Box(Transform parent,string name,Vector3 position,Vector3 scale,Material material){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;return go;}
    }
}
