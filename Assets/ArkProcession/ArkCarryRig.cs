using UnityEngine;
namespace ArkProcession {
[DefaultExecutionOrder(1000)]
public sealed class ArkCarryRig : MonoBehaviour {
 public Animator[] priests;
 public Transform[] leftGrips, rightGrips;
 public Transform ark;
 public bool twoHands = false;
 public JerichoPrototype.ProcessionManager procession;
 public Vector3 formationOffset = new Vector3(0,0,-24);
 public bool walk = true;
 [Min(.1f)] public float cycleSeconds = 1.15f;
 public float forwardSpeed = .75f;
 Vector3 arkRest; float phase;
 void Awake(){arkRest=ark.localPosition;foreach(var a in priests){a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;}}
 void Update(){if(procession && procession.leader){var target=procession.leader.position+procession.Heading*formationOffset;target.y=transform.position.y;var before=transform.position;transform.position=Vector3.MoveTowards(before,target,6.5f*Time.deltaTime);transform.rotation=Quaternion.RotateTowards(transform.rotation,procession.Heading,65f*Time.deltaTime);walk=Vector3.Distance(before,transform.position)>.001f;}else if(walk)transform.position+=transform.forward*(forwardSpeed*Time.deltaTime);if(walk)phase+=Time.deltaTime/cycleSeconds;Sample(phase);}
 public void Sample(float time){
  if(!ark)return;
  if(arkRest==Vector3.zero)arkRest=ark.localPosition;
  ark.localPosition=arkRest+Vector3.up*(walk?.012f*Mathf.Sin(time*Mathf.PI*4):0);
  foreach(var a in priests){a.SetBool("Marching",walk);a.SetFloat("MarchPhase",Mathf.Repeat(time,1));}
 }
 void LateUpdate(){ApplyHands();}
 public void ApplyHands(){for(int i=0;i<priests.Length;i++){
  if(twoHands || i%2==0)Solve(priests[i],HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,rightGrips[i]);
  if(twoHands || i%2==1)Solve(priests[i],HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,leftGrips[i]);
 }}
 static void Solve(Animator a,HumanBodyBones up,HumanBodyBones low,HumanBodyBones end,Transform target){
  var u=a.GetBoneTransform(up);var l=a.GetBoneTransform(low);var h=a.GetBoneTransform(end);if(!u||!l||!h||!target)return;
  var delta=target.position-u.position;float d=delta.magnitude; if(d<.001f)return;
  float ab=Vector3.Distance(u.position,l.position),bc=Vector3.Distance(l.position,h.position);
  d=Mathf.Clamp(d,Mathf.Abs(ab-bc)+.001f,ab+bc-.001f);var dir=delta.normalized;
  var bend=Vector3.ProjectOnPlane(-a.transform.up+a.transform.forward*.2f,dir).normalized;
  float along=(ab*ab-bc*bc+d*d)/(2*d);var elbow=u.position+dir*along+bend*Mathf.Sqrt(Mathf.Max(0,ab*ab-along*along));
  u.rotation=Quaternion.FromToRotation(l.position-u.position,elbow-u.position)*u.rotation;
  l.rotation=Quaternion.FromToRotation(h.position-l.position,target.position-l.position)*l.rotation;
 }
}
}

