using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace JerichoPrototype.Editor
{
    public static class BuzzardRigVerification
    {
        [InitializeOnLoadMethod] static void Request()
        {
            EditorApplication.delayCall+=()=>{
                if(!File.Exists(".utmp/bird-rig-test") || EditorApplication.isPlayingOrWillChangePlaymode)return;
                File.Delete(".utmp/bird-rig-test");Run();
            };
        }
        [MenuItem("Tools/Jericho Prototype/Verify Buzzard Rig")]
        public static void Run()
        {
            GameObject root=null;Camera camera=null;GameObject lamp=null;
            try
            {
                root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BuzzardFlight/BuzzardFlying.prefab"));
                root.transform.position=new Vector3(1000,1000,1000);
                var motion=root.GetComponent<BuzzardAnimation>();var model=motion.clips.gameObject;
                var skin=root.GetComponentInChildren<SkinnedMeshRenderer>();
                if(!skin || skin.bones.Length<10)throw new Exception("Missing skin/bones");
                var mesh=new Mesh();motion.clips["Fly"].clip.SampleAnimation(model,0);skin.BakeMesh(mesh);var before=mesh.vertices;
                motion.clips["Fly"].clip.SampleAnimation(model,.25f);skin.BakeMesh(mesh);var after=mesh.vertices;
                float max=0;for(int i=0;i<before.Length;i++)max=Mathf.Max(max,Vector3.Distance(before[i],after[i]));
                if(max<.01f)throw new Exception("Wing animation is not deforming the mesh");UnityEngine.Object.DestroyImmediate(mesh);
                camera=new GameObject("Bird preview camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.24f,.32f,.40f);camera.nearClipPlane=.01f;camera.farClipPlane=30;
                lamp=new GameObject("Bird preview light");var light=lamp.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(40,-30,0);
                foreach(string name in new[]{"Fly","Glide","Land","TakeOff","Perch","Hop"})
                {
                    var clip=motion.clips[name].clip;
                    if(clip.length<=0)throw new Exception("Empty clip "+name);
                    clip.SampleAnimation(model,name=="Land"?clip.length:name=="TakeOff"?clip.length:.25f);
                    var center=root.transform.position;
                    camera.transform.position=center+new Vector3(2.6f,1.4f,3.2f);camera.transform.LookAt(center);camera.fieldOfView=34;
                    var rt=new RenderTexture(1000,800,24);var old=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                    var image=new Texture2D(1000,800);image.ReadPixels(new Rect(0,0,1000,800),0,0);image.Apply();File.WriteAllBytes("Logs/BuzzardWork/unity-"+name+".png",image.EncodeToPNG());
                    camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);
                }
                File.WriteAllText("Logs/bird-rig-test.txt","PASS: "+skin.bones.Length+" bones; six nonempty clips; animated mesh displacement "+max+"; Unity previews saved.");
            }
            catch(Exception e){File.WriteAllText("Logs/bird-rig-test.txt",e.ToString());Debug.LogException(e);}
            finally{if(root)UnityEngine.Object.DestroyImmediate(root);if(camera)UnityEngine.Object.DestroyImmediate(camera.gameObject);if(lamp)UnityEngine.Object.DestroyImmediate(lamp);}
        }
    }
}
