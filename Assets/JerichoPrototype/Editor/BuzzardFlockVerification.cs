using System;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace JerichoPrototype.Editor
{
    [InitializeOnLoad]
    public static class BuzzardFlockVerification
    {
        static float started;
        static int observed;
        static BuzzardFlock flock;
        static BuzzardFlockVerification()
        {
            EditorApplication.delayCall+=()=>{
                if(!File.Exists(".utmp/flock-test") || EditorApplication.isPlayingOrWillChangePlaymode)return;
                File.Delete(".utmp/flock-test");StageUtility.GoToMainStage();
                SessionState.SetBool("FlockTest",true);EditorApplication.isPlaying=true;
            };
            EditorApplication.playModeStateChanged+=state=>{
                if(!SessionState.GetBool("FlockTest",false))return;
                if(state==PlayModeStateChange.EnteredPlayMode)EditorApplication.delayCall+=Begin;
                if(state==PlayModeStateChange.EnteredEditMode){EditorApplication.update-=Tick;SessionState.SetBool("FlockTest",false);}
            };
        }
        static void Begin()
        {
            flock=UnityEngine.Object.FindObjectOfType<BuzzardFlock>();
            var briefing=UnityEngine.Object.FindObjectOfType<BriefingDialogue>();briefing.Close();
            for(int i=0;i<4;i++)briefing.Puzzle.Select(i);briefing.Puzzle.Check();briefing.BeginMarch();
            started=Time.time;observed=0;EditorApplication.update+=Tick;
        }
        static void Tick()
        {
            try
            {
                var phases=(int[])typeof(BuzzardFlock).GetField("groundPhase",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(flock);
                if(phases==null)return;
                int phase=phases[4];
                if((observed&(1<<phase))==0)
                {
                    observed|=1<<phase;Capture("Logs/BuzzardWork/flock-phase-"+phase+".png");
                }
                if(observed==31 && phase==0 && Time.time-started>10)
                {
                    int count=flock.GetComponentsInChildren<BuzzardAnimation>().Length;
                    if(count!=18)throw new Exception("Wrong flock count "+count);
                    Finish("PASS: 18 rigged birds; bird 5 progressed through flying, landing, perching, hopping, takeoff and back to flight in Play Mode. Phase captures saved.");
                }
                else if(Time.time-started>40)throw new Exception("Ground sequence incomplete. Observed phase mask "+observed);
            }
            catch(Exception e){Finish("FAIL: "+e);}
        }
        static void Capture(string path)
        {
            var cam=flock.gameplayCamera;var rt=new RenderTexture(1280,800,24);var old=RenderTexture.active;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
            var image=new Texture2D(1280,800);image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
            cam.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);
        }
        static void Finish(string result){File.WriteAllText("Logs/flock-test.txt",result);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;}
    }
}
