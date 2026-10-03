using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace ArkProcession.Editor {
public static class ArkCarryBuilder {
 const string Root="Assets/ArkProcession/";
 [InitializeOnLoadMethod] static void Init(){EditorApplication.delayCall+=()=>{if(!File.Exists(".utmp/ark-build"))return;File.Delete(".utmp/ark-build");try{Build();}catch(Exception ex){File.WriteAllText("Logs/ark-carry-verification.txt",ex.ToString());Debug.LogException(ex);}};}
 [MenuItem("Tools/Jericho Prototype/Build Four Priest Ark Preview")]
 public static void Build(){
 if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play Mode before building.");
 var previous=SceneManager.GetActiveScene();
 var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
 var group=new GameObject("Four Priest Ark Carry");var rig=group.AddComponent<ArkCarryRig>();
 var ark=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Ark.fbx"),group.transform);ark.name="Ark";
 // Measure the straight exposed pole shafts in imported world units before scaling.
 var mf=ark.GetComponentInChildren<MeshFilter>();var vertices=mf.sharedMesh.vertices.Select(v=>mf.transform.TransformPoint(v)).ToArray();
 var positive=vertices.Where(v=>v.x>.65f&&v.x<.8f&&v.z>0).ToArray();
 var negative=vertices.Where(v=>v.x>.65f&&v.x<.8f&&v.z<0).ToArray();
 if(positive.Length<10||negative.Length<10)throw new Exception("Cannot locate the two pole shafts.");
 Vector3 p=new Vector3(0,positive.Average(v=>v.y),positive.Average(v=>v.z));
 Vector3 n=new Vector3(0,negative.Average(v=>v.y),negative.Average(v=>v.z));
 const float scale=3f;
 ark.transform.localRotation=Quaternion.Euler(0,90,0)*ark.transform.localRotation;ark.transform.localScale*=scale;
 ark.transform.localPosition=new Vector3(0,1.12f-(p.y+n.y)*.5f*scale,0);rig.ark=ark.transform;
 var mat=AssetDatabase.LoadAssetAtPath<Material>(Root+"ArkGold.mat");if(!mat){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,Root+"ArkGold.mat");}mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Ark.png");mat.SetFloat("_Metallic",.65f);mat.SetFloat("_Glossiness",.55f);
 foreach(var r in ark.GetComponentsInChildren<Renderer>())r.sharedMaterial=mat;
 rig.priests=new Animator[4];rig.leftGrips=new Transform[4];rig.rightGrips=new Transform[4];
 for(int i=0;i<4;i++){
 float side=i%2==0?-1:1;float z=i<2?1.9f:-1.9f;float poleX=(side<0?n.z:p.z)*scale;
 var priest=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JerichoPrototype/Priest.prefab"),group.transform);priest.name="Ark Bearer "+(i+1);
 foreach(var horn in priest.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Ceremonial Horn"||t.name=="Icosphere").ToArray())UnityEngine.Object.DestroyImmediate(horn.gameObject); foreach(var c in priest.GetComponents<MonoBehaviour>())UnityEngine.Object.DestroyImmediate(c); var motor=priest.GetComponent<CharacterController>();if(motor)UnityEngine.Object.DestroyImmediate(motor); var a=priest.GetComponentInChildren<Animator>();a.Rebind();a.Play("IDLE",0,0);a.Update(0); var skin=priest.GetComponentInChildren<SkinnedMeshRenderer>();var baked=new Mesh();skin.BakeMesh(baked);var points=baked.vertices.Select(v=>skin.transform.position+skin.transform.rotation*v).ToArray();var bounds=new Bounds(points[0],Vector3.zero);foreach(var v in points)bounds.Encapsulate(v);UnityEngine.Object.DestroyImmediate(baked); var cloth=AssetDatabase.LoadAssetAtPath<Material>(Root+"PriestCloth.mat");if(!cloth){cloth=new Material(Shader.Find("Standard"));cloth.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/CHARACTERS/Meshy_AI_Desert_Sentinel_biped_texture_0.png");AssetDatabase.CreateAsset(cloth,Root+"PriestCloth.mat");}skin.sharedMaterial=cloth;
 File.AppendAllText("Logs/ark-debug.txt",$"bounds {bounds} skinScale {skin.transform.lossyScale} priestScale {priest.transform.localScale} head {a.GetBoneTransform(HumanBodyBones.Head).position}\n");float s=1.8f/bounds.size.y;priest.transform.localScale*=s;priest.transform.localPosition=new Vector3(poleX+side*.30f,-bounds.min.y*s,z);
 a.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/CHARACTERS/PRIEST.controller");a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;rig.priests[i]=a;File.AppendAllText("Logs/ark-debug.txt",$"After scale {s} position {priest.transform.position} head {a.GetBoneTransform(HumanBodyBones.Head).position}\n");
 for(int hand=0;hand<2;hand++){var t=new GameObject(hand==0?"Left grip":"Right grip").transform;t.SetParent(ark.transform,true);t.position=new Vector3(poleX,1.12f,z+(hand==0?-.14f:.14f));if(hand==0)rig.leftGrips[i]=t;else rig.rightGrips[i]=t;}
 a.Rebind();a.SetBool("Marching",true);a.Play("WALK",0,.15f);a.Update(0);
 }
 rig.Sample(.15f);foreach(var a in rig.priests)a.Update(0);rig.ApplyHands();
 PrefabUtility.SaveAsPrefabAsset(group,Root+"FourPriestArk.prefab");
 var ground=GameObject.CreatePrimitive(PrimitiveType.Plane);ground.name="Preview Ground";ground.transform.localScale=Vector3.one*100;
 var groundMat=AssetDatabase.LoadAssetAtPath<Material>(Root+"Sand.mat");if(!groundMat){groundMat=new Material(Shader.Find("Standard")){color=new Color(.40f,.32f,.21f)};AssetDatabase.CreateAsset(groundMat,Root+"Sand.mat");}ground.GetComponent<Renderer>().sharedMaterial=groundMat;
 var light=new GameObject("Sun").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(45,-35,0);light.shadows=LightShadows.Soft;
 RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.55f,.57f,.62f);
 var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.SetParent(group.transform);camera.transform.position=new Vector3(6,4.2f,7.5f);camera.transform.LookAt(new Vector3(0,1,0));camera.fieldOfView=42;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.16f,.20f,.27f);camera.gameObject.AddComponent<AudioListener>();
 foreach(var obj in scene.GetRootGameObjects())foreach(var t in obj.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;camera.cullingMask=1<<30;light.cullingMask=1<<30;
 float worst=0;float legMotion=0;var start=rig.priests[0].GetBoneTransform(HumanBodyBones.LeftUpperLeg).localRotation;
 for(int frame=0;frame<32;frame++){
 rig.Sample(frame/32f);foreach(var a in rig.priests)a.Update(0);rig.ApplyHands();
 legMotion=Mathf.Max(legMotion,Quaternion.Angle(start,rig.priests[0].GetBoneTransform(HumanBodyBones.LeftUpperLeg).localRotation));
 for(int i=0;i<4;i++){if(rig.twoHands || i%2==1)worst=Mathf.Max(worst,Vector3.Distance(rig.priests[i].GetBoneTransform(HumanBodyBones.LeftHand).position,rig.leftGrips[i].position));if(rig.twoHands || i%2==0)worst=Mathf.Max(worst,Vector3.Distance(rig.priests[i].GetBoneTransform(HumanBodyBones.RightHand).position,rig.rightGrips[i].position));}
 if(frame==4||frame==20)Render(camera,"Logs/ark-carry-"+frame+".png");
 }
 File.WriteAllText("Logs/ark-carry-verification.txt",$"Pole centers: {p} / {n}\nScale: {scale}\nPole spacing: {(p.z-n.z)*scale:F3} m\nPriest height: 1.8 m\nMaximum hand target error over 32 phases: {worst:F4} m\nWalking leg rotation range: {legMotion:F2} degrees\n");
 EditorSceneManager.SaveScene(scene,Root+"ArkCarryPreview.unity");AssetDatabase.SaveAssets();SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);
 Debug.Log("Ark carry preview created. See Logs/ark-carry-verification.txt");
 }
 static void Render(Camera camera,string path){var rt=new RenderTexture(1280,900,24);camera.targetTexture=rt;var prior=RenderTexture.active;camera.Render();RenderTexture.active=rt;var img=new Texture2D(1280,900,TextureFormat.RGB24,false);img.ReadPixels(new Rect(0,0,1280,900),0,0);img.Apply();File.WriteAllBytes(path,img.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(img);UnityEngine.Object.DestroyImmediate(rt);}
}}





