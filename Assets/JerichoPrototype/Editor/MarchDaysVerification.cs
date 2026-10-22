using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace JerichoPrototype.Editor
{
    public static class MarchDaysVerification
    {
        [InitializeOnLoadMethod] static void Requested()
        {
            EditorApplication.delayCall+=()=>{
                if(!File.Exists(".utmp/march-days-test"))return;
                File.Delete(".utmp/march-days-test");
                try
                {
                    var days=new MarchDays();int count=0;
                    for(int day=1;day<=7;day++)
                    {
                        Require(days.Day==day && !days.NextMorning(),"Early day advance");
                        int required=day==7?7:1;
                        Require(days.Required==required,"Incorrect circuit requirement");
                        for(int i=0;i<required;i++){Require(!days.DayFinished,"Early day finish");days.FinishCircuit();count++;}
                        days.FinishCircuit();Require(days.Circuits==required,"Duplicate finish counted");
                        Require(days.NextMorning()==(day<7),"Next morning failed");
                    }
                    Require(count==13 && days.Complete && days.Day==7,"Final completion failed");
                    File.WriteAllText("Logs/march-days-test.txt","PASS: six one-circuit days; seventh day seven circuits; 13 total; no premature day advances; duplicate finishes rejected; no day eight.");
                }catch(Exception e){File.WriteAllText("Logs/march-days-test.txt",e.ToString());Debug.LogException(e);}
            };
        }
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
    }
}
