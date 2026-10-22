using UnityEngine;
namespace JerichoPrototype
{
    public sealed class BuzzardAnimation : MonoBehaviour
    {
        public Animation clips;
        string current;
        public void PlayPose(string pose,float offset=0)
        {
            if(!clips || !clips[pose] || current==pose)return;
            current=pose;
            clips[pose].normalizedTime=offset;
            clips.CrossFade(pose,.2f);
        }
    }
}
