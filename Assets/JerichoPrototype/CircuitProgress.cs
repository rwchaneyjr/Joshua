using UnityEngine;

namespace JerichoPrototype
{
    // Signed angular travel prevents back-and-forth movement from counting as a lap.
    public sealed class CircuitProgress
    {
        readonly Vector2 center, innerHalfSize;
        Vector2 previous;
        bool hasPrevious;
        public float Degrees { get; private set; }
        public bool Complete => Mathf.Abs(Degrees) >= 359f;
        public void FinishAtMarker(Vector3 position, Vector3 finish, float radius)
        {
            if(Complete || Mathf.Abs(Degrees)<315f)return;
            Vector2 delta=new Vector2(position.x-finish.x,position.z-finish.z);
            if(delta.sqrMagnitude<=radius*radius)Degrees=Mathf.Sign(Degrees)*360f;
        }
        public CircuitProgress(Vector2 center, Vector2 innerHalfSize)
        { this.center = center; this.innerHalfSize = innerHalfSize; }
        public void Sample(Vector3 worldPosition)
        {
            if (Complete) return;
            Vector2 current = new Vector2(worldPosition.x, worldPosition.z) - center;
            if (Inside(current)) { Reset(); return; }
            if (hasPrevious)
            {
                // Check the entire movement segment, not just its endpoints, for shortcuts.
                if (CrossesInterior(previous, current)) { Reset(); previous = current; hasPrevious = true; return; }
                Degrees += Vector2.SignedAngle(previous, current);
            }
            previous = current; hasPrevious = true;
        }
        bool Inside(Vector2 point) => Mathf.Abs(point.x) < innerHalfSize.x && Mathf.Abs(point.y) < innerHalfSize.y;
        bool CrossesInterior(Vector2 a, Vector2 b)
        {
            float enter = 0, exit = 1;
            Vector2 delta = b-a;
            for (int axis=0;axis<2;axis++)
            {
                if (Mathf.Abs(delta[axis]) < .0001f)
                { if (Mathf.Abs(a[axis]) >= innerHalfSize[axis]) return false; }
                else
                {
                    float one=(-innerHalfSize[axis]-a[axis])/delta[axis];
                    float two=(innerHalfSize[axis]-a[axis])/delta[axis];
                    enter=Mathf.Max(enter,Mathf.Min(one,two)); exit=Mathf.Min(exit,Mathf.Max(one,two));
                    if (enter >= exit) return false;
                }
            }
            return enter < exit;
        }
        void Reset() { Degrees=0;hasPrevious=false; }
    }
}
