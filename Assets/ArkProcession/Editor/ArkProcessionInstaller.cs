using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using JerichoPrototype;
namespace ArkProcession.Editor {
[InitializeOnLoad]
public static class ArkProcessionInstaller {
 static double started, last; static float error, distance; static Vector3 previous; static Quaternion legStart; static float legMotion; static bool sawWalk, sawStop; static Camera camera;
 static ArkProcessionInstaller(){EditorApplication.delayCall+=InstallRequested;EditorApplication.playModeStateChanged+=StateChanged;}
 static void InstallRequested(){if(!File.Exists(".utmp/ark-install"))return;File.Delete(".utmp/ark-install");try{Install();}catch(Exception ex){File.WriteAllText("Logs/ark-install.txt",ex.ToString());}}
 [MenuItem("Tools/Jericho Prototype/Install Four Ark Bearers")]
 public static void Install(){
 if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play Mode first.");
 var manager=UnityEngine.Object.FindObjectOfType<ProcessionManager>();if(!manager||!manager.leader)throw new Exception("Open the Jericho scene first.");
 var rig=UnityEngine.Object.FindObjectOfType<ArkCarryRig>();
 if(!rig){var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ArkProcession/FourPriestArk.prefab"),manager.gameObject.scene);Undo.RegisterCreatedObjectUndo(go,"Add four Ark bearers");rig=go.GetComponent<ArkCarryRig>();}
 rig.procession=manager;rig.twoHands=false;rig.walk=false;
 float rear=manager.followers.Where(f=>f).Min(f=>f.GetComponent<FormationSlot>().localOffset.z);
 rig.formationOffset=new Vector3(0,0,rear-5.5f);rig.transform.position=manager.leader.position+manager.leader.rotation*rig.formationOffset;rig.transform.rotation=manager.leader.rotation;
 foreach(var t in rig.GetComponentsInChildren<Transform>(true))t.gameObject.layer=2;
 EditorUtility.SetDirty(rig);PrefabUtility.RecordPrefabInstancePropertyModifications(rig);PrefabUtility.RecordPrefabInstancePropertyModifications(rig.transform);
 EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);EditorSceneManager.SaveScene(manager.gameObject.scene);
 File.WriteAllText("Logs/ark-install.txt",$"Installed four dedicated bearers behind existing procession at offset {rig.formationOffset}. Seven hornblowers preserved.\n");
 SessionState.SetBool("ArkCarryPlayTest",true);EditorApplication.isPlaying=true;
 }
 static void StateChanged(PlayModeStateChange state){if(!SessionState.GetBool("ArkCarryPlayTest",false))return;if(state==PlayModeStateChange.EnteredPlayMode){started=last=EditorApplication.timeSinceStartup;error=distance=legMotion=0;sawWalk=sawStop=false;var rig=UnityEngine.Object.FindObjectOfType<ArkCarryRig>();previous=rig.transform.position;legStart=rig.priests[0].GetBoneTransform(HumanBodyBones.LeftUpperLeg).localRotation;var controller=rig.procession.leader.GetComponent<JoshuaController>();if(controller)controller.enabled=false;EditorApplication.update+=Tick;}if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool("ArkCarryPlayTest",false);EditorApplication.update-=Tick;}}
 static void Tick(){
 if(!Application.isPlaying)return;
 try{var rig=UnityEngine.Object.FindObjectOfType<ArkCarryRig>();double now=EditorApplication.timeSinceStartup;float dt=(float)(now-last);last=now;float elapsed=(float)(now-started);
 if(elapsed<3)rig.procession.leader.position+=rig.procession.leader.forward*(dt*.75f);
 if(elapsed>.5f){distance+=Vector3.Distance(previous,rig.transform.position);sawWalk|=rig.walk;if(elapsed>4)sawStop|=!rig.walk;legMotion=Mathf.Max(legMotion,Quaternion.Angle(legStart,rig.priests[0].GetBoneTransform(HumanBodyBones.LeftUpperLeg).localRotation));for(int i=0;i<4;i++){var hand=rig.priests[i].GetBoneTransform(i%2==0?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);error=Mathf.Max(error,Vector3.Distance(hand.position,(i%2==0?rig.rightGrips[i]:rig.leftGrips[i]).position));}}previous=rig.transform.position;
 if(elapsed>2 && !camera){camera=new GameObject("Ark verification camera").AddComponent<Camera>();camera.transform.position=rig.transform.TransformPoint(new Vector3(6,4.2f,7.5f));camera.transform.LookAt(rig.transform.position+Vector3.up);camera.fieldOfView=42;var rt=new RenderTexture(1280,900,24);camera.targetTexture=rt;camera.Render();var prior=RenderTexture.active;RenderTexture.active=rt;var img=new Texture2D(1280,900);img.ReadPixels(new Rect(0,0,1280,900),0,0);img.Apply();File.WriteAllBytes("Logs/ark-main-playmode.png",img.EncodeToPNG());RenderTexture.active=prior;camera.targetTexture=null;camera.enabled=false;UnityEngine.Object.Destroy(img);UnityEngine.Object.Destroy(rt);}
 if(elapsed>5){bool pass=distance>1&&sawWalk&&sawStop&&legMotion>5&&error<.04f;File.AppendAllText("Logs/ark-install.txt",$"{(pass?"PASS":"FAIL")} Play Mode: travel {distance:F3}m, walking {sawWalk}, stopping {sawStop}, leg motion {legMotion:F2} degrees, max grip error {error:F4}m.\n");EditorApplication.update-=Tick;EditorApplication.isPlaying=false;}
 }catch(Exception ex){File.AppendAllText("Logs/ark-install.txt",ex.ToString());EditorApplication.update-=Tick;EditorApplication.isPlaying=false;}
 }
}}
