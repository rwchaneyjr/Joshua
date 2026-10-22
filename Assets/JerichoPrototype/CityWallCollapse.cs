using System.Collections;
using UnityEngine;

namespace JerichoPrototype
{
    public sealed class CityWallCollapse : MonoBehaviour
    {
        public Transform[] pieces;
        public float collapseForce = 5.5f;
        public float collapseTorque = 3.5f;
        public bool Collapsed { get; private set; }
        public bool Finished { get; private set; }
        public void RestoreCollapsed()
        {
            StopAllCoroutines();Collapsed=true;Finished=true;
            for(int i=0;i<pieces.Length;i++)
            {
                Vector3 inward=-new Vector3(positions[i].x,0,positions[i].z).normalized;
                var target=positions[i]+inward*(1.5f+i%4);target.y=.45f+(i%3)*.3f;
                pieces[i].localPosition=target;
                    pieces[i].localRotation=Quaternion.Euler(48+i%22,i*20%360,i%2==0?8:-8);
                foreach(var body in pieces[i].GetComponentsInChildren<Rigidbody>()) Destroy(body.gameObject);
                foreach(var c in pieces[i].GetComponentsInChildren<Collider>())c.enabled=false;
            }
        }
        Vector3[] positions;
        Quaternion[] rotations;
        void Awake()
        {
            positions = new Vector3[pieces.Length];
            rotations = new Quaternion[pieces.Length];
            for (int i = 0; i < pieces.Length; i++)
            { positions[i] = pieces[i].localPosition; rotations[i] = pieces[i].localRotation; }
        }
        [ContextMenu("Preview collapse (Play Mode)")]
        public void Collapse()
        {
            if (!Application.isPlaying || Collapsed) return;
            Collapsed = true;
            StartCoroutine(Fall());
        }
        [ContextMenu("Reset walls (Play Mode)")]
        public void ResetWalls()
        {
            if (!Application.isPlaying || positions == null) return;
            StopAllCoroutines(); Collapsed = false; Finished=false;
            for (int i = 0; i < pieces.Length; i++)
            {
                pieces[i].localPosition = positions[i]; pieces[i].localRotation = rotations[i];
                foreach(var body in pieces[i].GetComponentsInChildren<Rigidbody>()) Destroy(body.gameObject);
                foreach (var c in pieces[i].GetComponentsInChildren<Collider>()) c.enabled = true;
            }
        }
        IEnumerator Fall()
        {
            // Detach each masonry block with a staggered impulse. The wall core
            // remains static behind the blocks, while the visible stones collide
            // and settle into believable rubble.
            Vector3 center = transform.position;
            var released = new bool[pieces.Length];
            float elapsed = 0f;
            while (elapsed < 7.5f)
            {
                for (int i = 0; i < pieces.Length; i++)
                {
                    if (released[i] || elapsed < (i % 8) * .08f) continue;
                    released[i] = true;
                    var piece = pieces[i];
                    foreach (var c in piece.GetComponentsInChildren<Collider>()) c.enabled = true;
                    var body = piece.gameObject.GetComponent<Rigidbody>();
                    if (!body) body = piece.gameObject.AddComponent<Rigidbody>();
                    body.mass = 1.5f;
                    body.drag = .15f;
                    body.angularDrag = .35f;
                    Vector3 outward = (piece.position - center);
                    outward.y = .15f;
                    outward = outward.sqrMagnitude > .01f ? outward.normalized : Vector3.forward;
                    body.AddForce((outward + Vector3.up * .25f) * collapseForce, ForceMode.Impulse);
                    body.AddTorque(Random.insideUnitSphere * collapseTorque, ForceMode.Impulse);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            Finished=true;
        }
    }
}
