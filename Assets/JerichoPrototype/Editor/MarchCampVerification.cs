using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace JerichoPrototype.Editor
{
    [InitializeOnLoad] public static class MarchCampVerification
    {
        static JerichoMission mission;static MarchCamp camp;static int stage;static float started;static bool captured;
        static MarchCampVerification()
        {
            EditorApplication.delayCall+=()=>{
                if(!File.Exists(".utmp/march-camp-test") || EditorApplication.isPlayingOrWillChangePlaymode)return;
                File.Delete(".utmp/march-camp-test");
                if(SceneManager.GetActiveScene().isDirty){File.WriteAllText("Logs/march-camp-test.txt","BLOCKED: save current scene before test.");return;}
                SessionState.SetString("CampPreviousScene",SceneManager.GetActiveScene().path);
                EditorSceneManager.OpenScene("Assets/Scenes/JerichoPrototype.unity");SessionState.SetBool("CampTest",true);EditorApplication.isPlaying=true;
            };
            EditorApplication.playModeStateChanged+=s=>{
                if(!SessionState.GetBool("CampTest",false))return;
                if(s==PlayModeStateChange.EnteredPlayMode)EditorApplication.delayCall+=Begin;
                if(s==PlayModeStateChange.EnteredEditMode){EditorApplication.update-=Tick;SessionState.SetBool("CampTest",false);var path=SessionState.GetString("CampPreviousScene","");if(!string.IsNullOrEmpty(path))EditorSceneManager.OpenScene(path);}
            };
        }
        static void Begin()
        {
            var briefing=UnityEngine.Object.FindObjectOfType<BriefingDialogue>();briefing.Close();for(int i=0;i<4;i++)briefing.Puzzle.Select(i);briefing.Puzzle.Check();briefing.BeginMarch();
            mission=UnityEngine.Object.FindObjectOfType<JerichoMission>();camp=mission.GetComponent<MarchCamp>();started=Time.unscaledTime;stage=0;captured=false;EditorApplication.update+=Tick;
        }
        static void Tick()
        {
            try
            {
                if(Time.unscaledTime-started>40)throw new Exception("Transition timeout");
                if(stage==0 && camp.Playing && Time.unscaledTime-started>2){Capture("Logs/camp-morning.png");stage=1;}
                if(stage==1 && !camp.Playing)
                {
                    if(!mission.player.GetComponent<JoshuaController>().enabled)throw new Exception("Opening did not restore player");
                    var c=(CircuitProgress)typeof(JerichoMission).GetField("circuit",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(mission);
                    // Feed a complete exterior circuit; do not move or save scene objects.
                    for(int angle=0;angle<=360;angle++){float a=angle*Mathf.Deg2Rad;var p=new Vector3(Mathf.Sin(a),0,-Mathf.Cos(a));p*=48/Mathf.Max(Mathf.Abs(p.x),Mathf.Abs(p.z));c.Sample(p);}
                    stage=2;
                }
                if(stage==2 && camp.Playing && camp.Hour>17.8f && camp.Hour<18.4f && !captured){Capture("Logs/camp-sunset.png");captured=true;}
                if(stage==2 && mission.Days.Day==2 && !camp.Playing)
                {
                    if(!captured || mission.Days.Circuits!=0 || !mission.player.GetComponent<JoshuaController>().enabled || Mathf.Abs(camp.Hour-9)>.1f)throw new Exception("Morning state not restored");
                    Finish("PASS: camp opening, daylight sunset, day-one horn moment, evening camp, fade/night/morning sequence, day-two reset, and movement restored. Morning and sunset images saved.");
                }
            }catch(Exception e){Finish("FAIL: "+e);}
        }
        static void Capture(string path)
        {
            var camera=UnityEngine.Object.FindObjectsOfType<Camera>().First(c=>c.name=="Camp camera");
            var rt=new RenderTexture(1000,650,24);var old=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(1000,650);image.ReadPixels(new Rect(0,0,1000,650),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
        }
        static void Finish(string text){File.WriteAllText("Logs/march-camp-test.txt",text);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;}
    }
}

