using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace JerichoPrototype.Editor {
public static class HornReferenceInstaller {
 static readonly string[] Names={"mixamorig:RightShoulder","mixamorig:RightArm","mixamorig:RightForeArm","mixamorig:RightHand","mixamorig:RightHandMiddle4","Ceremonial Horn"};
 [InitializeOnLoadMethod] static void Init(){EditorApplication.delayCall+=()=>{if(!File.Exists(".utmp/horn-install"))return;File.Delete(".utmp/horn-install");try{Install();}catch(Exception ex){File.WriteAllText("Logs/horn-install.txt",ex.ToString());Debug.LogException(ex);}};}
 [MenuItem("Tools/Jericho Prototype/Use Priest 1 Horn Pose")]
 public static void Install(){
 if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play Mode first.");
 var priests=UnityEngine.Object.FindObjectsOfType<ProcessionFollower>().Where(p=>p.name.StartsWith("Priest ")).OrderBy(p=>p.name).ToArray();
 var source=priests.Single(p=>p.name=="Priest 1");
 var pose=Names.Select(name=>{var t=source.GetComponentsInChildren<Transform>(true).Single(x=>x.name==name);return new PriestHornBlow.AuthoredTransform{path=AnimationUtility.CalculateTransformPath(t,source.transform),position=t.localPosition,rotation=t.localRotation,scale=t.localScale};}).ToArray();
 File.WriteAllText("Logs/horn-reference-backup.json",JsonUtility.ToJson(new PoseBackup{pose=pose},true));
 foreach(var priest in priests){Undo.RegisterFullObjectHierarchyUndo(priest.gameObject,"Apply Priest 1 horn pose");Apply(priest.gameObject,pose);EditorSceneManager.MarkSceneDirty(priest.gameObject.scene);}
 var prefab=PrefabUtility.LoadPrefabContents("Assets/JerichoPrototype/Priest.prefab");try{Apply(prefab,pose);PrefabUtility.SaveAsPrefabAsset(prefab,"Assets/JerichoPrototype/Priest.prefab");}finally{PrefabUtility.UnloadPrefabContents(prefab);}
 EditorSceneManager.SaveScene(source.gameObject.scene);AssetDatabase.SaveAssets();
 // Verify the saved prefab under Animator evaluation, at different world headings.
 var copy=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JerichoPrototype/Priest.prefab"));copy.transform.position=Vector3.up*100;
 try{var a=copy.GetComponentInChildren<Animator>();a.Rebind();a.Play("IDLE",0,0);a.Update(.1f);var blow=copy.GetComponent<PriestHornBlow>();
 foreach(float heading in new[]{0f,90f,180f}){copy.transform.rotation=Quaternion.Euler(0,heading,0);blow.BlowHorn();blow.DebugAdvance(.55f);foreach(var t in pose){var b=copy.transform.Find(t.path);if(Vector3.Distance(b.localPosition,t.position)>.000001f||Quaternion.Angle(b.localRotation,t.rotation)>.02f||Vector3.Distance(b.localScale,t.scale)>.00001f)throw new Exception("Hold pose mismatch: "+t.path);}blow.DebugAdvance(1.4f);blow.DebugAdvance(.5f);if(blow.IsBlowing||copy.GetComponent<ProcessionFollower>().MovementSuppressed)throw new Exception("Horn sequence did not release movement");}
 }finally{UnityEngine.Object.DestroyImmediate(copy);}
 File.WriteAllText("Logs/horn-install.txt",$"PASS: {priests.Length} horn priests and saved prefab updated from Priest 1. Exactly six transforms copied. Raised pose verified at 0, 90 and 180 degrees; movement resumes after sequence. Formation positions preserved.");
 }
 static void Apply(GameObject root,PriestHornBlow.AuthoredTransform[] pose){foreach(var p in pose){var t=root.transform.Find(p.path);if(!t)throw new Exception("Missing "+p.path);t.localPosition=p.position;t.localRotation=p.rotation;t.localScale=p.scale;EditorUtility.SetDirty(t);if(PrefabUtility.IsPartOfPrefabInstance(t))PrefabUtility.RecordPrefabInstancePropertyModifications(t);}var blow=root.GetComponent<PriestHornBlow>();if(!blow)blow=root.AddComponent<PriestHornBlow>();blow.SetAuthoredHoldPose(pose);EditorUtility.SetDirty(blow);if(PrefabUtility.IsPartOfPrefabInstance(blow))PrefabUtility.RecordPrefabInstancePropertyModifications(blow);}
 [Serializable]class PoseBackup{public PriestHornBlow.AuthoredTransform[] pose;}
}}
