using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace JerichoPrototype.Editor {
public static class HornReferenceInspect {
[InitializeOnLoadMethod] static void Init(){EditorApplication.delayCall+=()=>{if(!File.Exists(".utmp/horn-reference"))return;File.Delete(".utmp/horn-reference");var p=Object.FindObjectsOfType<ProcessionFollower>().FirstOrDefault(x=>x.name=="Priest 1");if(!p)return;var lines=p.GetComponentsInChildren<Transform>(true).Where(t=>t.name.Contains("Right")||t.name.Contains("Horn")||t==p.transform).Select(t=>$"{AnimationUtility.CalculateTransformPath(t,p.transform)} | pos {t.localPosition.ToString("F6")} rot {t.localRotation.ToString("F6")} scale {t.localScale.ToString("F6")}");File.WriteAllLines("Logs/horn-reference.txt",lines);};}
}}
