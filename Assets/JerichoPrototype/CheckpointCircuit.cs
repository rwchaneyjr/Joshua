using UnityEngine;
namespace JerichoPrototype
{
    // Explicit numbered checkpoints avoid losing the entire lap when the route cuts a corner.
    public sealed class CheckpointCircuit
    {
        readonly Vector3[] points;
        readonly float radius;
        public int Reached { get; private set; }
        public bool Complete => Reached >= points.Length;
        public CheckpointCircuit(Vector3[] points, float radius, int reached = 0)
        { this.points=points;this.radius=radius;Reached=Mathf.Clamp(reached,0,points.Length); }
        public bool Sample(Vector3 position)
        {
            if(Complete || points.Length==0)return false;
            Vector3 delta=position-points[Reached];delta.y=0;
            if(delta.sqrMagnitude>radius*radius)return false;
            Reached++;return true;
        }
        public float Fraction(Vector3 position)
        {
            if(Complete)return 1;
            Vector3 start=points[(Reached+points.Length-1)%points.Length];
            Vector3 segment=points[Reached]-start;segment.y=0;
            Vector3 delta=position-start;delta.y=0;
            float t=segment.sqrMagnitude<.01f?0:Mathf.Clamp01(Vector3.Dot(delta,segment)/segment.sqrMagnitude);
            return (Reached+t)/points.Length;
        }
    }
}
