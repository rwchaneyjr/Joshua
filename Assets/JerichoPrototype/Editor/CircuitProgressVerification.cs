using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JerichoPrototype.Editor
{
    public static class CircuitProgressVerification
    {
        [InitializeOnLoadMethod] static void Requested()
        {
            if (!File.Exists(".utmp/circuit-test")) return;
            File.Delete(".utmp/circuit-test"); Run();
        }
        [MenuItem("Tools/Jericho Prototype/Verify Circuit Counting")]
        static void Run()
        {
            try
            {
                foreach(float radius in new[]{40f,48f,60f}) foreach(int direction in new[]{-1,1})
                {
                    var c=New();
                    // Square perimeter, both directions, including the route inside/outside the signs.
                    Vector3[] route={new Vector3(0,0,-radius),new Vector3(radius,0,-radius),new Vector3(radius,0,radius),new Vector3(-radius,0,radius),new Vector3(-radius,0,-radius),new Vector3(0,0,-radius)};
                    if(direction<0)Array.Reverse(route);
                    c.Sample(route[0]);
                    for(int segment=1;segment<route.Length;segment++)
                        for(int step=1;step<=100;step++)c.Sample(Vector3.Lerp(route[segment-1],route[segment],step/100f));
                    Require(c.Complete,"Full lap failed at radius "+radius+" direction "+direction);
                }
                var partial=New(); partial.Sample(new Vector3(0,0,-48));partial.Sample(new Vector3(48,0,-48));
                foreach(int direction in new[]{-1,1})
                {
                    var nearFinish=New();var finish=new Vector3(0,0,-48);
                    nearFinish.Sample(finish);nearFinish.FinishAtMarker(finish,finish,10);
                    Require(!nearFinish.Complete,"Starting at 6 counted as a lap");
                    Vector3 last=finish;
                    for(int angle=1;angle<=350;angle++)
                    {
                        float radians=angle*direction*Mathf.Deg2Rad;
                        // Square route on the ray at each angle, always outside city walls.
                        var point=new Vector3(Mathf.Sin(radians),0,-Mathf.Cos(radians));
                        point*=48/Mathf.Max(Mathf.Abs(point.x),Mathf.Abs(point.z));
                        nearFinish.Sample(point);last=point;
                    }
                    Require(!nearFinish.Complete,"Expected to stop just before exact starting angle");
                    nearFinish.FinishAtMarker(last,finish,10);
                    Require(nearFinish.Complete,"Arrival in checkpoint 6 finish area failed");
                }
                Require(!partial.Complete,"Partial lap counted");
                for(int i=0;i<20;i++){partial.Sample(new Vector3(0,0,-48));partial.Sample(new Vector3(48,0,-48));}
                Require(!partial.Complete && Mathf.Abs(partial.Degrees)<46,"Backtracking counted");
                partial.Sample(new Vector3(-48,0,48));Require(Mathf.Abs(partial.Degrees)<.01f,"Shortcut across city counted");
                partial.Sample(Vector3.zero);Require(!partial.Complete && partial.Degrees==0,"City interior did not reset");
                File.WriteAllText("Logs/circuit-test.txt","PASS: full laps both directions at 40/48/60m; checkpoint 6 arrival before exact starting angle counts both directions; starting at 6, partial laps, backtracking and shortcuts rejected.");
            }
            catch(Exception e){File.WriteAllText("Logs/circuit-test.txt",e.ToString());Debug.LogException(e);}
        }
        static CircuitProgress New()=>new CircuitProgress(Vector2.zero,Vector2.one*32.16f);
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
    }
}
