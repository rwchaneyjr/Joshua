using System;
using System.IO;
using UnityEngine;
using UnityEditor;
namespace JerichoPrototype.Editor
{
    public static class CheckpointSaveVerification
    {
        [InitializeOnLoadMethod]static void Requested(){EditorApplication.delayCall+=()=>{if(!File.Exists(".utmp/checkpoint-save-test"))return;File.Delete(".utmp/checkpoint-save-test");Run();};}
        static void Run()
        {
            try
            {
                Vector3[] p={new Vector3(48,0,-48),new Vector3(48,0,0),new Vector3(48,0,48),new Vector3(-48,0,48),new Vector3(-48,0,-48),new Vector3(0,0,-48)};
                var route=new CheckpointCircuit(p,18);
                Require(!route.Sample(p[5]) && !route.Complete,"Finish shortcut counted");
                Require(!route.Sample(p[0]+Vector3.right*19),"Outside trigger counted");
                for(int i=0;i<6;i++) {Require(route.Sample(p[i]+Vector3.right*15),"Wide checkpoint trigger missed");Require(route.Reached==i+1,"Checkpoint did not advance");Require(!route.Sample(p[i]),"Duplicate checkpoint counted");}
                Require(route.Complete,"Lap did not finish");
                var saved=new MarchSave{day=4,circuits=0,reached=3,poses=new[]{new MarchPose{path="1/2",position=new Vector3(48,.1f,48),rotation=Quaternion.Euler(0,90,0)}}};
                var loaded=JsonUtility.FromJson<MarchSave>(JsonUtility.ToJson(saved));Require(loaded.Valid && loaded.day==4 && loaded.reached==3 && loaded.poses[0].position==saved.poses[0].position,"Save round trip failed");
                route=new CheckpointCircuit(p,18,loaded.reached);Require(!route.Sample(p[2]) && route.Sample(p[3]),"Resumed wrong checkpoint");
                var days=new MarchDays();days.Restore(7,6);Require(!days.Complete,"Early complete on resume");days.FinishCircuit();Require(days.Complete,"Resumed final circuit failed");
                File.WriteAllText("Logs/checkpoint-save-test.txt","PASS: 18m zones accept 15m misses, reject 19m; ordered visits; no skip to 6 or duplicate trigger; checkpoint save JSON round trip; resume correct checkpoint and final seventh-day circuit.");
            }catch(Exception e){File.WriteAllText("Logs/checkpoint-save-test.txt",e.ToString());Debug.LogException(e);}
        }
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    }
}
