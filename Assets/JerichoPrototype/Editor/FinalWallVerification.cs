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
    [InitializeOnLoad] public static class FinalWallVerification
    {
        static JerichoMission mission;static CityWallCollapse city;static float started;static bool horns;static int stage;
        static FinalWallVerification()
        {
            EditorApplication.delayCall+=()=>{
                if(!File.Exists(".utmp/final-wall-test") || EditorApplication.isPlayingOrWillChangePlaymode)return;
                File.Delete(".utmp/final-wall-test");
                if(SceneManager.GetActiveScene().isDirty){File.WriteAllText("Logs/final-wall-test.txt","BLOCKED: current scene has unsaved changes");return;}
                SessionState.SetString("FinalPrevious",SceneManager.GetActiveScene().path);EditorSceneManager.OpenScene("Assets/Scenes/JerichoPrototype.unity");SessionState.SetBool("FinalWallTest",true);EditorApplication.isPlaying=true;
            };
            EditorApplication.playModeStateChanged+=s=>{
                if(!SessionState.GetBool("FinalWallTest",false))return;
                if(s==PlayModeStateChange.EnteredPlayMode)EditorApplication.delayCall+=Begin;
                if(s==PlayModeStateChange.EnteredEditMode){EditorApplication.update-=Tick;SessionState.SetBool("FinalWallTest",false);EditorSceneManager.OpenScene(SessionState.GetString("FinalPrevious","Assets/Scenes/JerichoPrototype.unity"));}
            };
        }
        static void Begin()
        {
            mission=UnityEngine.Object.FindObjectOfType<JerichoMission>();city=UnityEngine.Object.FindObjectOfType<CityWallCollapse>();
            started=Time.unscaledTime;horns=false;stage=0;mission.EnableTesting();mission.TestNextCheckpoint();EditorApplication.update+=Tick;
        }
        static void Tick()
        {
            try
            {
                if(Time.unscaledTime-started>35)throw new Exception("Finale timeout");
                if(stage==0 && mission.CompletedCheckpoints==1)
                {
                    if(city.Collapsed || mission.SaveEnabled)throw new Exception("Early collapse or test saving enabled");
                    mission.TestFinishDay(true);stage=1;
                }
                if(stage==1)
                {
                    horns|=UnityEngine.Object.FindObjectsOfType<PriestHornBlow>().Any(p=>p.IsBlowing);
                    if(city.Finished && mission.player.GetComponent<JoshuaController>().enabled)
                    {
                        if(!horns || !mission.Complete || city.pieces.Any(p=>p.localPosition.y>1.1f))throw new Exception("Final horn/collapse failed");
                        var saved=mission.CaptureProgress();if(!saved.wallsFallen)throw new Exception("Collapsed state not saved");
                        city.ResetWalls();Load(saved);
                        if(!city.Finished || city.pieces.Any(p=>p.localPosition.y>1.1f))throw new Exception("Completed save did not restore rubble");
                        city.ResetWalls();saved.wallsFallen=false;Load(saved);stage=2;
                    }
                }
                if(stage==2 && city.Finished && mission.player.GetComponent<JoshuaController>().enabled)
                    Finish("PASS: next-checkpoint shortcut; test mode disables saving; final circuit plays horns then collapses all "+city.pieces.Length+" wall pieces; controls restored; finished save restores rubble; older completed save replays missing collapse.");
            }catch(Exception e){Finish("FAIL: "+e);}
        }
        static void Load(MarchSave save)
        {typeof(JerichoMission).GetField("pendingSave",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(mission,save);typeof(JerichoMission).GetField("<SaveChoiceOpen>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(mission,true);mission.ChooseSavedGame(true);}
        static void Finish(string result){File.WriteAllText("Logs/final-wall-test.txt",result);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;}
    }
}
