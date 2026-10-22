using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JerichoPrototype.Editor
{
    public static class GrandJerichoBuilder
    {
        const string Request = ".utmp/grand-jericho";
        static readonly List<Transform> pieces = new List<Transform>();
        static Transform root;
        static Material stone, pale, dark, roof, wood, blue;
        [InitializeOnLoadMethod] static void Initialize()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
                File.Delete(Request);
                try { Build(); } catch (Exception e) { File.WriteAllText("Logs/grand-jericho.txt", e.ToString()); Debug.LogException(e); }
            };
        }
        [MenuItem("Tools/Jericho Prototype/Upgrade City")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var scene = SceneManager.GetSceneByPath(PrototypeBuilder.ScenePath);
            if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(PrototypeBuilder.ScenePath, OpenSceneMode.Additive);
            var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
            if (all.Any(t => t.name == "Grand Jericho")) throw new InvalidOperationException("City already upgraded; preserving it.");
            Directory.CreateDirectory("Logs");
            File.Copy(PrototypeBuilder.ScenePath, "Logs/Jericho-before-city-upgrade.unity", true);
            root = new GameObject("Grand Jericho").transform; SceneManager.MoveGameObjectToScene(root.gameObject, scene);
            pieces.Clear();
            stone = Mat("City sandstone", new Color(.65f,.45f,.27f)); pale = Mat("City limestone",new Color(.87f,.71f,.47f));
            dark = Mat("City shadow stone",new Color(.40f,.28f,.18f)); roof = Mat("City terracotta",new Color(.57f,.25f,.13f));
            wood = Mat("City cedar",new Color(.22f,.115f,.055f)); blue = Mat("City indigo",new Color(.10f,.27f,.36f));
            foreach(var t in all.Where(t => new[]{"North wall","South wall","East wall","West wall"}.Contains(t.name))) t.gameObject.SetActive(false);
            var ground = all.First(t=>t.name=="Ground"); ground.localScale = new Vector3(240,1,240);
            // 64m square city, separate masonry blocks with visible recessed joints.
            for(int side=0;side<4;side++) for(int col=0;col<16;col++)
            {
                if(side==0 && (col==7 || col==8)) continue;
                float along=-30+col*4;
                for(int row=0;row<5;row++)
                {
                    Vector3 p = new Vector3(along,1+row*2,-32);
                    var q=Quaternion.Euler(0,side*90,0);
                    var b=Box("Wall masonry",q*p,new Vector3(3.94f,1.94f,2.6f),(row+col)%4==0?pale:stone,true);
                    b.rotation=q;
                }
                var top=Box("Battlement",Quaternion.Euler(0,side*90,0)*new Vector3(along,10.65f,-32),new Vector3(2,1.3f,2.7f),pale,true);top.rotation=Quaternion.Euler(0,side*90,0);
            }
            foreach(float x in new[]{-32f,32f}) foreach(float z in new[]{-32f,32f}) Tower(x,z,15);
            Tower(-7,-32,17);Tower(7,-32,17);
            Box("Gate lintel",new Vector3(0,9.6f,-32),new Vector3(10,2,3.8f),pale,true);
            Box("Cedar gate left",new Vector3(-1.9f,4,-32),new Vector3(3.7f,8,.5f),wood,true);
            Box("Cedar gate right",new Vector3(1.9f,4,-32),new Vector3(3.7f,8,.5f),wood,true);
            for(int i=0;i<3;i++)Box("Gate bronze band",new Vector3(0,1.5f+i*2.5f,-32.35f),new Vector3(7.5f,.22f,.12f),dark,true);
            // Raised inner citadel and varied flat-roofed dwellings, keeping the gate avenue open.
            Box("Citadel terrace",new Vector3(0,1,15),new Vector3(22,2,18),dark);
            Box("Citadel hall",new Vector3(0,7,17),new Vector3(15,10,11),pale);
            Box("Citadel cornice",new Vector3(0,12.3f,17),new Vector3(16, .6f,12),stone);
            for(int i=0;i<5;i++) Box("Citadel front pillar",new Vector3(-6+i*3,7,10.8f),new Vector3(.65f,10,.65f),stone);
            for(int i=0;i<8;i++) Box("Citadel steps",new Vector3(0,.12f+i*.24f,4+i*.65f),new Vector3(9,.24f+i*.48f,.75f),pale);
            for(int x=-2;x<=2;x++) for(int z=-2;z<=2;z++)
            {
                if(x==0 || (z>=1 && Mathf.Abs(x)<=1))continue;
                float h=3.5f+((x+z+8)%3)*1.3f;
                Vector3 p=new Vector3(x*10,h/2,z*9);
                Box("Courtyard dwelling",p,new Vector3(6,h,6),((x+z)%2==0)?pale:stone);
                Box("Roof coping",p+Vector3.up*(h/2+.15f),new Vector3(6.4f,.3f,6.4f),roof);
                Box("Doorway",new Vector3(p.x,1.15f,p.z-3.02f),new Vector3(1.2f,2.3f,.12f),wood);
                for(int w=-1;w<=1;w+=2)Box("Recessed window",new Vector3(p.x+w*1.9f,h*.7f,p.z-3.04f),new Vector3(.65f,.85f,.12f),dark);
            }
            var collapse=root.gameObject.AddComponent<CityWallCollapse>();collapse.pieces=pieces.ToArray();
            var mission=all.Select(t=>t.GetComponent<JerichoMission>()).First(m=>m);
            Vector3[] route={new Vector3(48,0,-48),new Vector3(48,0,0),new Vector3(48,0,48),new Vector3(-48,0,48),new Vector3(-48,0,-48),new Vector3(0,0,-48)};
            for(int i=0;i<mission.checkpoints.Length;i++)
            {
                mission.checkpoints[i].position=route[i];
                var sign=all.FirstOrDefault(t=>t.name=="Route sign "+(i+1));if(sign)sign.position=route[i]+Vector3.up*.03f;
            }
            Vector3 shift=new Vector3(0,mission.player.position.y,-48)-mission.player.position;
            mission.player.position+=shift;
            foreach(var f in all.Select(t=>t.GetComponent<ProcessionFollower>()).Where(f=>f))f.transform.position+=shift;
            foreach(var a in all.Select(t=>t.GetComponent<ArkProcession.ArkCarryRig>()).Where(a=>a))a.transform.position+=shift;
            foreach(var c in all.Select(t=>t.GetComponent<Camera>()).Where(c=>c)){c.farClipPlane=350;c.transform.position+=shift;}
            foreach(var t in all){EditorUtility.SetDirty(t);if(PrefabUtility.IsPartOfPrefabInstance(t))PrefabUtility.RecordPrefabInstancePropertyModifications(t);}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            // Save an overview image for visual inspection without changing the gameplay camera.
            var camera=new GameObject("Temporary city overview").AddComponent<Camera>();camera.transform.position=new Vector3(95,85,-105);camera.transform.LookAt(new Vector3(0,2,0));camera.farClipPlane=400;camera.fieldOfView=48;
            var rt=new RenderTexture(1400,1000,24);camera.targetTexture=rt;var prior=RenderTexture.active;
            camera.Render();RenderTexture.active=rt;var image=new Texture2D(1400,1000);image.ReadPixels(new Rect(0,0,1400,1000),0,0);image.Apply();File.WriteAllBytes("Logs/grand-jericho.png",image.EncodeToPNG());RenderTexture.active=prior;
            UnityEngine.Object.DestroyImmediate(camera.gameObject);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);
            File.WriteAllText("Logs/grand-jericho.txt","PASS: city saved; "+pieces.Count+" collapse pieces; 64m walls; 96m route; original walls retained inactive. Overview rendered.");
        }
        static void Tower(float x,float z,float height)
        {
            for(int i=0;i<(int)(height/2);i++) Box("Tower course",new Vector3(x,1+i*2,z),new Vector3(5.5f,1.94f,5.5f),i%3==0?pale:stone,true);
            for(int i=-1;i<=1;i++)foreach(int side in new[]{-1,1}){
                Box("Tower crown",new Vector3(x+i*2,height,z+side*2.2f),new Vector3(1.2f,1.6f,1.2f),pale,true);
                Box("Tower crown",new Vector3(x+side*2.2f,height,z+i*2),new Vector3(1.2f,1.6f,1.2f),pale,true);
            }
            Box("Indigo tower banner",new Vector3(x,height-4,z-2.81f),new Vector3(1.2f,3,.08f),blue,true);
        }
        static Transform Box(string name,Vector3 p,Vector3 s,Material m,bool fall=false)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root,false);go.transform.localPosition=p;go.transform.localScale=s;go.GetComponent<Renderer>().sharedMaterial=m;if(fall)pieces.Add(go.transform);return go.transform;
        }
        static Material Mat(string name,Color c)
        {
            string path="Assets/JerichoPrototype/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;
            m=new Material(Shader.Find("Standard")){color=c};m.SetFloat("_Glossiness",.12f);AssetDatabase.CreateAsset(m,path);return m;
        }
    }
}
