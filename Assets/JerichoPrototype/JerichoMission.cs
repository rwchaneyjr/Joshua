using System.Collections;
using UnityEngine;
namespace JerichoPrototype
{
    public sealed class JerichoMission : MonoBehaviour
    {
        public Transform player;
        public Transform[] checkpoints;
        public float checkpointRadius = 4;
        public int CompletedCheckpoints { get; private set; }
        public MarchDays Days { get; } = new MarchDays();
        public bool Complete => Days.Complete;
        public bool ShowInstructions => Days.Day == 1 && CompletedCheckpoints == 0;
        BriefingDialogue briefing;
        CircuitProgress circuit;
        HalfCircuitHorns hornMoments;
        MarchCamp camp;
        Vector3 cityCenter;
        Vector2 innerHalfSize;
        bool openingShown, transitioning;
        void Start()
        {
            briefing=FindObjectOfType<BriefingDialogue>();
            if(checkpoints==null || checkpoints.Length==0 || !checkpoints[0])return;
            var bounds=new Bounds(checkpoints[0].position,Vector3.zero);
            foreach(var checkpoint in checkpoints)if(checkpoint)bounds.Encapsulate(checkpoint.position);
            cityCenter=bounds.center;innerHalfSize=new Vector2(bounds.extents.x,bounds.extents.z)*.67f;
            hornMoments=GetComponent<HalfCircuitHorns>();if(!hornMoments)hornMoments=gameObject.AddComponent<HalfCircuitHorns>();
            camp=GetComponent<MarchCamp>();if(!camp)camp=gameObject.AddComponent<MarchCamp>();
            camp.Initialize(cityCenter);ResetCircuit();
        }
        void ResetCircuit(){circuit=new CircuitProgress(new Vector2(cityCenter.x,cityCenter.z),innerHalfSize);if(player)circuit.Sample(player.position);CompletedCheckpoints=0;}
        void Update()
        {
            if(!player || circuit==null || (briefing && briefing.IsOpen) || transitioning || Complete || hornMoments.Playing)return;
            if(!openingShown){openingShown=true;StartCoroutine(OpenCamp());return;}
            circuit.Sample(player.position);
            if(checkpoints.Length>=6 && checkpoints[5])circuit.FinishAtMarker(player.position,checkpoints[5].position,Mathf.Max(10,checkpointRadius));
            float fraction=Mathf.Clamp01(Mathf.Abs(circuit.Degrees)/360);
            CompletedCheckpoints=circuit.Complete?checkpoints.Length:Mathf.Min(checkpoints.Length-1,Mathf.FloorToInt(fraction*checkpoints.Length));
            camp.SetDayProgress((Days.Circuits+fraction)/Days.Required);
            if(circuit.Complete)StartCoroutine(FinishCircuit());
        }
        IEnumerator OpenCamp(){transitioning=true;yield return camp.Show(1,false,player);ResetCircuit();transitioning=false;}
        IEnumerator FinishCircuit()
        {
            transitioning=true;Days.FinishCircuit();
            hornMoments.PlayAtFinish(player,cityCenter);
            while(hornMoments.Playing)yield return null;
            if(Days.Complete){transitioning=false;yield break;}
            if(Days.DayFinished){yield return camp.Show(Days.Day,true,player);Days.NextMorning();}
            ResetCircuit();transitioning=false;
        }
        void OnDisable(){StopAllCoroutines();transitioning=false;if(camp)camp.enabled=false;}
        void OnGUI()
        {
            if((briefing && briefing.IsOpen) || (camp && camp.Playing))return;
            using(BlackGameText.Scope())
            {
                GUI.Label(new Rect(30,15,500,25),"MISSION: JERICHO — DAY "+Days.Day+" / 7");
                float hour=camp?camp.Hour:9;int hours=Mathf.FloorToInt(hour);int minutes=Mathf.FloorToInt((hour-hours)*60);
                string time=(hours>12?hours-12:hours)+":"+minutes.ToString("00")+(hours>=12?" PM":" AM");
                GUI.Label(new Rect(30,43,500,90),"Circuits completed today: "+Days.Circuits+" / "+Days.Required+"\n"+time+" — Joshua 6\n"+(Complete?"SEVEN-DAY MARCH COMPLETE":"Current circuit: "+Mathf.FloorToInt(Mathf.Clamp01(Mathf.Abs(circuit==null?0:circuit.Degrees)/360)*100)+"%"));
                if(ShowInstructions)GUI.Label(new Rect(30,135,650,90),"March once each day for six days; seven circuits on day seven.\nWASD move | Shift run | Right click: mouse look\nLeft click: full procession view | Esc: free cursor\nFinish the day's march to return to camp at sunset.");
            }
        }
    }
}
