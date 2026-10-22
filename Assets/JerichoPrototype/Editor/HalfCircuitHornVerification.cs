using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace JerichoPrototype.Editor
{
    [InitializeOnLoad]
    public static class HalfCircuitHornVerification
    {
        static HalfCircuitHorns moment;
        static JerichoMission mission;
        static ThirdPersonCamera camera;
        static float started;
        static int stage;
        static bool sawBlowing;
        static Transform checkpoint;
        static HalfCircuitHornVerification()
        {
            EditorApplication.delayCall+=()=>{
                if(!File.Exists(".utmp/half-horn-test") || EditorApplication.isPlayingOrWillChangePlaymode)return;
                File.Delete(".utmp/half-horn-test");
                SessionState.SetBool("HalfHornTest",true);EditorApplication.isPlaying=true;
            };
            EditorApplication.playModeStateChanged+=s=>{
                if(!SessionState.GetBool("HalfHornTest",false))return;
                if(s==PlayModeStateChange.EnteredPlayMode)EditorApplication.delayCall+=Begin;
                if(s==PlayModeStateChange.EnteredEditMode){EditorApplication.update-=Tick;SessionState.SetBool("HalfHornTest",false);}
            };
        }
        static void Begin()
        {
            var briefing=UnityEngine.Object.FindObjectOfType<BriefingDialogue>();briefing.Close();
            for(int i=0;i<4;i++)briefing.Puzzle.Select(i);briefing.Puzzle.Check();briefing.BeginMarch();
            mission=UnityEngine.Object.FindObjectOfType<JerichoMission>();moment=mission.GetComponent<HalfCircuitHorns>();
            camera=UnityEngine.Object.FindObjectOfType<ThirdPersonCamera>();
            stage=0;sawBlowing=false;started=Time.time;
            checkpoint=mission.checkpoints[2];
            checkpoint.position=mission.player.position+Vector3.right*30;
            moment.CheckCheckpoint(checkpoint,mission.player,Vector3.zero);
            if(moment.Playing){Finish("FAIL: triggered before halfway");return;}
            checkpoint.position=mission.player.position;
            moment.CheckCheckpoint(checkpoint,mission.player,Vector3.zero);EditorApplication.update+=Tick;
        }
        static void Tick()
        {
            try
            {
                if(Time.time-started>12)throw new Exception("Moment did not restore controls");
                var priests=UnityEngine.Object.FindObjectsOfType<PriestHornBlow>();
                if(stage==0 && Time.time-started>1.6f)
                {
                    if(!priests.All(p=>p.IsBlowing) || !camera.ShowingHornView)throw new Exception("Seven priests/camera not active together");
                    var cam=camera.GetComponent<Camera>();
                    foreach(var p in priests)
                    {
                        var point=cam.WorldToViewportPoint(p.transform.position+Vector3.up*1.5f);
                        if(point.z<=0 || point.x<0 || point.x>1 || point.y<0 || point.y>1)throw new Exception("Priest outside camera: "+p.name);
                    }
                    var rt=new RenderTexture(1280,800,24);var old=RenderTexture.active;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
                    var image=new Texture2D(1280,800);image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();File.WriteAllBytes("Logs/half-circuit-horns.png",image.EncodeToPNG());
                    cam.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);
                    sawBlowing=true;stage=1;
                }
                if(stage==1 && !moment.Playing)
                {
                    if(camera.ShowingHornView || !mission.player.GetComponent<JoshuaController>().enabled)throw new Exception("Camera/player not restored");
                    moment.CheckCheckpoint(checkpoint,mission.player,Vector3.zero);if(moment.Playing)throw new Exception("Repeated while inside checkpoint");
                    checkpoint.position=mission.player.position+Vector3.right*30;moment.CheckCheckpoint(checkpoint,mission.player,Vector3.zero);
                    checkpoint.position=mission.player.position;moment.CheckCheckpoint(checkpoint,mission.player,Vector3.zero);if(!moment.Playing)throw new Exception("Return to checkpoint did not trigger");
                    stage=2;
                }
                if(stage==2 && !moment.Playing)Finish("PASS: checkpoint arrival and return trigger, no repeated trigger while inside, all priests visible and blowing, camera and controls restored. Visual capture saved. Blowing observed: "+sawBlowing);
            }
            catch(Exception e){Finish("FAIL: "+e);}
        }
        static void Finish(string result){File.WriteAllText("Logs/half-horn-test.txt",result);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;}
    }
}
