using System.Collections;
using UnityEngine;

namespace JerichoPrototype
{
    public sealed class CityWallCollapse : MonoBehaviour
    {
        public Transform[] pieces;
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
                pieces[i].localRotation=Quaternion.Euler(70+i%35,i*37%360,i%2==0?15:-15);
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
                foreach (var c in pieces[i].GetComponentsInChildren<Collider>()) c.enabled = true;
            }
        }
        IEnumerator Fall()
        {
            // Controlled rubble stays inside the marching route; no hundreds-body physics spike.
            foreach (var p in pieces) foreach (var c in p.GetComponentsInChildren<Collider>()) c.enabled = false;
            float elapsed = 0;
            while (elapsed < 5)
            {
                elapsed += Time.deltaTime;
                for (int i = 0; i < pieces.Length; i++)
                {
                    float t = Mathf.Clamp01((elapsed - (i % 19) * .07f) / 2.8f);
                    float ease = t * t * (3 - 2 * t);
                    Vector3 inward = -new Vector3(positions[i].x, 0, positions[i].z).normalized;
                    Vector3 target = positions[i] + inward * (1.5f + i % 4);
                    target.y = .45f + (i % 3) * .3f;
                    pieces[i].localPosition = Vector3.Lerp(positions[i], target, ease);
                    pieces[i].localRotation = Quaternion.Slerp(rotations[i], Quaternion.Euler(70 + i % 35, i * 37 % 360, i % 2 == 0 ? 15 : -15), ease);
                }
                yield return null;
            }
            Finished=true;
        }
    }
}
