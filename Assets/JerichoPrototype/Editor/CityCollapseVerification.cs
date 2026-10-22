using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JerichoPrototype.Editor
{
    [InitializeOnLoad]
    public static class CityCollapseVerification
    {
        static CityWallCollapse city;
        static Vector3[] start;
        static float started;
        static Camera camera;
        static CityCollapseVerification()
        {
            EditorApplication.delayCall += Request;
            EditorApplication.playModeStateChanged += State;
        }
        static void Request()
        {
            if (!File.Exists(".utmp/city-test") || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(".utmp/city-test");
            if (UnityEngine.SceneManagement.SceneManager.GetSceneByName("TrumpetSearch").isDirty)
            { File.WriteAllText("Logs/city-test.txt","BLOCKED: save the modified search scene first."); return; }
            EditorSceneManager.OpenScene(PrototypeBuilder.ScenePath);
            SessionState.SetBool("CityCollapseTest",true);EditorApplication.isPlaying=true;
        }
        static void State(PlayModeStateChange state)
        {
            if (!SessionState.GetBool("CityCollapseTest",false)) return;
            if(state==PlayModeStateChange.EnteredPlayMode)
            {
                city=UnityEngine.Object.FindObjectOfType<CityWallCollapse>();start=city.pieces.Select(p=>p.localPosition).ToArray();
                camera=new GameObject("City test overview").AddComponent<Camera>();camera.transform.position=new Vector3(95,85,-105);camera.transform.LookAt(new Vector3(0,2,0));camera.fieldOfView=48;camera.farClipPlane=400;
                Render("Logs/grand-jericho.png");city.Collapse();started=Time.time;EditorApplication.update+=Tick;
            }
            if(state==PlayModeStateChange.EnteredEditMode){EditorApplication.update-=Tick;SessionState.SetBool("CityCollapseTest",false);}
        }
        static void Tick()
        {
            if(!Application.isPlaying || Time.time-started<5.5f)return;
            try
            {
                if(!city.Collapsed || city.pieces.Any(p=>p.localPosition.y>1.1f))throw new Exception("Pieces did not finish collapsing.");
                if(city.pieces.Any(p=>p.GetComponent<Collider>().enabled))throw new Exception("Falling collider still enabled.");
                Render("Logs/grand-jericho-collapsed.png");city.ResetWalls();
                for(int i=0;i<start.Length;i++)if(Vector3.Distance(start[i],city.pieces[i].localPosition)>.001f)throw new Exception("Reset mismatch.");
                if(city.Collapsed || city.pieces.Any(p=>!p.GetComponent<Collider>().enabled))throw new Exception("Reset collider mismatch.");
                File.WriteAllText("Logs/city-test.txt","PASS: "+start.Length+" pieces collapsed and reset in Play Mode; wall collision disabled during collapse and restored on reset. Before/after renders saved.");
            }
            catch(Exception e){File.WriteAllText("Logs/city-test.txt",e.ToString());}
            finally{EditorApplication.update-=Tick;EditorApplication.isPlaying=false;}
        }
        static void Render(string path)
        {
            var rt=new RenderTexture(1400,1000,24);var previous=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(1400,1000);image.ReadPixels(new Rect(0,0,1400,1000),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);camera.enabled=false;
        }
    }
}
