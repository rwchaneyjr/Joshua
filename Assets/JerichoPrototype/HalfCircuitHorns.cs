using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JerichoPrototype
{
    public sealed class HalfCircuitHorns : MonoBehaviour
    {
        public bool Playing { get; private set; }
        bool checkpointOccupied;
        public void PlayAtFinish(Transform player, Vector3 cityCenter)
        {
            if(!player || Playing)return;
            StartCoroutine(Perform(player,cityCenter));
        }
        public void CheckCheckpoint(Transform checkpoint, Transform player, Vector3 cityCenter)
        {
            if (!checkpoint || !player || Playing) return;
            Vector3 delta=player.position-checkpoint.position;delta.y=0;
            if(delta.magnitude>16f)checkpointOccupied=false;
            if(Playing || checkpointOccupied || delta.magnitude>10f)return;
            checkpointOccupied=true;
            StartCoroutine(Perform(player,cityCenter));
        }
        ThirdPersonCamera cameraRig;
        JoshuaController controller;
        bool controllerEnabled;
        readonly Dictionary<ProcessionFollower,bool> followers = new Dictionary<ProcessionFollower,bool>();
        readonly Dictionary<ArkProcession.ArkCarryRig,bool> carriers = new Dictionary<ArkProcession.ArkCarryRig,bool>();
        IEnumerator Perform(Transform player,Vector3 cityCenter)
        {
            var priests=FindObjectsOfType<PriestHornBlow>().Where(p=>p.gameObject.scene==gameObject.scene && p.isActiveAndEnabled).ToArray();
            cameraRig=FindObjectsOfType<ThirdPersonCamera>().FirstOrDefault(c=>c.target==player && c.isActiveAndEnabled);
            if(priests.Length==0 || !cameraRig)yield break;
            Playing=true;
            controller=player.GetComponent<JoshuaController>();
            if(controller)
            {
                controllerEnabled=controller.enabled; controller.enabled=false;
                if(controller.animator){controller.animator.SetInteger("TestState",1);controller.animator.SetFloat("Speed",0);}
            }
            foreach(var f in FindObjectsOfType<ProcessionFollower>().Where(f=>f.gameObject.scene==gameObject.scene))
            {followers[f]=f.MovementSuppressed;f.SetMovementSuppressed(true);}
            foreach(var rig in FindObjectsOfType<ArkProcession.ArkCarryRig>().Where(r=>r.gameObject.scene==gameObject.scene && r.isActiveAndEnabled))
            {carriers[rig]=rig.walk;rig.walk=false;rig.enabled=false;}
            Bounds bounds=new Bounds(priests[0].transform.position+Vector3.up,Vector3.one*2);
            // Imported mesh bounds include distant helper geometry; frame the actual priest bodies.
            foreach(var p in priests)bounds.Encapsulate(new Bounds(p.transform.position+Vector3.up*1.2f,new Vector3(2.5f,3,2.5f)));
            bounds.Expand(.5f);
            cameraRig.ShowHornView(bounds,cityCenter);
            yield return new WaitForSeconds(.9f);
            foreach(var priest in priests)priest.BlowHorn();
            // Wait for actual phase completion; timing remains owned by the authored horn animation.
            while(priests.Any(p=>p && p.IsBlowing))yield return null;
            yield return new WaitForSeconds(.3f);
            Restore();
        }
        void Restore()
        {
            if(!Playing)return;
            if(cameraRig)cameraRig.EndHornView();
            if(controller)controller.enabled=controllerEnabled;
            foreach(var entry in followers)if(entry.Key)entry.Key.SetMovementSuppressed(entry.Value);
            foreach(var entry in carriers)if(entry.Key){entry.Key.walk=entry.Value;entry.Key.enabled=true;}
            followers.Clear();carriers.Clear();Playing=false;
        }
        void OnDisable(){StopAllCoroutines();Restore();}
    }
}
