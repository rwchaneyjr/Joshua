using UnityEngine;

namespace JerichoPrototype
{
    [DefaultExecutionOrder(1100)]
    public sealed class BuzzardFlock : MonoBehaviour
    {
        public GameObject coloredBird;
        public Camera gameplayCamera;
        public Vector3 cityCenter;
        public int birdCount=18;
        public float orbitClearance=9f;
        public float minimumLandingDistance=12f;
        public float maximumLandingDistance=35f;
        Transform leader;
        Transform[] procession;
        Transform[] birds;
        BuzzardAnimation[] birdAnimations;
        int[] groundPhase;
        float[] nextLanding, phaseStart, footHeight;
        Vector3[] landing, departure;
        float clock;
        void Start()
        {
            if(!coloredBird || !gameplayCamera)return;
            var followCamera=gameplayCamera.GetComponent<ThirdPersonCamera>();
            leader=followCamera?followCamera.target:null;
            var members=new System.Collections.Generic.List<Transform>();
            if(leader)members.Add(leader);
            foreach(var follower in FindObjectsOfType<ProcessionFollower>())
                if(follower.gameObject.scene==gameObject.scene)members.Add(follower.transform);
            foreach(var rig in FindObjectsOfType<ArkProcession.ArkCarryRig>())
                if(rig.gameObject.scene==gameObject.scene){members.Add(rig.transform);foreach(var priest in rig.priests)if(priest)members.Add(priest.transform);}
            procession=members.ToArray();
            birds=new Transform[birdCount];
            birdAnimations=new BuzzardAnimation[birdCount];groundPhase=new int[birdCount];nextLanding=new float[birdCount];phaseStart=new float[birdCount];footHeight=new float[birdCount];landing=new Vector3[birdCount];departure=new Vector3[birdCount];
            for(int i=0;i<birds.Length;i++)
            {
                var holder=new GameObject("Circling buzzard "+(i+1)).transform;holder.SetParent(transform,false);
                var model=Instantiate(coloredBird,holder);model.name="Colored buzzard";
                model.transform.localPosition=Vector3.zero;
                // Keep the artist's orientation and material; normalize the visible wingspan.
                var renderers=model.GetComponentsInChildren<Renderer>();
                if(model.GetComponent<BuzzardAnimation>())
                {
                    // The FBX skin's imported bounds include a 100x unit conversion.
                    // Use the measured 1.901m source wingspan rather than those inflated bounds.
                    model.transform.localScale=Vector3.one*((i<6?2.4f:3.4f)/1.90108f);
                    foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())skin.updateWhenOffscreen=true;
                }
                else if(renderers.Length>0)
                {
                    var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                    float factor=(i<6?2.4f:3.4f)/Mathf.Max(.001f,Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z)));
                    model.transform.localScale*=factor;model.transform.localPosition-=holder.InverseTransformVector(bounds.center-holder.position)*factor;
                }
                foreach(var collider in model.GetComponentsInChildren<Collider>())collider.enabled=false;
                foreach(var camera in model.GetComponentsInChildren<Camera>())camera.enabled=false;
                foreach(var light in model.GetComponentsInChildren<Light>())light.enabled=false;
                birds[i]=holder;
                birdAnimations[i]=model.GetComponent<BuzzardAnimation>();
                footHeight[i]=.43f*Mathf.Abs(transform.lossyScale.y);
                nextLanding[i]=6+(i%2)*9;
                if(birdAnimations[i])birdAnimations[i].PlayPose("Fly",i*.173f%1);
            }
            PositionBirds(true);
        }
        void LateUpdate(){clock+=Time.deltaTime;PositionBirds(false);}
        void PositionBirds(bool snap)
        {
            if(birds==null || !gameplayCamera)return;
            var groupBounds=new Bounds(leader?leader.position:cityCenter,Vector3.zero);
            foreach(var member in procession)if(member)groupBounds.Encapsulate(member.position);
            Vector3 orbitCenter=groupBounds.center;
            float groupRadius=new Vector2(groupBounds.extents.x,groupBounds.extents.z).magnitude;
            for(int i=0;i<birds.Length;i++)
            {
                float phase=clock*(.30f+(i%4)*.035f)+i*2.399f;
                Vector3 position,forward;
                // Full world-space circles enclose the leader, soldiers, priests and Ark.
                float radius=groupRadius+orbitClearance+(i%3)*2;
                position=orbitCenter+new Vector3(Mathf.Cos(phase)*radius,6+(i%4)*1.5f+Mathf.Sin(phase*2),Mathf.Sin(phase)*radius);
                // Clear towers and buildings if part of an orbit passes above the city.
                if(Physics.Raycast(position+Vector3.up*80,Vector3.down,out var roof,160,1,QueryTriggerInteraction.Ignore))position.y=Mathf.Max(position.y,roof.point.y+4);
                forward=new Vector3(-Mathf.Sin(phase),0,Mathf.Cos(phase));
                if(birdAnimations[i] && (i==4 || i==5)) GroundVisit(i,ref position,ref forward);
                else if(birdAnimations[i])birdAnimations[i].PlayPose(Mathf.Sin(clock*.4f+i)>.15f?"Glide":"Fly",i*.173f%1);
                birds[i].position=snap?position:Vector3.Lerp(birds[i].position,position,1-Mathf.Exp(-8*Time.deltaTime));
                birds[i].rotation=Quaternion.LookRotation(forward.normalized,Vector3.up)*Quaternion.Euler(0,0,Mathf.Sin(phase)*18);
                if(groundPhase[i]>0)birds[i].rotation=Quaternion.LookRotation(forward.normalized,Vector3.up);
            }
        }
        void GroundVisit(int i,ref Vector3 flightPosition,ref Vector3 forward)
        {
            if(groundPhase[i]==0 && clock>=nextLanding[i])
            {
                if(TryLandingSpot(i,out var spot))
                {landing[i]=spot+Vector3.up*footHeight[i];departure[i]=birds[i].position;phaseStart[i]=clock;groundPhase[i]=1;birdAnimations[i].PlayPose("Land");}
                else nextLanding[i]=clock+3;
            }
            float elapsed=clock-phaseStart[i];
            if(groundPhase[i]==1)
            {
                forward=landing[i]-departure[i];forward.y=0;
                flightPosition=Vector3.Lerp(departure[i],landing[i],Mathf.SmoothStep(0,1,elapsed/2));
                if(elapsed>=2){groundPhase[i]=2;phaseStart[i]=clock;birdAnimations[i].PlayPose("Perch");}
            }
            else if(groundPhase[i]==2)
            {
                flightPosition=landing[i];forward=birds[i].forward;forward.y=0;
                if(elapsed>=1){groundPhase[i]=3;phaseStart[i]=clock;birdAnimations[i].PlayPose("Hop");}
            }
            else if(groundPhase[i]==3)
            {
                float progress=Mathf.Min(1,elapsed/3);
                forward=birds[i].forward;forward.y=0;
                flightPosition=landing[i]+forward*(progress*1.2f)+Vector3.up*(Mathf.Abs(Mathf.Sin(elapsed*Mathf.PI))*.28f);
                if(elapsed>=3){groundPhase[i]=4;phaseStart[i]=clock;departure[i]=flightPosition;birdAnimations[i].PlayPose("TakeOff");}
            }
            else if(groundPhase[i]==4)
            {
                flightPosition=Vector3.Lerp(departure[i],flightPosition,Mathf.SmoothStep(0,1,elapsed/2));
                if(elapsed>=2){groundPhase[i]=0;nextLanding[i]=clock+18;birdAnimations[i].PlayPose("Fly");}
            }
            else birdAnimations[i].PlayPose("Fly");
        }
        bool TryLandingSpot(int bird,out Vector3 spot)
        {
            spot=Vector3.zero;
            if(!leader)return false;
            for(int row=0;row<7;row++)for(int col=0;col<5;col++)
            {
                float x=.18f+col*.16f;if(bird%2==1)x=1-x;
                var ray=gameplayCamera.ViewportPointToRay(new Vector3(x,.32f+row*.055f,0));
                if(!Physics.Raycast(ray,out var hit,80,1,QueryTriggerInteraction.Ignore) || hit.normal.y<.9f)continue;
                Vector3 delta=hit.point-leader.position;delta.y=0;
                if(delta.magnitude<minimumLandingDistance || delta.magnitude>maximumLandingDistance || Mathf.Abs(hit.point.y-leader.position.y)>1.2f)continue;
                bool clear=true;
                foreach(var member in procession)if(member)
                {var gap=hit.point-member.position;gap.y=0;if(gap.magnitude<9){clear=false;break;}}
                if(!clear)continue;
                // Leave room for the short hop without moving into a wall or over an edge.
                var hopDirection=hit.point-birds[bird].position;hopDirection.y=0;hopDirection.Normalize();
                if(Physics.SphereCast(hit.point+Vector3.up*.5f,.4f,hopDirection,out _,1.8f,1,QueryTriggerInteraction.Ignore))continue;
                if(!Physics.Raycast(hit.point+hopDirection*1.5f+Vector3.up*2,Vector3.down,out var end,3,1,QueryTriggerInteraction.Ignore) || end.normal.y<.9f || Mathf.Abs(end.point.y-hit.point.y)>.25f)continue;
                spot=hit.point;return true;
            }
            return false;
        }
    }
}

