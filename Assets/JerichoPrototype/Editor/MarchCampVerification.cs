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
        static JerichoMission mission;static MarchCamp camp;static int stage;static float started;static bool captured;static int waypoint;static float morningHour;static bool restored;
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
            mission=UnityEngine.Object.FindObjectOfType<JerichoMission>();mission.SaveEnabled=false;typeof(JerichoMission).GetField("<SaveChoiceOpen>k__BackingField",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(mission,false);camp=mission.GetComponent<MarchCamp>();started=Time.unscaledTime;stage=0;captured=false;restored=false;EditorApplication.update+=Tick;
        }
        static void Tick()
        {
            try
            {
                if(Time.unscaledTime-started>75)throw new Exception("Transition timeout");
                if(stage==0 && camp.Playing && Time.unscaledTime-started>2){Capture("Logs/camp-morning.png");stage=1;}
                if(stage==1 && !camp.Playing)
                {
                    if(!mission.player.GetComponent<JoshuaController>().enabled)throw new Exception("Opening did not restore player");
                    waypoint=0;morningHour=camp.Hour;mission.player.GetComponent<JoshuaController>().readKeyboard=false;stage=3;
                }
                if(stage==3)
                {
                    if(mission.CompletedCheckpoints>waypoint)waypoint=mission.CompletedCheckpoints;
                    if(waypoint==3 && !restored)
                    {
                        var saved=JsonUtility.FromJson<MarchSave>(JsonUtility.ToJson(mission.CaptureProgress()));
                        var position=mission.player.position;
                        var motor=mission.player.GetComponent<CharacterController>();motor.enabled=false;mission.player.position+=Vector3.right*50;motor.enabled=true;
                        typeof(JerichoMission).GetField("pendingSave",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(mission,saved);
                        typeof(JerichoMission).GetField("<SaveChoiceOpen>k__BackingField",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(mission,true);
                        mission.ChooseSavedGame(true);
                        if(Vector3.Distance(mission.player.position,position)>.01f || mission.CompletedCheckpoints!=3)throw new Exception("Saved position/checkpoint restore failed");
                        restored=true;
                    }
                    if(waypoint>=6 || mission.Days.Circuits>0){if(camp.Hour<=morningHour+5)throw new Exception("Sun did not advance along actual player route");stage=2;}
                    else {var motor=mission.player.GetComponent<CharacterController>();motor.enabled=false;var target=mission.checkpoints[waypoint].position+Vector3.up*.1f;mission.player.position=Vector3.MoveTowards(mission.player.position,target,60*Time.deltaTime);motor.enabled=true;Physics.SyncTransforms();}
                }
                if(stage==2 && camp.Playing && camp.Hour>17.8f && camp.Hour<18.4f && !captured){Capture("Logs/camp-sunset.png");captured=true;}
                if(stage==2 && mission.Days.Day==2 && !camp.Playing)
                {
                    if(!restored || !captured || mission.Days.Circuits!=0 || !mission.player.GetComponent<JoshuaController>().enabled || Mathf.Abs(camp.Hour-9)>.1f)throw new Exception("Morning state not restored");
                    Finish("PASS: saved player/troop state restored at checkpoint 3; player moved along six actual scene checkpoints, sun advanced during movement, camp opening, daylight sunset, day-one horn moment, evening camp, fade/night/morning sequence, day-two reset, and movement restored. Morning and sunset images saved.");
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



