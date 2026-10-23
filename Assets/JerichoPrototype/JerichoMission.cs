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
        public int CurrentDay => Days.Day;
        public int CurrentCircuit => Days.Circuits;
        public int RequiredCircuits => Days.Required;
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
        CityWallCollapse city;
        ThirdPersonCamera finaleCamera;
        JoshuaController finaleController;
        bool finaleControllerEnabled;
        bool testPanel = true;
        readonly List<GameObject> finaleArmy = new List<GameObject>();
        public bool Testing { get; private set; }
        public void EnableTesting()
        {
            if(transitioning)return;
            Testing=true;SaveEnabled=false;SaveChoiceOpen=false;pendingSave=null;openingShown=true;
            if(briefing)briefing.ResumeSavedMarch();
            saveStatus="TEST MODE — progress is not saved";
        }
        public void TestNextCheckpoint()
        {
            if(transitioning || route==null || Complete)return;
            EnableTesting();
            var actors=Actors();var positions=actors.Select(a=>a.position).ToArray();
            Vector3 target=checkpoints[Mathf.Min(5,route.Reached)].position;target.y=player.position.y;
            Vector3 delta=target-player.position;
            var motors=actors.SelectMany(a=>a.GetComponentsInChildren<CharacterController>()).Distinct().ToArray();
            var states=motors.Select(m=>m.enabled).ToArray();foreach(var m in motors)m.enabled=false;
            for(int i=0;i<actors.Length;i++)actors[i].position=positions[i]+delta;
            for(int i=0;i<motors.Length;i++)motors[i].enabled=states[i];Physics.SyncTransforms();
        }
        public void TestFinishDay(bool finalDay)
        {
            if(transitioning || route==null)return;
            EnableTesting();
            if(finalDay){Days.Restore(7,6);if(city && city.Collapsed)city.ResetWalls();}
            else if(!Complete)Days.Restore(Days.Day,Days.Required-1);
            else return;
            ResetCircuit(5);TestNextCheckpoint();
        }
        void Start()
        {
            briefing=FindObjectOfType<BriefingDialogue>();
            if(!player || checkpoints==null || checkpoints.Length!=6 || checkpoints.Any(p=>!p)){Debug.LogError("March needs a player and six checkpoints.",this);return;}
            var bounds=new Bounds(checkpoints[0].position,Vector3.zero);foreach(var p in checkpoints)bounds.Encapsulate(p.position);cityCenter=bounds.center;
            hornMoments=GetComponent<HalfCircuitHorns>();if(!hornMoments)hornMoments=gameObject.AddComponent<HalfCircuitHorns>();
            camp=GetComponent<MarchCamp>();if(!camp)camp=gameObject.AddComponent<MarchCamp>();camp.Initialize(cityCenter);
            city=FindObjectsOfType<CityWallCollapse>().FirstOrDefault(c=>c.gameObject.scene==gameObject.scene);
            ResetCircuit();pendingSave=MarchSave.Load();SaveChoiceOpen=pendingSave!=null;
        }
        void ResetCircuit(int reached=0){route=new CheckpointCircuit(checkpoints.Select(p=>p.position).ToArray(),Mathf.Max(18,checkpointRadius),reached);}
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.E))testPanel=!testPanel;
            if(Input.GetKeyDown(KeyCode.F7))TestFinishDay(true);
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
            if(Days.Complete){yield return CollapseFinale();yield break;}
            if(Days.DayFinished){yield return camp.Show(Days.Day,true,player);Days.NextMorning();}
            ResetCircuit();transitioning=false;SaveProgress();
        }
        IEnumerator CollapseFinale()
        {
            transitioning=true;
            if(city && !city.Finished)
            {
                BuildFinaleArmy();
                ClearFinalePath();
                finaleController=player.GetComponent<JoshuaController>();
                if(finaleController){finaleControllerEnabled=finaleController.enabled;finaleController.enabled=false;}
                finaleCamera=FindObjectsOfType<ThirdPersonCamera>().FirstOrDefault(c=>c.target==player && c.isActiveAndEnabled);
                if(finaleCamera)
                {
                    finaleCamera.ShowCollapseView(player.position,cityCenter);
                }
                yield return new WaitForSeconds(1.2f);
                city.Collapse();while(city && !city.Finished)yield return null;
                yield return new WaitForSeconds(1);
                RestoreFinaleControls();
            }
            transitioning=false;SaveProgress();
        }

        void ClearFinalePath()
        {
            var towardCity = cityCenter - player.position;
            towardCity.y = 0;
            if (towardCity.sqrMagnitude < .01f) towardCity = player.forward;
            towardCity.Normalize();
            var flank = Vector3.Cross(Vector3.up, towardCity).normalized;

            var guard = player.Find("Joshua Stone Guard");
            if (!guard)
            {
                guard = new GameObject("Joshua Stone Guard").transform;
                guard.SetParent(player, false);
                var collider = guard.gameObject.AddComponent<CapsuleCollider>();
                collider.center = new Vector3(0, 1.1f, 0);
                collider.radius = 1.2f;
                collider.height = 3.1f;
            }

            // Move priests and existing procession members clear of the rubble,
            // leaving Joshua exactly where the finale camera expects him.
            int index = 0;
            foreach (var follower in FindObjectsOfType<ProcessionFollower>()
                .Where(f => f && f.gameObject.scene == gameObject.scene && !f.name.StartsWith("Finale Attack Soldier")))
            {
                var motor = follower.GetComponent<CharacterController>();
                if (motor) motor.enabled = false;
                follower.transform.position = player.position + flank * (-12f - (index % 5) * 1.5f) - towardCity * (index / 5 * 2f + 3f);
                follower.transform.rotation = Quaternion.LookRotation(towardCity, Vector3.up);
                follower.enabled = false;
                index++;
            }
        }

        void BuildFinaleArmy()
        {
            if (finaleArmy.Count > 0) return;
            var source = FindObjectsOfType<ProcessionFollower>()
                .FirstOrDefault(f => f && f.gameObject.scene == gameObject.scene && f.name.StartsWith("Soldier "));
            if (!source) return;

            Vector3 towardCity = cityCenter - player.position;
            towardCity.y = 0;
            if (towardCity.sqrMagnitude < .01f) towardCity = player.forward;
            towardCity.Normalize();
            Vector3 line = Vector3.Cross(Vector3.up, towardCity).normalized;

            // Extra ranks form up behind Joshua, facing the walls.
            for (int row = 0; row < 3; row++)
            for (int column = -4; column <= 4; column++)
            {
                var copy = Instantiate(source.gameObject);
                copy.name = "Finale Attack Soldier " + finaleArmy.Count;
                // Keep the attack ranks on the flank, clear of the inward-
                // falling masonry, while retaining them in the camera frame.
                copy.transform.position = player.position - towardCity * (8f + row * 2.2f) + line * (18f + column * 1.35f);
                copy.transform.rotation = Quaternion.LookRotation(towardCity, Vector3.up);
                var follower = copy.GetComponent<ProcessionFollower>();
                if (follower) follower.enabled = false;
                var controller = copy.GetComponent<JoshuaController>();
                if (controller) controller.enabled = false;
                finaleArmy.Add(copy);
            }
        }
        void RestoreFinaleControls()
        {
            if(finaleCamera)finaleCamera.EndHornView();
            if(finaleController)finaleController.enabled=finaleControllerEnabled;
            finaleCamera=null;finaleController=null;
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
                wallsFallen=city && city.Finished,
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
            if(briefing)briefing.ResumeSavedMarch();
            bool fallen=pendingSave.wallsFallen;pendingSave=null;saveStatus="Saved march resumed";
            if(Complete){if(fallen && city)city.RestoreCollapsed();else StartCoroutine(CollapseFinale());}
        }
        public void DrawSaveChoice()
        {
            using(BlackGameText.Scope())
            {
                var rect=new Rect(Screen.width/2-405,Screen.height/2-75,480,210);GUI.Box(rect,"Continue your march?");
                GUI.Label(new Rect(rect.x+25,rect.y+40,430,65),"Saved day: "+pendingSave.day+" / 7\nCircuits completed: "+pendingSave.circuits+" | Checkpoints passed: "+pendingSave.reached+" / 6");
                if(GUI.Button(new Rect(rect.x+25,rect.y+125,200,45),"Continue"))ChooseSavedGame(true);
                if(GUI.Button(new Rect(rect.x+255,rect.y+125,200,45),"Start over"))ChooseSavedGame(false);
            }
        }
        void OnApplicationPause(bool paused){if(paused)SaveProgress();}
        void OnApplicationQuit(){SaveProgress();}
        void OnDisable(){StopAllCoroutines();RestoreFinaleControls();transitioning=false;if(camp)camp.enabled=false;}
        void OnGUI()
        {
            GUI.enabled = true;
            GUI.color = Color.black;
            if(GUI.Button(new Rect(Screen.width-165,12,150,28),"Step ahead / E"))testPanel=!testPanel;
            if(testPanel)
            {
                var old=GUI.color;GUI.color=Color.white;GUI.DrawTexture(new Rect(Screen.width-320,45,305,195),Texture2D.whiteTexture);GUI.color=old;
                using(BlackGameText.Scope())
                {
                    GUI.Label(new Rect(Screen.width-305,52,275,45),"Step through the march\nPress E to show or hide this menu.");
                    GUI.enabled=!transitioning;
                    if(GUI.Button(new Rect(Screen.width-305,103,275,32),"Next checkpoint"))TestNextCheckpoint();
                    if(GUI.Button(new Rect(Screen.width-305,140,275,32),"Finish this day → camp"))TestFinishDay(false);
                    if(GUI.Button(new Rect(Screen.width-305,177,275,32),"Final circuit → walls fall"))TestFinishDay(true);
                    GUI.enabled=true;
                    GUI.Label(new Rect(Screen.width-305,212,275,24),transitioning?"Playing sequence…":"Restart Play Mode to leave test mode.");
                }
            }
            if((briefing && briefing.IsOpen) || (camp && camp.Playing))return;
            using(BlackGameText.Scope())
            {
                GUI.Label(new Rect(30,15,500,25),"MISSION: JERICHO — DAY "+Days.Day+" / 7");
                float hour=camp?camp.Hour:9;int hours=Mathf.FloorToInt(hour);int minutes=Mathf.FloorToInt((hour-hours)*60);
                string time=(hours>12?hours-12:hours)+":"+minutes.ToString("00")+(hours>=12?" PM":" AM");
                float circuitPercent = route == null || !player ? 0f : route.Fraction(player.position) * 100f;
                GUI.Label(new Rect(30,43,500,90),"Circuits completed today: "+Days.Circuits+" / "+Days.Required+"\n"+time+" — Joshua 6\n"+(Complete?"SEVEN-DAY MARCH COMPLETE":"Current circuit: "+Mathf.FloorToInt(circuitPercent)+"%"));
                if(ShowInstructions)GUI.Label(new Rect(30,135,650,90),"March once each day for six days; seven circuits on day seven.\nWASD move | Shift run | Right click: mouse look\nLeft click: full procession view | Esc: free cursor\nFinish the day's march to return to camp at sunset.");
            }
        }
    }
}
