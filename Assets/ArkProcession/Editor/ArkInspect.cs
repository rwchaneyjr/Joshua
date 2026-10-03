using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
namespace ArkProcession.Editor {
public static class ArkInspect {
 [InitializeOnLoadMethod] static void Init(){ EditorApplication.delayCall += Run; }
 static void Run(){ if(!File.Exists(".utmp/ark-inspect"))return; File.Delete(".utmp/ark-inspect");
 var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ArkProcession/Ark.fbx"));
 go.transform.position=Vector3.up*100; var rs=go.GetComponentsInChildren<Renderer>(); var b=rs[0].bounds; foreach(var r in rs)b.Encapsulate(r.bounds);
 File.WriteAllText("Logs/ark-bounds.txt",b.ToString("F4")+"\n"+string.Join("\n",go.GetComponentsInChildren<MeshFilter>().Select(m=>m.name+" "+m.sharedMesh.vertexCount+" "+m.sharedMesh.bounds.ToString("F4"))));
 var mat=new Material(Shader.Find("Standard"));mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ArkProcession/Ark.png");foreach(var r in rs)r.sharedMaterial=mat;
 var cam=new GameObject("preview").AddComponent<Camera>();cam.transform.position=b.center+new Vector3(1,0.9f,1.3f)*b.size.magnitude*.65f;cam.transform.LookAt(b.center);cam.nearClipPlane=.001f;cam.farClipPlane=10000;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.gray;
 var light=new GameObject("light").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(50,-30,0);
 var rt=new RenderTexture(1000,800,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var img=new Texture2D(1000,800);img.ReadPixels(new Rect(0,0,1000,800),0,0);img.Apply();File.WriteAllBytes("Logs/ark-inspect.png",img.EncodeToPNG());RenderTexture.active=null;Object.DestroyImmediate(img);Object.DestroyImmediate(cam.gameObject);Object.DestroyImmediate(light.gameObject);Object.DestroyImmediate(go);Object.DestroyImmediate(mat);Object.DestroyImmediate(rt);
 }
}}

