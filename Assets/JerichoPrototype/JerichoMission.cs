using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace JerichoPrototype
{
    public sealed class JerichoMission : MonoBehaviour
    {
        public Transform player;
        public Transform[] checkpoints;
        public float checkpointRadius=4;
        public int CompletedCheckpoints => route==null?0:route.Reached;
        public MarchDays Days { get; }=new MarchDays();
        public bool Complete=>Days.Complete;
        public bool ShowInstructions=>Days.Day==1 && CompletedCheckpoints==0;
        public bool SaveChoiceOpen { get; private set; }
        public bool SaveEnabled { get; set; }=true;
        BriefingDialogue briefing;
        CheckpointCircuit route;
        HalfCircuitHorns hornMoments;
        MarchCamp camp;
        MarchSave pendingSave;
        Vector3 cityCenter;
        bool openingShown,transitioning;
        string saveStatus="";
        void Start()
        {
            briefing=FindObjectOfType<BriefingDialogue>();
            if(!player || checkpoints==null || checkpoints.Length!=6 || checkpoints.Any(p=>!p)){Debug.LogError("March needs a player and six checkpoints.",this);return;}
            var bounds=new Bounds(checkpoints[0].position,Vector3.zero);foreach(var p in checkpoints)bounds.Encapsulate(p.position);cityCenter=bounds.center;
            hornMoments=GetComponent<HalfCircuitHorns>();if(!hornMoments)hornMoments=gameObject.AddComponent<HalfCircuitHorns>();
            camp=GetComponent<MarchCamp>();if(!camp)camp=gameObject.AddComponent<MarchCamp>();camp.Initialize(cityCenter);
            ResetCircuit();pendingSave=MarchSave.Load();SaveChoiceOpen=pendingSave!=null;
        }
        void ResetCircuit(int reached=0){route=new CheckpointCircuit(checkpoints.Select(p=>p.position).ToArray(),Mathf.Max(18,checkpointRadius),reached);}
        void Update()
        {
            if(!player || route==null || SaveChoiceOpen || (briefing && briefing.IsOpen) || transitioning || Complete || hornMoments.Playing)return;
            if(!openingShown){openingShown=true;StartCoroutine(OpenCamp());return;}
            bool arrived=route.Sample(player.position);
            camp.SetDayProgress((Days.Circuits+route.Fraction(player.position))/Days.Required);
            if(route.Complete){StartCoroutine(FinishCircuit());return;}
            if(arrived)SaveProgress();
        }
        IEnumerator OpenCamp(){transitioning=true;yield return camp.Show(1,false,player);ResetCircuit();transitioning=false;SaveProgress();}
        IEnumerator FinishCircuit()
        {
            transitioning=true;Days.FinishCircuit();hornMoments.PlayAtFinish(player,cityCenter);
            while(hornMoments.Playing)yield return null;
            if(Days.Complete){transitioning=false;SaveProgress();yield break;}
            if(Days.DayFinished){yield return camp.Show(Days.Day,true,player);Days.NextMorning();}
            ResetCircuit();transitioning=false;SaveProgress();
        }
        static string Path(Transform t)
        {
            string path=t.GetSiblingIndex().ToString();while(t.parent){t=t.parent;path=t.GetSiblingIndex()+"/"+path;}return path;
        }
        Transform[] Actors()
        {
            var actors=new HashSet<Transform>{player};
            foreach(var f in FindObjectsOfType<ProcessionFollower>().Where(f=>f.gameObject.scene==gameObject.scene))actors.Add(f.transform);
            foreach(var r in FindObjectsOfType<ArkProcession.ArkCarryRig>().Where(r=>r.gameObject.scene==gameObject.scene))
            {actors.Add(r.transform);foreach(var a in r.priests)if(a)actors.Add(a.transform);}
            return actors.Where(t=>t).OrderBy(t=>Path(t).Count(c=>c=='/')).ToArray();
        }
        public void SaveProgress()
        {
            if(!SaveEnabled || !openingShown || transitioning || route==null || SaveChoiceOpen)return;
            var data=CaptureProgress();
            try{data.Store();saveStatus="Progress saved";}catch(System.Exception e){saveStatus="Save failed";Debug.LogWarning(e.Message,this);}
        }
        public MarchSave CaptureProgress()
        {
            var formation=FindObjectsOfType<ProcessionManager>().FirstOrDefault(p=>p.leader==player);
            var camera=FindObjectsOfType<ThirdPersonCamera>().FirstOrDefault(c=>c.target==player);
            return new MarchSave{day=Days.Day,circuits=Days.Circuits,reached=route.Reached,
                formationHeading=formation?formation.Heading:player.rotation,
                cameraYaw=camera?Quaternion.LookRotation(camera.MovementForward).eulerAngles.y:player.eulerAngles.y,
                poses=Actors().Select(t=>new MarchPose{path=Path(t),position=t.position,rotation=t.rotation}).ToArray()};
        }
        public void ChooseSavedGame(bool resume)
        {
            if(!SaveChoiceOpen)return;
            SaveChoiceOpen=false;
            if(!resume){MarchSave.Clear();pendingSave=null;return;}
            var actors=Actors();
            // Disable motors while applying all parent/child world poses, then restore their state.
            var motors=actors.SelectMany(a=>a.GetComponentsInChildren<CharacterController>()).Distinct().ToArray();
            var enabled=motors.Select(m=>m.enabled).ToArray();foreach(var m in motors)m.enabled=false;
            foreach(var actor in actors){var pose=pendingSave.poses.FirstOrDefault(p=>p.path==Path(actor));if(pose!=null)actor.SetPositionAndRotation(pose.position,pose.rotation);}
            for(int i=0;i<motors.Length;i++)motors[i].enabled=enabled[i];Physics.SyncTransforms();
            Days.Restore(pendingSave.day,pendingSave.circuits);ResetCircuit(pendingSave.reached);openingShown=true;
            foreach(var formation in FindObjectsOfType<ProcessionManager>().Where(p=>p.leader==player))formation.RestoreHeading(pendingSave.formationHeading);
            foreach(var camera in FindObjectsOfType<ThirdPersonCamera>().Where(c=>c.target==player))camera.RestoreHeading(pendingSave.cameraYaw);
            camp.SetDayProgress((Days.Circuits+route.Fraction(player.position))/Days.Required);
            if(briefing)briefing.ResumeSavedMarch();pendingSave=null;saveStatus="Saved march resumed";
        }
        public void DrawSaveChoice()
        {
            using(BlackGameText.Scope())
            {
                var rect=new Rect(Screen.width/2-240,Screen.height/2-105,480,210);GUI.Box(rect,"Continue your march?");
                GUI.Label(new Rect(rect.x+25,rect.y+40,430,65),"Saved day: "+pendingSave.day+" / 7\nCircuits completed: "+pendingSave.circuits+" | Checkpoints passed: "+pendingSave.reached+" / 6");
                if(GUI.Button(new Rect(rect.x+25,rect.y+125,200,45),"Continue"))ChooseSavedGame(true);
                if(GUI.Button(new Rect(rect.x+255,rect.y+125,200,45),"Start over"))ChooseSavedGame(false);
            }
        }
        void OnApplicationPause(bool paused){if(paused)SaveProgress();}
        void OnApplicationQuit(){SaveProgress();}
        void OnDisable(){StopAllCoroutines();transitioning=false;if(camp)camp.enabled=false;}
        void OnGUI()
        {
            if(briefing && briefing.IsOpen)return;
            if(SaveChoiceOpen){DrawSaveChoice();return;}
            if(camp && camp.Playing)return;
            using(BlackGameText.Scope())
            {
                GUI.Label(new Rect(30,15,550,25),"MISSION: JERICHO — DAY "+Days.Day+" / 7");
                float hour=camp?camp.Hour:9;int hours=Mathf.FloorToInt(hour);int minutes=Mathf.FloorToInt((hour-hours)*60);
                string time=(hours>12?hours-12:hours)+":"+minutes.ToString("00")+(hours>=12?" PM":" AM");
                GUI.Label(new Rect(30,43,650,100),"Circuits completed today: "+Days.Circuits+" / "+Days.Required+"\n"+time+" — Joshua 6\n"+(Complete?"SEVEN-DAY MARCH COMPLETE":"Checkpoint: "+CompletedCheckpoints+" / 6 — Next: "+Mathf.Min(6,CompletedCheckpoints+1))+"\n"+saveStatus);
                if(ShowInstructions)GUI.Label(new Rect(30,150,650,90),"Follow checkpoints 1–6 in order. Checkpoint 6 ends each circuit.\nWASD move | Shift run | Right click: mouse look\nLeft click: full procession view | Esc: free cursor\nProgress saves at checkpoints and each new morning.");
            }
        }
    }
}
