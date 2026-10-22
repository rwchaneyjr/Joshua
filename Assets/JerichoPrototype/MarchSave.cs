using System;
using UnityEngine;
namespace JerichoPrototype
{
    [Serializable] public sealed class MarchSave
    {
        public const string Key="Jericho.MarchProgress.v1";
        public int version=1, day=1, circuits, reached;
        public MarchPose[] poses;
        public Quaternion formationHeading=Quaternion.identity;
        public float cameraYaw;
        public bool wallsFallen;
        public bool Valid => version==1 && day>=1 && day<=7 && circuits>=0 && circuits<=(day==7?7:1) && reached>=0 && reached<=6 && poses!=null && poses.Length>0;
        public static MarchSave Load()
        {
            try {var data=JsonUtility.FromJson<MarchSave>(PlayerPrefs.GetString(Key,""));return data!=null && data.Valid?data:null;}
            catch(Exception){return null;}
        }
        public void Store(){PlayerPrefs.SetString(Key,JsonUtility.ToJson(this));PlayerPrefs.Save();}
        public static void Clear(){PlayerPrefs.DeleteKey(Key);PlayerPrefs.Save();}
    }
    [Serializable] public sealed class MarchPose
    {
        public string path;
        public Vector3 position;
        public Quaternion rotation;
    }
}
